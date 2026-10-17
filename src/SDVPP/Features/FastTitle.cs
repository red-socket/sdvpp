using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace SDVPP.Features;

/// <summary>Skip the title screen's intro and speed up its transitions.</summary>
/// <remarks>Client-only. The title screen exists only on the main screen, before any save is loaded.</remarks>
internal sealed class FastTitle
{
    /// <summary>How much faster the flash-and-fade after picking a save runs (vanilla: 2.15 s).</summary>
    private const int LoadDelaySpeedup = 5;

    private readonly Func<ModConfig> config;

    /// <summary>The title menu whose intro was already skipped.</summary>
    private TitleMenu? skippedTitle;

    public FastTitle(IModHelper helper, Func<ModConfig> config)
    {
        this.config = config;

        // the first title menu is created after Entry and before GameLaunched; this skips the logo and white fade
        if (config().FastTitleScreen)
            TitleMenu.SkipSplashScreens = true;

        helper.Events.GameLoop.UpdateTicking += this.OnUpdateTicking;
    }

    private void OnUpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        if (!this.config().FastTitleScreen || Game1.activeClickableMenu is not TitleMenu title)
            return;

        // logo and buttons: show at once instead of panning up and popping the buttons in one by one
        if (!ReferenceEquals(title, this.skippedTitle) && !title.titleInPosition)
        {
            this.skippedTitle = title;
            title.viewportY = -1000f;
            title.viewportDY = 0f; // both together, or the update re-arms the logo swipe
            title.buttonsToShow = 4;
            title.showButtonsTimer = 0;
            title.logoSwipeTimer = 1f; // the next update puts the title in position and sets up its click areas
        }

        // slide to and from New/Load/Co-op: vanilla moves 1 px per ms, so it takes longer the wider the screen
        int slideMs = this.config().TitleSlideMilliseconds;
        if (slideMs > 0 && title.isTransitioningButtons && Math.Abs(title.buttonsDX) == 1)
            title.buttonsDX = Math.Sign(title.buttonsDX) * Math.Max(1, (int)Math.Round(title.width / (double)slideMs));

        // flash and fade after picking a save (vanilla ticks timerToLoad down itself, and loads when it reaches 0)
        if (TitleMenu.subMenu is LoadGameMenu load && load.timerToLoad > 1)
        {
            int extra = (int)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds * (LoadDelaySpeedup - 1);
            load.timerToLoad = Math.Max(1, load.timerToLoad - extra);
        }
    }
}
