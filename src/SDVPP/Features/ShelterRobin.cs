using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;

namespace SDVPP.Features;

/// <summary>Robin's buildings and house upgrades can use the Wood and Hardwood stored in the wood shelter.</summary>
/// <remarks>
/// The game checks and takes building materials from the backpack with <c>Inventory.ContainsId</c> and
/// <c>Inventory.ReduceId</c>; a transpiler sends those calls through helpers that also count the shelter while this
/// screen holds its lock. The lock is held for as long as Robin's menu is open, because the menu checks materials when
/// you confirm and takes them later without checking again.
/// </remarks>
internal sealed class ShelterRobin
{
    /// <summary>Harmony patches are static, so they reach the feature through this.</summary>
    private static ShelterRobin? instance;

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Func<ModConfig> config;

    /// <summary>Whether this screen holds the shelter lock for Robin, so the helpers may count the shelter.</summary>
    private readonly PerScreen<bool> lockHeld = new();

    /// <summary>The Robin menu the lock was requested for.</summary>
    private readonly PerScreen<CarpenterMenu?> lockedMenu = new();

    public ShelterRobin(IModHelper helper, IMonitor monitor, Func<ModConfig> config, Harmony harmony)
    {
        instance = this;
        this.helper = helper;
        this.monitor = monitor;
        this.config = config;

        HarmonyMethod transpiler = new(typeof(ShelterRobin), nameof(Transpile_UseShelterWood));
        foreach (MethodBase method in new MethodBase[]
        {
            AccessTools.Method(typeof(CarpenterMenu), nameof(CarpenterMenu.DoesFarmerHaveEnoughResourcesToBuild)),
            AccessTools.Method(typeof(CarpenterMenu), nameof(CarpenterMenu.ConsumeResources)),
            AccessTools.Method(typeof(CarpenterMenu), nameof(CarpenterMenu.draw), new[] { typeof(Microsoft.Xna.Framework.Graphics.SpriteBatch) }),
            AccessTools.Method(typeof(GameLocation), "houseUpgradeAccept"),
            AccessTools.Method(typeof(GameLocation), "communityUpgradeAccept")
        })
        {
            harmony.Patch(method, transpiler: transpiler);
        }
        harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.answerDialogueAction)),
            prefix: new HarmonyMethod(typeof(ShelterRobin), nameof(Before_AnswerDialogueAction))
        );

        helper.Events.Display.MenuChanged += this.OnMenuChanged;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
    }

    /*********
    ** Material checks
    *********/
    /// <summary>Send the method's backpack material checks through <see cref="ContainsId"/> and <see cref="ReduceId"/>.</summary>
    /// <remarks>Same stack shape: the inventory the game called on becomes the helper's first argument.</remarks>
    private static IEnumerable<CodeInstruction> Transpile_UseShelterWood(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        MethodInfo containsId = AccessTools.Method(typeof(Inventory), nameof(Inventory.ContainsId), new[] { typeof(string), typeof(int) });
        MethodInfo reduceId = AccessTools.Method(typeof(Inventory), nameof(Inventory.ReduceId), new[] { typeof(string), typeof(int) });

        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(containsId))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(ShelterRobin), nameof(ContainsId));
                replaced++;
            }
            else if (instruction.Calls(reduceId))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(ShelterRobin), nameof(ReduceId));
                replaced++;
            }
            yield return instruction;
        }

        if (replaced == 0)
            instance?.monitor.Log($"Couldn't find the material checks in {original.DeclaringType?.Name}.{original.Name}; Robin won't use shelter wood there.", LogLevel.Error);
    }

    /// <summary>The backpack's <c>ContainsId</c>, also counting shelter wood while Robin's lock is held.</summary>
    public static bool ContainsId(Inventory inventory, string itemId, int minimum)
    {
        if (GetShelterFor(inventory, itemId) is not { } shelter)
            return inventory.ContainsId(itemId, minimum);
        return inventory.CountId(itemId) + shelter.CountId(itemId) >= minimum;
    }

    /// <summary>The backpack's <c>ReduceId</c>, taking from the backpack first and the rest from the shelter while Robin's lock is held.</summary>
    public static int ReduceId(Inventory inventory, string itemId, int count)
    {
        if (GetShelterFor(inventory, itemId) is not { } shelter)
            return inventory.ReduceId(itemId, count);

        int removed = inventory.ReduceId(itemId, Math.Min(count, inventory.CountId(itemId)));
        if (removed < count)
            removed += shelter.ReduceId(itemId, count - removed);
        return removed;
    }

    /// <summary>The shelter inventory to add to a check, or null to leave the game's check alone.</summary>
    private static Inventory? GetShelterFor(Inventory inventory, string itemId)
    {
        try
        {
            if (instance?.lockHeld.Value != true || !ReferenceEquals(inventory, Game1.player.Items))
                return null;
            if (ItemRegistry.QualifyItemId(itemId) is not ("(O)388" or "(O)709"))
                return null;
            return WoodShelter.GetInventory();
        }
        catch (Exception ex)
        {
            instance?.monitor.Log($"Checking shelter wood failed; using the backpack only.\n{ex}", LogLevel.Error);
            return null;
        }
    }

    private static bool ShelterHasWood()
    {
        return WoodShelter.GetInventory() is { } shelter && (shelter.CountId("(O)388") > 0 || shelter.CountId("(O)709") > 0);
    }

    /*********
    ** Robin's build menu
    *********/
    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is not CarpenterMenu menu || !this.config().BuildWithShelterWood || ReferenceEquals(menu, this.lockedMenu.Value) || !ShelterHasWood())
            return;

        this.lockedMenu.Value = menu;
        WoodShelter.GetMutex().RequestLock(acquired: () =>
        {
            if (ReferenceEquals(Game1.activeClickableMenu, menu))
                this.lockHeld.Value = true;
            else
                WoodShelter.GetMutex().ReleaseLock();
        });
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        // release once Robin's menu is gone (a confirmation dialogue may briefly replace it)
        if (this.lockedMenu.Value is not { } menu || Game1.activeClickableMenu is DialogueBox || ReferenceEquals(Game1.activeClickableMenu, menu))
            return;

        this.lockedMenu.Value = null;
        if (this.lockHeld.Value)
        {
            this.lockHeld.Value = false;
            WoodShelter.GetMutex().ReleaseLock();
        }
    }

    /*********
    ** House and community upgrades
    *********/
    /// <summary>Accept a house or community upgrade under the shelter lock, so the shelter's wood counts.</summary>
    private static bool Before_AnswerDialogueAction(GameLocation __instance, string questionAndAnswer, ref bool __result)
    {
        string? acceptMethod = questionAndAnswer switch
        {
            "upgrade_Yes" => "houseUpgradeAccept",
            "communityUpgrade_Yes" => "communityUpgradeAccept",
            _ => null
        };
        if (acceptMethod is null || instance is null || !instance.config().BuildWithShelterWood || !ShelterHasWood())
            return true;

        ShelterRobin feature = instance;
        MethodInfo accept = AccessTools.Method(typeof(GameLocation), acceptMethod);
        WoodShelter.GetMutex().RequestLock(
            acquired: () =>
            {
                feature.lockHeld.Value = true;
                try
                {
                    accept.Invoke(__instance, null);
                }
                finally
                {
                    feature.lockHeld.Value = false;
                    WoodShelter.GetMutex().ReleaseLock();
                }
            },
            failed: () => Game1.showRedMessage(feature.helper.Translation.Get("woodShelter.inUse"))
        );

        __result = true;
        return false;
    }
}
