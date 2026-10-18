using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SDVPP.Features;

/// <summary>Provide minecart rails with a transparent background, so the content pack can lay them over sand.</summary>
/// <remarks>
/// Every rail tile in the game has dirt or grass under it. This builds a two-tile sheet (straight track, track end)
/// at load time from the player's own outdoors tilesheet: the rail and sleeper colours are the ones the dirt and grass
/// versions of the straight track have in common, and every other pixel becomes transparent. No game art is shipped.
/// The content pack's Beach stop puts these tiles on the map's Back2 layer, above the vanilla sand. Client-only.
/// </remarks>
internal sealed class RailTiles
{
    /// <summary>The asset name the content pack's map fragment references as its tilesheet image.</summary>
    private const string AssetName = "Maps/z_SDVPP_Rails";

    /// <summary>The vanilla tilesheet the rails come from. Spring's colours suit sand in every season.</summary>
    private const string SourceSheet = "Maps/spring_outdoorsTileSheet";

    /// <summary>Tile indices in <see cref="SourceSheet"/>: straight track on dirt, straight track on grass, track end on grass.</summary>
    private const int TrackOnDirt = 459, TrackOnGrass = 1076, EndOnGrass = 1075;

    private const int TileSize = 16;

    private readonly IModHelper helper;
    private readonly IMonitor monitor;

    public RailTiles(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // always provided, even if building it fails: a map with a missing tilesheet can't load at all
        if (e.NameWithoutLocale.IsEquivalentTo(AssetName))
            e.LoadFrom(this.Build, AssetLoadPriority.Exclusive);
    }

    /// <summary>Build the sheet: tile 0 is a straight track, tile 1 the end of a track.</summary>
    private Texture2D Build()
    {
        var sheet = new Texture2D(Game1.graphics.GraphicsDevice, TileSize * 2, TileSize);
        try
        {
            var source = this.helper.GameContent.Load<Texture2D>(SourceSheet);
            var railColours = new HashSet<Color>(GetTile(source, TrackOnDirt));
            railColours.IntersectWith(GetTile(source, TrackOnGrass));

            Color[] Keep(Color[] tile)
            {
                for (int i = 0; i < tile.Length; i++)
                {
                    if (!railColours.Contains(tile[i]))
                        tile[i] = Color.Transparent;
                }
                return tile;
            }

            sheet.SetData(0, new Rectangle(0, 0, TileSize, TileSize), Keep(GetTile(source, TrackOnGrass)), 0, TileSize * TileSize);
            sheet.SetData(0, new Rectangle(TileSize, 0, TileSize, TileSize), Keep(GetTile(source, EndOnGrass)), 0, TileSize * TileSize);
        }
        catch (System.Exception ex)
        {
            this.monitor.Log($"Couldn't build the minecart rail tiles; the Beach stop will have no rails.\n{ex}", LogLevel.Error);
        }
        return sheet;
    }

    private static Color[] GetTile(Texture2D sheet, int index)
    {
        int columns = sheet.Width / TileSize;
        var area = new Rectangle(index % columns * TileSize, index / columns * TileSize, TileSize, TileSize);
        var data = new Color[TileSize * TileSize];
        sheet.GetData(0, area, data, 0, data.Length);
        return data;
    }
}
