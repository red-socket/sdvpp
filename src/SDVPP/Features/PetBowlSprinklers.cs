using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;

namespace SDVPP.Features;

/// <summary>Sprinklers fill a pet bowl whose water tile they cover, the same tile the watering can fills.</summary>
/// <remarks>
/// Host-authoritative. The pet resets the bowl overnight before the new day starts, so filling it on
/// <see cref="IGameLoopEvents.DayStarted"/> lasts all day and pays friendship the next night, like rain does.
/// </remarks>
internal sealed class PetBowlSprinklers
{
    private readonly IMonitor monitor;
    private readonly Func<ModConfig> config;

    public PetBowlSprinklers(IModHelper helper, IMonitor monitor, Func<ModConfig> config)
    {
        this.monitor = monitor;
        this.config = config;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsMainPlayer || !this.config().SprinklersFillPetBowl)
            return;

        // same rule that stops vanilla sprinklers
        if (Game1.player.team.SpecialOrderRuleActive("NO_SPRINKLER"))
            return;

        int filled = 0;
        Utility.ForEachBuilding<PetBowl>(bowl =>
        {
            if (!bowl.watered.Value && IsCoveredBySprinkler(bowl))
            {
                bowl.watered.Value = true;
                filled++;
            }
            return true;
        });

        this.monitor.Log($"Sprinklers filled {filled} pet bowl(s).", LogLevel.Trace);
    }

    private static bool IsCoveredBySprinkler(PetBowl bowl)
    {
        GameLocation? location = bowl.GetParentLocation();
        if (location is null)
            return false;

        foreach (StardewValley.Object obj in location.objects.Values)
        {
            if (!obj.IsSprinkler())
                continue;

            // mirror vanilla: sprinklers outdoors don't run on rainy days (rain fills the bowl instead)
            if (location.IsOutdoors && location.IsRainingHere())
                continue;

            foreach (var tile in obj.GetSprinklerTiles())
            {
                int x = (int)tile.X, y = (int)tile.Y;
                string? value = null;
                if (bowl.doesTileHaveProperty(x, y, "PetBowl", "Buildings", ref value)
                    && location.doesTileHavePropertyNoNull(x, y, "NoSprinklers", "Back") != "T")
                    return true;
            }
        }

        return false;
    }
}
