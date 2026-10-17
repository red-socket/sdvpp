using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;

namespace SDVPP.Features;

/// <summary>The wood shelter beside the farmhouse door stores Wood and Hardwood.</summary>
/// <remarks>
/// The shelter is the farmhouse building's two <c>LumberPile</c> action tiles; the content pack points them at
/// <see cref="ActionId"/>. Storage is a team-wide global inventory: synced to every player and saved by the host,
/// with the host-granted mutex making sure only one player edits it at a time.
/// </remarks>
internal sealed class WoodShelter
{
    /// <summary>The tile action set on the farmhouse's lumber pile tiles by the content pack.</summary>
    public const string ActionId = "Luke.SDVPP_WoodShelter";

    /// <summary>The team global inventory holding the stored wood.</summary>
    private const string InventoryId = "Luke.SDVPP_WoodShelter";

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Func<ModConfig> config;

    /// <summary>Whether this screen has shown the shelter menu and still holds the lock for it.</summary>
    private readonly PerScreen<bool> menuShown = new();

    public WoodShelter(IModHelper helper, IMonitor monitor, Func<ModConfig> config)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.config = config;

        GameLocation.RegisterTileAction(ActionId, this.OnShelterAction);
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
    }

    /// <summary>Whether an item can be stored in the shelter.</summary>
    private static bool IsWood(Item? item)
    {
        return item?.QualifiedItemId is "(O)388" or "(O)709";
    }

    private static NetMutex GetMutex()
    {
        return Game1.player.team.GetOrCreateGlobalInventoryMutex(InventoryId);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // create the shared entries on the host before any farmhand can race to create them
        if (Context.IsMainPlayer)
        {
            Game1.player.team.GetOrCreateGlobalInventory(InventoryId);
            GetMutex();
        }
    }

    private bool OnShelterAction(GameLocation location, string[] args, Farmer who, Point tile)
    {
        // keep vanilla's lumber pile: the last step of Mr. Qi's hunt gives the Club Card here
        bool clubCardStep = who.hasOrWillReceiveMail("TH_SandDragon") && !who.hasOrWillReceiveMail("TH_LumberPile");
        if (!this.config().WoodShelterStorage || clubCardStep)
            return location.performAction(new[] { "LumberPile" }, who, new xTile.Dimensions.Location(tile.X, tile.Y));

        GetMutex().RequestLock(
            acquired: () =>
            {
                this.monitor.Log("Opened the wood shelter.", LogLevel.Trace);
                Game1.playSound("woodyStep");
                new ShelterStorage(this).ShowMenu();
            },
            failed: () =>
            {
                this.monitor.Log("Wood shelter is in use by another player.", LogLevel.Trace);
                Game1.showRedMessage(this.helper.Translation.Get("woodShelter.inUse"));
            }
        );
        return true;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        // fallback for a menu closed without its exit function (e.g. emergency shutdown), like vanilla chests do.
        // Only after the menu was shown: the host gets the lock at once, but the menu opens on the next team update.
        if (!this.menuShown.Value || Game1.activeClickableMenu is ItemGrabMenu { context: ShelterStorage })
            return;
        this.menuShown.Value = false;

        // (look up without creating, so a farmhand never adds the entry before the host's copy syncs)
        if (Context.IsWorldReady && Game1.player.team.globalInventoryMutexes.TryGetValue(InventoryId, out NetMutex mutex) && mutex.IsLockHeld())
            mutex.ReleaseLock();
    }

    /// <summary>A chest that is never placed, used only to show and edit the shelter's inventory.</summary>
    private sealed class ShelterStorage : Chest
    {
        private readonly WoodShelter feature;

        public ShelterStorage(WoodShelter feature)
            : base(playerChest: true)
        {
            this.feature = feature;
            this.GlobalInventoryId = InventoryId;
        }

        /// <inheritdoc />
        /// <remarks>Vanilla calls this again after every transfer, so each new menu gets the filter and exit function.</remarks>
        public override void ShowMenu()
        {
            this.feature.menuShown.Value = true;
            Game1.activeClickableMenu = new ItemGrabMenu(
                inventory: this.GetItemsForPlayer(),
                reverseGrab: false,
                showReceivingMenu: true,
                highlightFunction: IsWood,
                behaviorOnItemSelectFunction: this.grabItemFromInventory,
                message: null,
                behaviorOnItemGrab: this.grabItemFromChest,
                canBeExitedWithKey: true,
                showOrganizeButton: true,
                source: ItemGrabMenu.source_none, // no colour picker or chest icon
                sourceItem: this,
                context: this
            )
            {
                exitFunction = () =>
                {
                    this.feature.menuShown.Value = false;
                    GetMutex().ReleaseLock();
                }
            };
        }

        /// <inheritdoc />
        public override Item? addItem(Item item)
        {
            // the menu filter already blocks other items; this guards any other path
            return IsWood(item) ? base.addItem(item) : item;
        }
    }
}
