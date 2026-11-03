using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace SDVPP.Features;

/// <summary>List the content pack's sprinkler upgrade recipes right after the sprinklers they make.</summary>
/// <remarks>
/// The crafting menu shows recipes in <c>Data/CraftingRecipes</c> order, and Content Patcher adds new entries at the
/// end, so the upgrades landed on the menu's last page while the vanilla recipe with the same icon stayed in its usual
/// spot. This only changes the order, so it's client-only; the recipes themselves come from the content pack.
/// </remarks>
internal sealed class SprinklerRecipeOrder
{
    /// <summary>Each upgrade recipe, and the vanilla recipe it goes after.</summary>
    private static readonly Dictionary<string, string> Placement = new()
    {
        ["Luke.SDVPP.Content_QualitySprinklerUpgrade"] = "Quality Sprinkler",
        ["Luke.SDVPP.Content_IridiumSprinklerUpgrade"] = "Iridium Sprinkler"
    };

    public SprinklerRecipeOrder(IModHelper helper)
    {
        helper.Events.Content.AssetRequested += OnAssetRequested;
    }

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // late, so the content pack's recipes are already in
        if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            e.Edit(Reorder, AssetEditPriority.Late);
    }

    private static void Reorder(IAssetData asset)
    {
        IDictionary<string, string> recipes = asset.AsDictionary<string, string>().Data;
        var moving = Placement.Where(p => recipes.ContainsKey(p.Key) && recipes.ContainsKey(p.Value)).ToList();
        if (moving.Count == 0)
            return;

        var ordered = new List<KeyValuePair<string, string>>(recipes.Count);
        foreach (var recipe in recipes)
        {
            if (moving.Any(p => p.Key == recipe.Key))
                continue;
            ordered.Add(recipe);
            foreach (var (upgrade, after) in moving)
            {
                if (after == recipe.Key)
                    ordered.Add(new(upgrade, recipes[upgrade]));
            }
        }

        // a cleared dictionary enumerates in the order entries are added again
        recipes.Clear();
        foreach (var (key, value) in ordered)
            recipes.Add(key, value);
    }
}
