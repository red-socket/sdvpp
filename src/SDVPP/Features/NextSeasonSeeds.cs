using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Shops;

namespace SDVPP.Features;

/// <summary>On the last day of a season, Pierre and JojaMart also sell the next season's seeds.</summary>
/// <remarks>
/// Edits <c>Data/Shops</c> late, after other mods, and only touches the seed entries it finds: each one whose
/// condition is a plain <c>SEASON x</c> (optionally with <c>YEAR 2</c>) also shows on day 28 of the season before.
/// Entries another mod changed or replaced are simply skipped, so nothing warns. Shop stock is worked out on each
/// player's own machine, so every player's own setting applies to their own shopping.
/// </remarks>
internal sealed class NextSeasonSeeds
{
    private static readonly string[] Shops = { "SeedShop", "Joja" };

    /// <summary>A plain season condition, as vanilla writes it: <c>SEASON spring</c> or <c>SEASON spring, YEAR 2</c>.</summary>
    private static readonly Regex SeasonCondition = new(@"^\s*SEASON\s+(spring|summer|fall|winter)\s*(,\s*YEAR\s+2\s*)?$", RegexOptions.IgnoreCase);

    private static readonly Dictionary<string, string> PreviousSeason = new(StringComparer.OrdinalIgnoreCase)
    {
        ["spring"] = "winter",
        ["summer"] = "spring",
        ["fall"] = "summer",
        ["winter"] = "fall"
    };

    private readonly IMonitor monitor;
    private readonly Func<ModConfig> config;

    public NextSeasonSeeds(IModHelper helper, IMonitor monitor, Func<ModConfig> config)
    {
        this.monitor = monitor;
        this.config = config;
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops") && this.config().NextSeasonSeeds)
            e.Edit(asset => this.Edit(asset.AsDictionary<string, ShopData>().Data), AssetEditPriority.Late);
    }

    private void Edit(IDictionary<string, ShopData> shops)
    {
        int changed = 0;
        foreach (string shopId in Shops)
        {
            if (!shops.TryGetValue(shopId, out ShopData? shop) || shop.Items is null)
                continue;

            // seeds sold in two seasons (corn, wheat, sunflower) have an entry for each; the earlier one already covers day 28
            var soldIn = new HashSet<(string, string)>();
            foreach (ShopItemData item in shop.Items)
            {
                if (item.ItemId is not null && item.Condition is not null && SeasonCondition.Match(item.Condition) is { Success: true } m)
                    soldIn.Add((ItemRegistry.QualifyItemId(item.ItemId) ?? item.ItemId, m.Groups[1].Value.ToLowerInvariant()));
            }

            foreach (ShopItemData item in shop.Items)
            {
                if (item.Condition is null || SeasonCondition.Match(item.Condition) is not { Success: true } match)
                    continue;
                if (item.ItemId is null || ItemRegistry.GetData(item.ItemId)?.Category != StardewValley.Object.SeedsCategory)
                    continue;

                string season = match.Groups[1].Value.ToLowerInvariant();
                bool fromYear2 = match.Groups[2].Success;
                if (soldIn.Contains((ItemRegistry.QualifyItemId(item.ItemId) ?? item.ItemId, PreviousSeason[season])))
                    continue;

                // the day before a year-2 spring is winter 28 of year 1, so only the other seasons keep the year
                string dayBefore = $"SEASON_DAY {PreviousSeason[season]} 28" + (fromYear2 && season != "spring" ? ", YEAR 2" : "");
                item.Condition = $"ANY \"{item.Condition.Trim()}\" \"{dayBefore}\"";
                changed++;
            }
        }

        this.monitor.Log($"Next season's seeds: {changed} shop entries also show on day 28.", LogLevel.Trace);
    }
}
