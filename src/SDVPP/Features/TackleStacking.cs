using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace SDVPP.Features;

/// <summary>Unused fishing tackle stacks like bait; a rod still only takes one tackle per slot.</summary>
/// <remarks>
/// Every player needs this on: each machine works out stack sizes itself. Worn tackle (any <c>uses</c>) keeps a
/// stack size of 1, so the game's own <c>canStackWith</c> never merges it with new tackle.
/// </remarks>
internal sealed class TackleStacking
{
    private const int TackleCategory = SObject.tackleCategory;

    /// <summary>Harmony patches are static, so they reach the feature through this.</summary>
    private static TackleStacking? instance;

    private readonly IMonitor monitor;
    private readonly Func<ModConfig> config;

    public TackleStacking(IMonitor monitor, Func<ModConfig> config, Harmony harmony)
    {
        instance = this;
        this.monitor = monitor;
        this.config = config;

        harmony.Patch(
            original: AccessTools.Method(typeof(SObject), nameof(SObject.maximumStackSize)),
            postfix: new HarmonyMethod(typeof(TackleStacking), nameof(After_MaximumStackSize))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(Tool), nameof(Tool.attach)),
            prefix: new HarmonyMethod(typeof(TackleStacking), nameof(Before_Attach))
        );
    }

    private static bool Enabled => instance?.config().StackUnusedTackle == true;

    /// <summary>Let unused tackle stack.</summary>
    private static void After_MaximumStackSize(SObject __instance, ref int __result)
    {
        if (__instance.Category == TackleCategory && __instance.uses.Value == 0 && Enabled)
            __result = 999;
    }

    /// <summary>Attach one tackle from a stack instead of the whole stack, which would share one wear counter.</summary>
    private static bool Before_Attach(Tool __instance, SObject? o, ref SObject? __result)
    {
        try
        {
            if (!Enabled || __instance is not FishingRod rod || o is null || o.Category != TackleCategory || !rod.CanUseTackle())
                return true;

            // slot 0 is bait; the rest take tackle
            for (int slot = 1; slot < rod.attachments.Length; slot++)
            {
                if (rod.attachments[slot] is null)
                {
                    rod.attachments[slot] = (SObject)o.getOne();
                    Game1.playSound("button1");
                    __result = (SObject?)o.ConsumeStack(1);
                    return false;
                }
            }

            // every tackle slot is full: swap with the first one, like vanilla
            SObject? old = rod.attachments[1];
            if (o.Stack == 1)
            {
                rod.attachments[1] = o;
                Game1.playSound("button1");
                __result = old;
                return false;
            }

            // a stack can't be swapped with the old tackle in hand, so the old one goes to the backpack
            if (old != null && !Game1.player.couldInventoryAcceptThisItem(old))
            {
                Game1.playSound("cancel");
                __result = o;
                return false;
            }
            rod.attachments[1] = (SObject)o.getOne();
            if (old != null)
                Game1.player.addItemToInventoryBool(old);
            Game1.playSound("button1");
            __result = (SObject?)o.ConsumeStack(1);
            return false;
        }
        catch (Exception ex)
        {
            instance?.monitor.Log($"Attaching tackle failed, using the vanilla behavior.\n{ex}", LogLevel.Error);
            return true;
        }
    }
}
