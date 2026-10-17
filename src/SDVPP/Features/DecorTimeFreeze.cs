using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace SDVPP.Features;

/// <summary>When time stands still for decorating.</summary>
internal enum DecorFreezeMode
{
    Off,
    HoldingFurniture,
    AnywhereAtHome
}

/// <summary>Stop the clock while the players are decorating at home.</summary>
/// <remarks>
/// Host-authoritative: only the host advances the clock, so this runs on the host with the host's config.
/// The clock is shared, so in co-op it only stops when every online player qualifies. This holds the
/// ten-minute timer in place instead of pausing the game, so NPCs, buffs and the bed's sleep prompt keep working.
/// </remarks>
internal sealed class DecorTimeFreeze
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Func<ModConfig> config;

    /// <summary>The ten-minute timer value being held, or null when time runs normally.</summary>
    private readonly PerScreen<int?> heldInterval = new();

    /// <summary>Whether the HUD message was shown today.</summary>
    private readonly PerScreen<bool> announcedToday = new();

    public DecorTimeFreeze(IModHelper helper, IMonitor monitor, Func<ModConfig> config)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.config = config;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => this.heldInterval.Value = null;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // the new day restarts the timer at zero; don't carry yesterday's held value into it
        this.heldInterval.Value = null;
        this.announcedToday.Value = false;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        bool freeze = this.ShouldFreeze(this.config().DecoratingTimeFreeze);
        if (!freeze)
        {
            if (this.heldInterval.Value is not null)
            {
                this.heldInterval.Value = null;
                this.monitor.Log("Decorating time freeze off.", LogLevel.Trace);
            }
            return;
        }

        if (this.heldInterval.Value is null)
        {
            // keep a margin below the ten-minute threshold so one long frame can't tip the clock over
            this.heldInterval.Value = Math.Min(Game1.gameTimeInterval, Game1.realMilliSecondsPerGameTenMinutes - 1000);
            this.monitor.Log($"Decorating time freeze on at {Game1.timeOfDay}.", LogLevel.Trace);

            if (!this.announcedToday.Value)
            {
                this.announcedToday.Value = true;
                Game1.addHUDMessage(HUDMessage.ForCornerTextbox(this.helper.Translation.Get("decorFreeze.started")));
            }
        }

        Game1.gameTimeInterval = this.heldInterval.Value.Value;
    }

    private bool ShouldFreeze(DecorFreezeMode mode)
    {
        if (mode == DecorFreezeMode.Off)
            return false;

        foreach (Farmer farmer in Game1.getOnlineFarmers())
        {
            // FarmHouse includes cabins
            if (farmer.currentLocation is not FarmHouse)
                return false;
            if (mode == DecorFreezeMode.HoldingFurniture && farmer.ActiveObject is not Furniture)
                return false;
        }
        return true;
    }
}
