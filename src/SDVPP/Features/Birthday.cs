using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace SDVPP.Features;

/// <summary>The farmer has a birthday: Mayor Lewis asks for the date, and on the day villagers wish them well and friends send letters.</summary>
/// <remarks>
/// Per player: the date lives in the player's own <c>Farmer.modData</c> (synced and saved per farmhand), and everything
/// runs on that player's screen. Greetings use a conversation topic, which the content pack adds to some villagers'
/// dialogue; letters and their gifts are in the content pack's <c>Data/mail</c> entries.
/// </remarks>
internal sealed class Birthday
{
    private const string BirthdayKey = "Luke.SDVPP/Birthday";
    private const string CelebratedYearKey = "Luke.SDVPP/BirthdayYear";
    private const string AskAgainKey = "Luke.SDVPP/BirthdayAskDay";

    /// <summary>The conversation topic the content pack's greetings are keyed by.</summary>
    private const string TopicId = "Luke.SDVPP_Birthday";

    /// <summary>The content pack's mail keys.</summary>
    private const string AskMailId = "Luke.SDVPP.Content_BirthdayAsk";
    private const string LetterPrefix = "Luke.SDVPP.Content_Birthday_";

    /// <summary>Villagers with a birthday greeting in the content pack.</summary>
    private static readonly string[] Greeters = { "Lewis", "Evelyn", "Gus", "Robin", "Pierre", "Marnie", "Linus", "Willy", "Clint", "Wizard", "Penny", "Vincent" };

    /// <summary>Friends who send a birthday letter at <see cref="LetterHearts"/> hearts or more.</summary>
    private static readonly string[] LetterSenders = { "Lewis", "Evelyn", "Gus", "Robin" };
    private const int LetterHearts = 4;

    /// <summary>Days before the ask letter comes back if the player didn't give a date.</summary>
    private const int AskAgainDays = 7;

    private static readonly string[] Seasons = { "spring", "summer", "fall", "winter" };

    /// <summary>Harmony patches are static, so they reach the feature through this.</summary>
    private static Birthday? instance;

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Func<ModConfig> config;

    /// <summary>Whether the spouse should wish the player a happy birthday the next time they talk today.</summary>
    private readonly PerScreen<bool> spouseGreetingPending = new();

    public Birthday(IModHelper helper, IMonitor monitor, Func<ModConfig> config, Harmony harmony)
    {
        instance = this;
        this.helper = helper;
        this.monitor = monitor;
        this.config = config;

        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.Display.MenuChanged += this.OnMenuChanged;
        harmony.Patch(
            original: AccessTools.Method(typeof(NPC), nameof(NPC.checkAction)),
            prefix: new HarmonyMethod(typeof(Birthday), nameof(Before_NpcCheckAction))
        );
        helper.ConsoleCommands.Add("sdvpp_birthday", "Show, set or reset your SDV++ birthday.\n\nUsage: sdvpp_birthday\nUsage: sdvpp_birthday set <season> <day>\nUsage: sdvpp_birthday reset", this.OnCommand);
    }

    /*********
    ** The day itself
    *********/
    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.spouseGreetingPending.Value = false;
        if (!this.config().PlayerBirthday)
            return;

        Farmer player = Game1.player;
        if (GetBirthday(player) is not var (season, day))
        {
            this.SendAskLetterIfDue(player);
            return;
        }

