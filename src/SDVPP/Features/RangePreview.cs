using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace SDVPP.Features;

/// <summary>Show the area a sprinkler, scarecrow, bee house or Junimo hut covers, while holding or placing one and when pointing at a placed one.</summary>
/// <remarks>Client-only, drawing only.</remarks>
internal sealed class RangePreview
{
    /// <summary>The bee house, whose honey takes the flavor of a flower within this Manhattan distance.</summary>
    private const string BeeHouseId = "(BC)10";
    private const int BeeHouseRange = 5;

    /// <summary>Junimo huts harvest a square this far from the tile below their door's left.</summary>
    private const int JunimoHutRadius = 8;

    /// <summary>The game's green placement tile.</summary>
    private static readonly Rectangle TileSprite = new(194, 388, 16, 16);

    /// <summary>The build menu's current action (build, move...). Read by name, because the Android version of the game has no such field and a direct reference would stop the whole mod from loading there.</summary>
    private static readonly FieldInfo? CarpenterActionField = AccessTools.Field(typeof(CarpenterMenu), "Action");

    private readonly Func<ModConfig> config;

    /// <summary>Tile offsets per shape, built once.</summary>
    private readonly Dictionary<string, List<Point>> shapes = new();

    public RangePreview(IModHelper helper, Func<ModConfig> config)
    {
        this.config = config;
        helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady || !this.config().RangePreview)
            return;

        GameLocation location = Game1.currentLocation;

        // placing a Junimo hut from the Wizard's book
        if (Game1.activeClickableMenu is CarpenterMenu { onFarm: true, freeze: false } carpenter)
        {
            Building? hut = CarpenterActionField?.GetValue(carpenter) switch
            {
                CarpenterMenu.CarpentryAction.None => carpenter.currentBuilding,
                CarpenterMenu.CarpentryAction.Move => carpenter.buildingToMove,
                _ => null
            };
            if (hut is JunimoHut)
            {
                Point topLeft = new((Game1.viewport.X + Game1.getOldMouseX(ui_scale: false)) / 64, (Game1.viewport.Y + Game1.getOldMouseY(ui_scale: false)) / 64);
                this.Draw(e.SpriteBatch, new Point(topLeft.X + 1, topLeft.Y + 1), this.Square(JunimoHutRadius));
            }
            return;
        }
        if (Game1.activeClickableMenu != null)
            return;

        // holding one: where it would be placed
        SObject? held = Game1.player.ActiveObject;
        if (held != null && this.GetShape(held) is { } heldShape)
        {
            this.Draw(e.SpriteBatch, GetPlacementTile(held, location), heldShape);
            return;
        }

        // pointing at a placed one
        Vector2 tile = Game1.wasMouseVisibleThisFrame ? Game1.currentCursorTile : Game1.player.GetGrabTile();
        if (location.objects.TryGetValue(tile, out SObject placed) && this.GetShape(placed) is { } placedShape)
            this.Draw(e.SpriteBatch, new Point((int)tile.X, (int)tile.Y), placedShape);
        else if (location.getBuildingAt(tile) is JunimoHut placedHut)
            this.Draw(e.SpriteBatch, new Point(placedHut.tileX.Value + 1, placedHut.tileY.Value + 1), this.Square(JunimoHutRadius));
    }

    /// <summary>The tile offsets an item covers, or null if it has no range.</summary>
    private List<Point>? GetShape(SObject obj)
    {
        if (obj.IsSprinkler())
        {
            int radius = obj.GetModifiedRadiusForSprinkler();
            return radius == 0 ? this.Cross() : this.Square(radius);
        }
        if (obj.IsScarecrow())
            return this.Disc(obj.GetRadiusForScarecrow());
        if (obj.QualifiedItemId == BeeHouseId)
            return this.Diamond(BeeHouseRange);
        return null;
    }

    /// <summary>The tile the held item would be placed on, worked out the way the game draws its placement tile.</summary>
    private static Point GetPlacementTile(SObject held, GameLocation location)
    {
        bool nonMouse = !Game1.IsPerformingMousePlacement();
        Game1.isCheckingNonMousePlacement = nonMouse;
        try
        {
            Vector2 grab = Game1.GetPlacementGrabTile();
            int x = (int)grab.X * 64, y = (int)grab.Y * 64;
            if (nonMouse)
            {
                Vector2 nearby = Utility.GetNearbyValidPlacementPosition(Game1.player, location, held, x, y);
                x = (int)nearby.X;
                y = (int)nearby.Y;
            }
            return new Point(x / 64, y / 64);
        }
        finally
        {
            Game1.isCheckingNonMousePlacement = false;
        }
    }

    private void Draw(SpriteBatch batch, Point center, List<Point> shape)
    {
        Rectangle view = new(Game1.viewport.X / 64 - 1, Game1.viewport.Y / 64 - 1, Game1.viewport.Width / 64 + 3, Game1.viewport.Height / 64 + 3);
        foreach (Point offset in shape)
        {
            Point tile = new(center.X + offset.X, center.Y + offset.Y);
            if (!view.Contains(tile))
                continue;
            Vector2 position = Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64, tile.Y * 64));
            batch.Draw(Game1.mouseCursors, position, TileSprite, Color.White * 0.5f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
        }
    }

    private List<Point> Cross() => this.Shape("cross", 1, (x, y) => Math.Abs(x) + Math.Abs(y) == 1);

    private List<Point> Square(int radius) => this.Shape($"square{radius}", radius, (_, _) => true);

    /// <summary>Scarecrows protect crops strictly closer than their radius.</summary>
    private List<Point> Disc(int radius) => this.Shape($"disc{radius}", radius, (x, y) => Math.Sqrt(x * x + y * y) < radius);

    private List<Point> Diamond(int range) => this.Shape($"diamond{range}", range, (x, y) => (x, y) != (0, 0) && Math.Abs(x) + Math.Abs(y) <= range);

    private List<Point> Shape(string key, int radius, Func<int, int, bool> includes)
    {
        if (!this.shapes.TryGetValue(key, out List<Point>? shape))
        {
            shape = new List<Point>();
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (includes(x, y))
                        shape.Add(new Point(x, y));
                }
            }
            this.shapes[key] = shape;
        }
        return shape;
    }
}
