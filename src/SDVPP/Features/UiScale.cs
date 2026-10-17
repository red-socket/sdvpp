using System;
using System.Linq;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace SDVPP.Features;

/// <summary>Use the player's UI scale on the title screen, and allow larger UI scale and zoom settings.</summary>
/// <remarks>
/// Client-only. Before a save is loaded the game forces UI scale to 100%, which makes the title, load, co-op and
/// character creation menus tiny at high resolutions.
/// </remarks>
internal sealed class UiScale
{
    /// <summary>The highest zoom level offered (vanilla: 200%).</summary>
    private const int MaxZoomPercent = 250;

    /// <summary>The smallest screen area, in UI pixels, that still fits the 880x680 game menu and its tabs.</summary>
    private const int MinUiWidth = 1280, MinUiHeight = 720;

    /// <summary>Harmony patches are static, so they reach the feature through this.</summary>
    private static UiScale? instance;

    private readonly Func<ModConfig> config;

    /// <summary>The player's in-game UI scale, last read from their saved options (0 if unknown).</summary>
    private float savedUiScale;

    public UiScale(IModHelper helper, Func<ModConfig> config, Harmony harmony)
    {
        instance = this;
        this.config = config;

        harmony.Patch(
            original: AccessTools.PropertyGetter(typeof(Options), nameof(Options.desiredUIScale)),
            postfix: new HarmonyMethod(typeof(UiScale), nameof(After_DesiredUIScale))
        );

        helper.Events.GameLoop.GameLaunched += (_, _) => this.ReadSavedUiScale();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => this.ReadSavedUiScale();
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.Display.MenuChanged += this.OnMenuChanged;
    }

    /// <summary>The UI scale to use on the title screen, or null to leave the game's 100%.</summary>
    private float? TitleScale
    {
        get
        {
            int percent = this.config().TitleUiScale;
            if (percent > 0)
                return percent / 100f;
            return this.savedUiScale > 0 ? this.savedUiScale : null;
        }
    }

    /// <summary>Read the UI scale the game saved with the player's options (written whenever a save is loaded or exited).</summary>
    private void ReadSavedUiScale()
    {
        // the live options while in a save; otherwise the default_options file (Game1.options is blank at launch)
        float scale = Game1.options?.singlePlayerDesiredUIScale ?? -1f;
        if (scale <= 0)
        {
            var saved = new Options();
            saved.LoadDefaultOptions();
            scale = saved.singlePlayerDesiredUIScale;
        }
        this.savedUiScale = scale;
    }

    /// <summary>Report the title scale while no save is loaded. The game reapplies this every tick.</summary>
    /// <remarks>The value stays constant for the whole title session: a resize while a title menu is open would move it off-centre.</remarks>
    private static void After_DesiredUIScale(ref float __result)
    {
        if (instance is null || Game1.gameMode == Game1.playingGameMode || !Game1.game1.IsMainInstance)
            return;
        if (instance.TitleScale is float scale)
            __result = scale;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        // vanilla places the window-mode button using the world width, which puts it off-screen above 100% UI scale
        if (Game1.activeClickableMenu is TitleMenu { windowedButton: { } button })
            button.bounds.X = Game1.uiViewport.Width - 36 - 16;
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        // the game rebuilds the game menu when the scale changes, so this runs again each time
        if (!this.config().ExtendedScaleRanges || e.NewMenu is not GameMenu gameMenu)
            return;

        foreach (OptionsPage page in gameMenu.pages.OfType<OptionsPage>())
        {
            foreach (OptionsPlusMinus option in page.options.OfType<OptionsPlusMinus>())
            {
                int max = option.whichOption switch
                {
                    Options.uiScaleSlider => MaxUiScalePercent(),
                    Options.zoom => MaxZoomPercent,
                    _ => 0
                };
                if (max == 0)
                    continue;

                // the game uses one list for both values and labels, and parses "175%" without a clamp
                int last = option.options.Select(ParsePercent).DefaultIfEmpty(0).Max();
                for (int percent = last + 5; percent <= max; percent += 5)
                    option.options.Add(percent + "%");
                if (!ReferenceEquals(option.options, option.displayOptions))
                {
                    for (int percent = last + 5; percent <= max; percent += 5)
                        option.displayOptions.Add(percent + "%");
                }
                Game1.options.setPlusMinusToProperValue(option);
            }
        }
    }

    /// <summary>The largest UI scale, in 5% steps, at which the game menu still fits this screen.</summary>
    private static int MaxUiScalePercent()
    {
        float screenWidth = Game1.viewport.Width * Game1.options.zoomLevel;
        float screenHeight = Game1.viewport.Height * Game1.options.zoomLevel;
        float fit = Math.Min(screenWidth / MinUiWidth, screenHeight / MinUiHeight);
        return (int)(fit * 100) / 5 * 5;
    }

    private static int ParsePercent(string value)
    {
        return int.TryParse(value.TrimEnd('%'), out int percent) ? percent : 0;
    }
}
