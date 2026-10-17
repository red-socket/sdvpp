using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace SDVPP.Features;

/// <summary>Ask for confirmation before a warp totem or the Return Scepter warps the player.</summary>
/// <remarks>
/// Client-only: every check is about the local player, and each player's own config applies.
/// The use is cancelled in a Harmony prefix and the question opens on the next tick, so whatever else
/// the game does with the same button press runs first; if that opened a menu, no question is asked.
/// </remarks>
internal sealed class WarpConfirm
{
    /// <summary>Totems whose use warps the player (<c>Object.performUseAction</c>). Rain and treasure totems are not warps.</summary>
    private static readonly HashSet<string> WarpTotemIds = new() { "(O)688", "(O)689", "(O)690", "(O)261", "(O)886" };

    private const string ReturnScepterId = "(T)ReturnScepter";

    /// <summary>Harmony patches are static, so they reach the feature through this.</summary>
    private static WarpConfirm? instance;

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Func<ModConfig> config;

    /// <summary>The use cancelled this tick, waiting to ask the player.</summary>
    private readonly PerScreen<Item?> pending = new();

    /// <summary>Set while re-running a confirmed use, so the patches let it through.</summary>
    private readonly PerScreen<bool> bypass = new();

    public WarpConfirm(IModHelper helper, IMonitor monitor, Func<ModConfig> config, Harmony harmony)
    {
        instance = this;
        this.helper = helper;
        this.monitor = monitor;
        this.config = config;

        harmony.Patch(
            original: AccessTools.Method(typeof(SObject), nameof(SObject.performUseAction)),
            prefix: new HarmonyMethod(typeof(WarpConfirm), nameof(Before_PerformUseAction))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(Wand), nameof(Wand.DoFunction)),
            prefix: new HarmonyMethod(typeof(WarpConfirm), nameof(Before_WandDoFunction))
        );

        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
    }

    /// <summary>Cancel a warp totem used by the local player, unless it was confirmed.</summary>
    private static bool Before_PerformUseAction(SObject __instance, ref bool __result)
    {
        try
        {
            if (instance is null || instance.bypass.Value || !instance.config().ConfirmWarpTotems)
                return true;

            // only the held item; the night market and desert festival warp with a fresh totem
            Farmer player = Game1.player;
            if (!WarpTotemIds.Contains(__instance.QualifiedItemId) || !ReferenceEquals(__instance, player.ActiveObject))
                return true;

            // vanilla's own conditions for the totem to work; otherwise let it fail as usual
            bool canUse = player.canMove && !__instance.isTemporarilyInvisible
                && !Game1.eventUp && !Game1.isFestival() && !Game1.fadeToBlack
                && !player.swimming.Value && !player.bathingClothes.Value && !player.onBridge.Value;
            if (!canUse)
                return true;

            instance.pending.Value = __instance;
            __result = false; // true would make the game consume the totem
            return false;
        }
        catch (Exception ex)
        {
            instance?.monitor.Log($"Warp totem confirmation failed, using the totem without asking.\n{ex}", LogLevel.Error);
            return true;
        }
    }

    /// <summary>Cancel the local player's Return Scepter, unless it was confirmed.</summary>
    private static bool Before_WandDoFunction(Wand __instance, Farmer who)
    {
        try
        {
            if (instance is null || instance.bypass.Value || !instance.config().ConfirmReturnScepter)
                return true;

            // DoFunction also runs on this machine for other players' scepters
            if (!who.IsLocalPlayer || __instance.QualifiedItemId != ReturnScepterId)
                return true;

            instance.pending.Value = __instance;
            return false;
        }
        catch (Exception ex)
        {
            instance?.monitor.Log($"Return Scepter confirmation failed, warping without asking.\n{ex}", LogLevel.Error);
            return true;
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        Item? item = this.pending.Value;
        if (item is null)
            return;
        this.pending.Value = null;

        // the same press did something else (opened a chest, talked to someone, ...)
        if (!Context.CanPlayerMove || !ReferenceEquals(Game1.player.CurrentItem, item))
            return;

        string question = item is Wand
            ? this.helper.Translation.Get("warpConfirm.scepter", new { item = item.DisplayName })
            : this.helper.Translation.Get("warpConfirm.totem", new { item = item.DisplayName });

        GameLocation location = Game1.currentLocation;
        location.createQuestionDialogue(question, location.createYesNoResponses(), (_, answer) =>
        {
            if (answer == "Yes")
                Game1.afterDialogues = () => this.UseConfirmed(item); // after the box closes and the player can move again
        });
    }

    /// <summary>Run the use the player confirmed, the same way the game would have.</summary>
    private void UseConfirmed(Item item)
    {
        Farmer player = Game1.player;
        if (!ReferenceEquals(player.CurrentItem, item))
            return;

        this.bypass.Value = true;
        try
        {
            if (item is Wand)
                player.BeginUsingTool();
            else if (item is SObject totem && totem.performUseAction(Game1.currentLocation))
                player.reduceActiveItemByOne();
        }
        finally
        {
            this.bypass.Value = false;
        }
    }
}
