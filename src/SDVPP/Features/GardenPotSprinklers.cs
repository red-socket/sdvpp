using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace SDVPP.Features;

/// <summary>Sprinklers water garden pots in their range, as they do tilled soil.</summary>
/// <remarks>
/// Host-authoritative; pot state syncs to farmhands. Vanilla sprinklers only water hoe dirt on the ground, overnight
/// after the pots' own day update has dried them, so watering on <see cref="IGameLoopEvents.DayStarted"/> lasts the
/// day. Follows the vanilla sprinkler rules: not outdoors on rainy days (rain waters the pots there), not on
/// NoSprinklers tiles, not while the NO_SPRINKLER special order rule is active.
/// </remarks>
internal sealed class GardenPotSprinklers
{
    private readonly IMonitor monitor;
    private readonly Func<ModConfig> config;

    public GardenPotSprinklers(IModHelper helper, IMonitor monitor, Func<ModConfig> config)
    {
        this.monitor = monitor;
        this.config = config;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsMainPlayer || !this.config().SprinklersWaterGardenPots)
            return;
        if (Game1.player.team.SpecialOrderRuleActive("NO_SPRINKLER"))
            return;

        int watered = 0;
        Utility.ForEachLocation(location =>
        {
            if (location.IsOutdoors && location.IsRainingHere())
                return true;

            foreach (StardewValley.Object sprinkler in location.objects.Values)
            {
                if (!sprinkler.IsSprinkler() || sprinkler.GetModifiedRadiusForSprinkler() < 0)
                    continue;

                foreach (var tile in sprinkler.GetSprinklerTiles())
                {
                    if (location.objects.TryGetValue(tile, out var obj) && obj is IndoorPot pot && pot.hoeDirt.Value is { } dirt
                        && !dirt.isWatered()
                        && location.doesTileHavePropertyNoNull((int)tile.X, (int)tile.Y, "NoSprinklers", "Back") != "T")
                    {
                        pot.Water();
                        watered++;
                    }
                }
            }
            return true;
        });

        this.monitor.Log($"Sprinklers watered {watered} garden pot(s).", LogLevel.Trace);
    }
}
