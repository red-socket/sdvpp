using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using xTile.Dimensions;
using SObject = StardewValley.Object;

namespace SDVPP.Features;

/// <summary>Stop two accidents: the "Eat?" prompt when clicking crops or forage with food in hand, and gifting an item to an NPC who stands below a door you meant to open.</summary>
/// <remarks>Client-only: both run for the local player.</remarks>
internal sealed class AccidentGuards
{
    /// <summary>Harmony patches are static, so they reach the feature through this.</summary>
    private static AccidentGuards? instance;

    private readonly IMonitor monitor;
    private readonly Func<ModConfig> config;

    /// <summary>Set while the game handles a click on a door or warp tile that no NPC stands on.</summary>
    private readonly PerScreen<bool> checkingDoor = new();

    public AccidentGuards(IMonitor monitor, Func<ModConfig> config, Harmony harmony)
    {
        instance = this;
        this.monitor = monitor;
        this.config = config;

        harmony.Patch(
            original: AccessTools.Method(typeof(Game1), nameof(Game1.pressActionButton)),
            transpiler: new HarmonyMethod(typeof(AccidentGuards), nameof(Transpile_PressActionButton))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.checkAction)),
            prefix: new HarmonyMethod(typeof(AccidentGuards), nameof(Before_LocationCheckAction)),
            postfix: new HarmonyMethod(typeof(AccidentGuards), nameof(After_LocationCheckAction))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(NPC), nameof(NPC.checkAction)),
            prefix: new HarmonyMethod(typeof(AccidentGuards), nameof(Before_NpcCheckAction))
        );
    }

    /*********
    ** "Eat?" prompt
    *********/
    /// <summary>Feed the Eat branch's "freshly pressed" check through <see cref="AllowEatPrompt"/>.</summary>
    /// <remarks>The branch is the only one whose <c>didPlayerJustRightClick</c> call follows the <c>-300</c> inedible check; nothing in it has run yet.</remarks>
    private static IEnumerable<CodeInstruction> Transpile_PressActionButton(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo justRightClicked = AccessTools.Method(typeof(Game1), nameof(Game1.didPlayerJustRightClick));
        List<CodeInstruction> code = new(instructions);

        for (int i = 0; i < code.Count; i++)
        {
            if (!code[i].LoadsConstant(-300))
                continue;
            for (int j = i + 1; j < Math.Min(i + 6, code.Count); j++)
            {
                if (code[j].Calls(justRightClicked))
                {
                    code.Insert(j + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AccidentGuards), nameof(AllowEatPrompt))));
                    return code;
                }
            }
        }

        instance?.monitor.Log("Couldn't find the \"Eat?\" prompt in Game1.pressActionButton; that guard is disabled.", LogLevel.Error);
        return code;
    }

    /// <summary>Whether the "Eat?" prompt may open, given vanilla's own answer.</summary>
    private static bool AllowEatPrompt(bool vanilla)
    {
        try
        {
            if (!vanilla || instance?.config().NoEatPromptOnCrops != true)
                return vanilla;
            return !IsTargetingCropOrForage();
        }
        catch (Exception ex)
        {
            instance?.monitor.Log($"The \"Eat?\" guard failed; showing the prompt as usual.\n{ex}", LogLevel.Error);
            return vanilla;
        }
    }

    /// <summary>Whether any tile the action press just checked holds a crop or forage.</summary>
    /// <remarks>The same tiles <c>Game1.pressActionButton</c> tries: the cursor tile (or the grab tile), one above and below it, and the player's own tile.</remarks>
    private static bool IsTargetingCropOrForage()
    {
        Farmer player = Game1.player;
        GameLocation location = player.currentLocation;

        Vector2 target = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y) / 64f;
        if (!Game1.wasMouseVisibleThisFrame || Game1.mouseCursorTransparency == 0f || !Utility.tileWithinRadiusOfPlayer((int)target.X, (int)target.Y, 1, player))
            target = player.GetGrabTile();
        target = new Vector2((int)target.X, (int)target.Y);

        foreach (Vector2 tile in new[] { target, target + new Vector2(0, 1), target - new Vector2(0, 1), player.Tile })
        {
            if (!Utility.tileWithinRadiusOfPlayer((int)tile.X, (int)tile.Y, 1, player))
                continue;

            if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature feature) && feature is HoeDirt { crop: not null })
                return true;
            if (location.objects.TryGetValue(tile, out SObject obj)
                && (obj.IsSpawnedObject || obj.isForage() || obj is IndoorPot { hoeDirt.Value.crop: not null }))
                return true;
        }
        return false;
    }

    /*********
    ** Gifts through doors
    *********/
    /// <summary>Note when the clicked tile is a door or warp that no NPC stands on.</summary>
    private static void Before_LocationCheckAction(GameLocation __instance, Location tileLocation, Farmer who)
    {
        if (instance is null || !who.IsLocalPlayer || who.ActiveObject is null || !instance.config().NoGiftsThroughDoors)
            return;

        string? action = __instance.doesTileHaveProperty(tileLocation.X, tileLocation.Y, "Action", "Buildings");
        bool isDoor = action != null && (action.Contains("Warp", StringComparison.OrdinalIgnoreCase) || action.Contains("Door", StringComparison.OrdinalIgnoreCase));
        instance.checkingDoor.Value = isDoor && __instance.isCharacterAtTile(new Vector2(tileLocation.X, tileLocation.Y)) is null;
    }

    private static void After_LocationCheckAction()
    {
        if (instance != null)
            instance.checkingDoor.Value = false;
    }

    /// <summary>While a door is being used with an item in hand, don't let an NPC standing below it take the item.</summary>
    private static bool Before_NpcCheckAction(Farmer who, ref bool __result)
    {
        if (instance?.checkingDoor.Value == true && who.IsLocalPlayer && who.ActiveObject != null)
        {
            __result = false;
            return false;
        }
        return true;
    }
}
