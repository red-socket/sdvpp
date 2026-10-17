using System;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace SDVPP.Features;

/// <summary>Control menus with the keyboard: arrows move the selection, Enter or Space clicks it, Backspace right-clicks.</summary>
/// <remarks>
/// Client-only, per screen. Vanilla only moves a menu's selection in gamepad mode (with WASD), so this briefly turns
/// gamepad navigation on and sends the menu its own movement key, reusing each menu's controller support. Also: number
/// keys answer question dialogues, and Escape leaves the title screen's sub-menus.
/// </remarks>
internal sealed class KeyboardMenus
{
    /// <summary>Delay before a held arrow repeats, and the time between repeats, in milliseconds (vanilla's values for held movement keys).</summary>
    private const int RepeatDelay = 250, RepeatInterval = 70;

    private readonly IModHelper helper;
    private readonly Func<ModConfig> config;

    /// <summary>The arrow key being held for auto-repeat, if any.</summary>
    private readonly PerScreen<SButton?> heldArrow = new();

    /// <summary>When the held arrow next repeats, in game time milliseconds.</summary>
    private readonly PerScreen<double> nextRepeat = new();

    public KeyboardMenus(IModHelper helper, Func<ModConfig> config)
    {
        this.helper = helper;
        this.config = config;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!this.config().KeyboardMenus || Game1.activeClickableMenu is not IClickableMenu root || IsTyping())
            return;

        IClickableMenu menu = Deepest(root);
        SButton button = e.Button;

        // question dialogues: number keys pick an answer
        if (menu is DialogueBox { isQuestion: true, transitioning: false } box && TryGetNumber(button, out int number)
            && number <= box.responses.Length && box.characterIndexInDialogue >= box.getCurrentString().Length)
        {
            this.helper.Input.Suppress(button);
            box.safetyTimer = 0;
            box.selectedResponse = number - 1;
            box.receiveLeftClick(0, 0);
            return;
        }

        // title screen: Escape leaves the Load / Co-op / New menus, which vanilla only allows with a gamepad
        if (button == SButton.Escape && root is TitleMenu title && TitleMenu.subMenu is { } subMenu)
        {
            this.helper.Input.Suppress(button);
            if (subMenu is LoadGameMenu { deleteConfirmationScreen: true } load)
            {
                load.deleteConfirmationScreen = false;
                load.selectedForDelete = -1;
                Game1.playSound("smallSelect");
            }
            else if (subMenu is not CharacterCustomization { showingCoopHelp: true })
                title.backButtonPressed();
            return;
        }

        switch (button)
        {
            case SButton.Up or SButton.Down or SButton.Left or SButton.Right:
                this.helper.Input.Suppress(button);
                Move(root, button);
                this.heldArrow.Value = button;
                this.nextRepeat.Value = Game1.currentGameTime.TotalGameTime.TotalMilliseconds + RepeatDelay;
                break;

            case SButton.Enter or SButton.Space:
                // only click a selection the player moved to, or advance a dialogue; leave other Enter uses alone
                if (!HasSelection(menu) && menu is not DialogueBox)
                    return;
                this.helper.Input.Suppress(button);
                InUiMode(() =>
                {
                    menu.receiveLeftClick(Game1.getMouseX(), Game1.getMouseY());
                    menu.releaseLeftClick(Game1.getMouseX(), Game1.getMouseY());
                });
                break;

            case SButton.Back:
                if (!HasSelection(menu))
                    return;
                this.helper.Input.Suppress(button);
                InUiMode(() => menu.receiveRightClick(Game1.getMouseX(), Game1.getMouseY()));
                break;
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (this.heldArrow.Value is not SButton arrow)
            return;

        if (Game1.activeClickableMenu is not IClickableMenu root || IsTyping() || !this.helper.Input.IsDown(arrow))
        {
            this.heldArrow.Value = null;
            return;
        }

        double now = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
        if (now >= this.nextRepeat.Value)
        {
            Move(root, arrow);
            this.nextRepeat.Value = now + RepeatInterval;
        }
    }

    /// <summary>Move the menu's selection by sending it the movement key it expects, with gamepad navigation briefly on.</summary>
    /// <remarks>The innermost menu routes the key itself (the game menu to its tab, the title screen to its sub-menu).</remarks>
    private static void Move(IClickableMenu root, SButton arrow)
    {
        InputButton[] binding = arrow switch
        {
            SButton.Up => Game1.options.moveUpButton,
            SButton.Down => Game1.options.moveDownButton,
            SButton.Left => Game1.options.moveLeftButton,
            _ => Game1.options.moveRightButton
        };
        if (binding.Length == 0 || binding[0].key == Keys.None)
            return;

        IClickableMenu target = Deepest(root);
        bool gamepadControls = Game1.options.gamepadControls, snappyMenus = Game1.options.snappyMenus;
        Game1.options.gamepadControls = true;
        Game1.options.snappyMenus = true;
        try
        {
            InUiMode(() =>
            {
                target.populateClickableComponentList();
                target.receiveKeyPress(binding[0].key);
            });
        }
        finally
        {
            Game1.options.gamepadControls = gamepadControls;
            Game1.options.snappyMenus = snappyMenus;
        }
    }

    /// <summary>Whether the menu has a selected component to click.</summary>
    /// <remarks>The game menu and the title screen pass input on, and their tab page or sub-menu holds the selection.</remarks>
    private static bool HasSelection(IClickableMenu menu)
    {
        IClickableMenu owner = menu switch
        {
            GameMenu gameMenu => gameMenu.GetCurrentPage(),
            TitleMenu title => TitleMenu.subMenu ?? title,
            _ => menu
        };
        return owner.currentlySnappedComponent != null || menu.currentlySnappedComponent != null;
    }

    /// <summary>Run code in UI coordinates, as menus expect; input events run in world coordinates.</summary>
    private static void InUiMode(Action action)
    {
        Game1.PushUIMode();
        try
        {
            action();
        }
        finally
        {
            Game1.PopUIMode();
        }
    }

    /// <summary>The innermost child menu, which receives input.</summary>
    private static IClickableMenu Deepest(IClickableMenu menu)
    {
        while (menu.GetChildMenu() is { } child)
            menu = child;
        return menu;
    }

    /// <summary>Whether a text box or the chat box has keyboard focus.</summary>
    private static bool IsTyping()
    {
        return Game1.keyboardDispatcher?.Subscriber != null || Game1.chatBox?.isActive() == true;
    }

    private static bool TryGetNumber(SButton button, out int number)
    {
        number = button switch
        {
            >= SButton.D1 and <= SButton.D9 => button - SButton.D1 + 1,
            >= SButton.NumPad1 and <= SButton.NumPad9 => button - SButton.NumPad1 + 1,
            _ => 0
        };
        return number > 0;
    }
}
