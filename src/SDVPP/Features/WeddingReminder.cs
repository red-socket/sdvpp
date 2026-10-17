using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SDVPP.Features;

/// <summary>Leave a letter and a HUD message on the day before the player's wedding.</summary>
/// <remarks>
/// Client-only: each player checks their own engagement on their own screen. The wedding plays the moment the
/// player wakes, so the day before is the last chance to dress for it; vanilla only says "in 3 days" when engaged.
/// </remarks>
internal sealed class WeddingReminder
{
    /// <summary>The letter's key in <c>Data/mail</c>, added by the content pack.</summary>
    private const string MailId = "Luke.SDVPP.Content_WeddingTomorrow";

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Func<ModConfig> config;

    public WeddingReminder(IModHelper helper, IMonitor monitor, Func<ModConfig> config)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.config = config;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!this.config().WeddingReminder || !IsWeddingTomorrow(Game1.player))
            return;

        // added straight to the mailbox: addMail would skip it after a first marriage, since the id was already read
        if (!Game1.player.mailbox.Contains(MailId))
            Game1.player.mailbox.Add(MailId);
        Game1.addHUDMessage(new HUDMessage(this.helper.Translation.Get("weddingReminder.hud"), HUDMessage.newQuest_type));
        this.monitor.Log("Wedding is tomorrow; left a reminder letter.", LogLevel.Trace);
    }

    private static bool IsWeddingTomorrow(Farmer player)
    {
        WorldDate tomorrow = new(Game1.Date);
        tomorrow.TotalDays++;

        // weddings never happen on festival or green-rain days; the game postpones them
        if (!Game1.canHaveWeddingOnDay(tomorrow.DayOfMonth, tomorrow.Season))
            return false;

        Friendship? friendship = null;
        if (player.spouse != null && player.friendshipData.TryGetValue(player.spouse, out Friendship npcFriendship))
            friendship = npcFriendship;
        else if (player.team.GetSpouse(player.UniqueMultiplayerID) is long otherPlayer)
            friendship = player.team.GetFriendship(player.UniqueMultiplayerID, otherPlayer);

        // roommates (Krobus) have no ceremony
        return friendship is { } f
            && f.IsEngaged()
            && !f.RoommateMarriage
            && f.WeddingDate != null
            && f.WeddingDate.TotalDays <= tomorrow.TotalDays;
    }
}