        if (season == Game1.season && day == Game1.dayOfMonth)
            this.Celebrate(player);
    }

    /// <summary>Run the birthday once this year: greetings, letters, and the spouse's wishes.</summary>
    private void Celebrate(Farmer player)
    {
        if (player.modData.TryGetValue(CelebratedYearKey, out string? year) && year == Game1.year.ToString())
            return;
        player.modData[CelebratedYearKey] = Game1.year.ToString();

        // greetings: the game shows a topic line once per villager ever, so forget last year's
        foreach (string npc in Greeters)
            player.mailReceived.Remove($"{npc}_{TopicId}");
        player.activeDialogueEvents[TopicId] = 0; // today only

        // letters straight into the mailbox, since the usual way skips mail the player has read before
        var letters = new List<string> { LetterPrefix + (player.IsMale ? "Mom" : "Dad") };
        letters.AddRange(LetterSenders.Where(npc => npc != player.spouse && player.getFriendshipHeartLevelForNPC(npc) >= LetterHearts).Select(npc => LetterPrefix + npc));
        foreach (string letter in letters)
        {
            if (!player.mailbox.Contains(letter))
                player.mailbox.Add(letter);
        }

        this.spouseGreetingPending.Value = player.getSpouse() != null;
        Game1.addHUDMessage(new HUDMessage(this.helper.Translation.Get("birthday.hud"), HUDMessage.newQuest_type));
        this.monitor.Log($"Birthday: {letters.Count} letter(s), greetings on.", LogLevel.Trace);
    }

    /// <summary>The first time the player talks to their spouse on their birthday, the spouse wishes them a happy birthday before anything else.</summary>
    /// <remarks>
    /// Topic lines rarely reach a spouse, and dialogue queued earlier would come after the spouse's daily line, so the
    /// greeting is shown when the player clicks. The kiss and the daily line follow on the next click.
    /// </remarks>
    private static bool Before_NpcCheckAction(NPC __instance, Farmer who, ref bool __result)
    {
        try
        {
            if (instance?.spouseGreetingPending.Value != true || !who.IsLocalPlayer || !ReferenceEquals(who.getSpouse(), __instance))
                return true;

            instance.spouseGreetingPending.Value = false;
            __instance.CurrentDialogue.Push(new Dialogue(__instance, null, instance.helper.Translation.Get("birthday.spouse")));
            Game1.drawDialogue(__instance);
            __result = true;
            return false;
        }
        catch (Exception ex)
        {
            instance?.monitor.Log($"The spouse's birthday greeting failed.\n{ex}", LogLevel.Error);
            return true;
        }
    }

    /*********
    ** Asking for the date
    *********/
    private void SendAskLetterIfDue(Farmer player)
    {
        uint today = Game1.stats.DaysPlayed;
        if (player.modData.TryGetValue(AskAgainKey, out string? raw) && uint.TryParse(raw, out uint due) && today < due)
            return;

        if (!player.mailbox.Contains(AskMailId))
            player.mailbox.Add(AskMailId);
        player.modData[AskAgainKey] = (today + AskAgainDays).ToString();
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (!this.config().PlayerBirthday || !Context.IsWorldReady)
            return;

        // Lewis's letter closed: ask for the date (not when re-reading it from the collection)
        if (e.OldMenu is LetterViewerMenu { mailTitle: AskMailId, isFromCollection: false } && e.NewMenu is null && GetBirthday(Game1.player) is null)
            this.AskSeason();

        if (e.NewMenu is Billboard { calendarDays: not null } calendar)
            AddToCalendar(calendar);
    }

    private void AskSeason()
    {
        var responses = Seasons
            .Select((season, i) => new Response(season, Utility.getSeasonNameFromNumber(i)))
            .Append(new Response("later", this.helper.Translation.Get("birthday.later")))
            .ToArray();
        this.Ask(this.helper.Translation.Get("birthday.askSeason"), responses, answer =>
        {
            if (answer != "later")
                this.AskWeek(answer);
        });
    }

    private void AskWeek(string season)
    {
        var responses = Enumerable.Range(0, 4)
            .Select(week => new Response(week.ToString(), this.helper.Translation.Get("birthday.week", new { from = week * 7 + 1, to = week * 7 + 7 })))
            .Append(new Response("back", this.helper.Translation.Get("birthday.back")))
            .ToArray();
        this.Ask(this.helper.Translation.Get("birthday.askWeek"), responses, answer =>
        {
            if (answer == "back")
                this.AskSeason();
            else
                this.AskDay(season, int.Parse(answer));
        });
    }

    private void AskDay(string season, int week)
    {
        var responses = Enumerable.Range(week * 7 + 1, 7)
            .Select(day => new Response(day.ToString(), this.helper.Translation.Get("birthday.dayOption", new { weekday = Game1.shortDayDisplayNameFromDayOfSeason(day), day })))
            .Append(new Response("back", this.helper.Translation.Get("birthday.back")))
            .ToArray();
        this.Ask(this.helper.Translation.Get("birthday.askDay"), responses, answer =>
        {
            if (answer == "back")
                this.AskWeek(season);
            else
                this.Confirm(season, int.Parse(answer));
        });
    }

    private void Confirm(string season, int day)
    {
        string question = this.helper.Translation.Get("birthday.confirm", new { season = SeasonName(season), day });
        this.Ask(question, Game1.currentLocation.createYesNoResponses(), answer =>
        {
            if (answer != "Yes")
            {
                this.AskSeason();
                return;
            }

            Farmer player = Game1.player;
            player.modData[BirthdayKey] = $"{season}/{day}";
            Game1.addHUDMessage(new HUDMessage(this.helper.Translation.Get("birthday.saved"), HUDMessage.newQuest_type));
            this.monitor.Log($"Birthday set to {season} {day}.", LogLevel.Trace);

            // picked today: celebrate now rather than next year
            if (Utility.getSeasonNumber(season) == Game1.seasonIndex && day == Game1.dayOfMonth)
                this.Celebrate(player);
        });
    }

    /// <summary>Show a question, and handle the answer once its box has closed.</summary>
    /// <remarks>The game clears the location's answer callback right after calling it, so a follow-up question opened inside the callback would lose its own.</remarks>
    private void Ask(string question, Response[] responses, Action<string> onAnswer)
    {
        Game1.currentLocation.createQuestionDialogue(question, responses, (_, answer) => Game1.afterDialogues = () => onAnswer(answer));
    }

    /*********
    ** Calendar
    *********/
    /// <summary>Show every online player's birthday this season on the calendar, with a cake.</summary>
    private static void AddToCalendar(Billboard calendar)
    {
        ParsedItemData cake = ItemRegistry.GetDataOrErrorItem("(O)221");
        foreach (Farmer farmer in Game1.getOnlineFarmers())
        {
            if (GetBirthday(farmer) is not var (season, day) || season != Game1.season)
                continue;

            string name = farmer.Name;
            bool endsInS = name.EndsWith("s", StringComparison.OrdinalIgnoreCase);
            string label = Game1.content.LoadString(endsInS ? "Strings\\UI:Billboard_SBirthday" : "Strings\\UI:Billboard_Birthday", name);
            var birthday = new Billboard.BillboardEvent(Billboard.BillboardEventType.Birthday, new[] { name }, label, cake.GetTexture(), cake.GetSourceRect());

            // first event's picture is the one drawn, so the cake goes first
            Billboard.BillboardEvent[] existing = calendar.calendarDayData.TryGetValue(day, out Billboard.BillboardDay? current) ? current.Events : Array.Empty<Billboard.BillboardEvent>();
            calendar.calendarDayData[day] = new Billboard.BillboardDay(existing.Prepend(birthday).ToArray());
        }
    }

    /*********
    ** Helpers
    *********/
    /// <summary>A player's birthday, if they've given one.</summary>
    private static (Season Season, int Day)? GetBirthday(Farmer player)
    {
        if (!player.modData.TryGetValue(BirthdayKey, out string? raw))
            return null;
        string[] parts = raw.Split('/');
        if (parts.Length == 2 && Utility.TryParseEnum(parts[0], out Season season) && int.TryParse(parts[1], out int day) && day is >= 1 and <= 28)
            return (season, day);
        return null;
    }

    private static string SeasonName(string season)
    {
        return Utility.getSeasonNameFromNumber(Utility.getSeasonNumber(season));
    }

    private void OnCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.monitor.Log("Load a save first.", LogLevel.Error);
            return;
        }

        Farmer player = Game1.player;
        switch (args.FirstOrDefault())
        {
            case null:
                this.monitor.Log(GetBirthday(player) is var (season, day) ? $"Birthday: {season} {day}." : "No birthday set.", LogLevel.Info);
                break;

            case "set" when args.Length == 3 && Seasons.Contains(args[1].ToLowerInvariant()) && int.TryParse(args[2], out int newDay) && newDay is >= 1 and <= 28:
                player.modData[BirthdayKey] = $"{args[1].ToLowerInvariant()}/{newDay}";
                player.modData.Remove(CelebratedYearKey);
                this.monitor.Log($"Birthday set to {args[1]} {newDay}.", LogLevel.Info);
                break;

            case "reset":
                player.modData.Remove(BirthdayKey);
                player.modData.Remove(CelebratedYearKey);
                player.modData.Remove(AskAgainKey);
                this.SendAskLetterIfDue(player);
                this.monitor.Log("Birthday cleared; Lewis's letter is in the mailbox.", LogLevel.Info);
                break;

            default:
                this.monitor.Log("Usage: sdvpp_birthday [set <season> <day> | reset]", LogLevel.Error);
                break;
        }
    }
}
