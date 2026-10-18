using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using SDVPP.Features;
using SDVPP.Integrations;

namespace SDVPP;

internal sealed class ModEntry : Mod
{
    private ModConfig config = new();

    public override void Entry(IModHelper helper)
    {
        this.config = helper.ReadConfig<ModConfig>();

        var harmony = new Harmony(this.ModManifest.UniqueID);
        _ = new PetBowlSprinklers(helper, this.Monitor, () => this.config);
        _ = new WarpConfirm(helper, this.Monitor, () => this.config, harmony);
        _ = new DecorTimeFreeze(helper, this.Monitor, () => this.config);
        _ = new WeddingReminder(helper, this.Monitor, () => this.config);
        _ = new Birthday(helper, this.Monitor, () => this.config, harmony);
        _ = new WoodShelter(helper, this.Monitor, () => this.config);
        _ = new TackleStacking(this.Monitor, () => this.config, harmony);
        _ = new FastTitle(helper, () => this.config);
        _ = new UiScale(helper, () => this.config, harmony);
        _ = new KeyboardMenus(helper, () => this.config);
        _ = new FastFades(helper, () => this.config);
        _ = new AccidentGuards(this.Monitor, () => this.config, harmony);
        _ = new RangePreview(helper, () => this.config);
        _ = new ShelterRobin(helper, this.Monitor, () => this.config, harmony);
        _ = new RailTiles(helper, this.Monitor);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.Monitor.Log(this.Helper.Translation.Get("loaded"), LogLevel.Info);
        this.RegisterConfigMenu();
    }

    /// <summary>Add the config options to Generic Mod Config Menu, if it's installed.</summary>
    private void RegisterConfigMenu()
    {
        var gmcm = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (gmcm is null)
            return;

        var t = this.Helper.Translation;
        gmcm.Register(
            this.ModManifest,
            reset: () => this.config = new ModConfig(),
            save: () => this.Helper.WriteConfig(this.config)
        );

        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.SprinklersFillPetBowl,
            setValue: value => this.config.SprinklersFillPetBowl = value,
            name: () => t.Get("config.SprinklersFillPetBowl.name"),
            tooltip: () => t.Get("config.SprinklersFillPetBowl.tooltip")
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.ConfirmWarpTotems,
            setValue: value => this.config.ConfirmWarpTotems = value,
            name: () => t.Get("config.ConfirmWarpTotems.name"),
            tooltip: () => t.Get("config.ConfirmWarpTotems.tooltip")
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.ConfirmReturnScepter,
            setValue: value => this.config.ConfirmReturnScepter = value,
            name: () => t.Get("config.ConfirmReturnScepter.name"),
            tooltip: () => t.Get("config.ConfirmReturnScepter.tooltip")
        );
        gmcm.AddTextOption(
            this.ModManifest,
            getValue: () => this.config.DecoratingTimeFreeze.ToString(),
            setValue: value => this.config.DecoratingTimeFreeze = Enum.Parse<DecorFreezeMode>(value),
            name: () => t.Get("config.DecoratingTimeFreeze.name"),
            tooltip: () => t.Get("config.DecoratingTimeFreeze.tooltip"),
            allowedValues: Enum.GetNames<DecorFreezeMode>(),
            formatAllowedValue: value => t.Get($"config.DecoratingTimeFreeze.{value}")
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.WeddingReminder,
            setValue: value => this.config.WeddingReminder = value,
            name: () => t.Get("config.WeddingReminder.name"),
            tooltip: () => t.Get("config.WeddingReminder.tooltip")
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.PlayerBirthday,
            setValue: value => this.config.PlayerBirthday = value,
            name: () => t.Get("config.PlayerBirthday.name"),
            tooltip: () => t.Get("config.PlayerBirthday.tooltip")
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.WoodShelterStorage,
            setValue: value => this.config.WoodShelterStorage = value,
            name: () => t.Get("config.WoodShelterStorage.name"),
            tooltip: () => t.Get("config.WoodShelterStorage.tooltip")
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.CraftWithShelterWood,
            setValue: value => this.config.CraftWithShelterWood = value,
            name: () => t.Get("config.CraftWithShelterWood.name"),
            tooltip: () => t.Get("config.CraftWithShelterWood.tooltip")
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.BuildWithShelterWood,
            setValue: value => this.config.BuildWithShelterWood = value,
            name: () => t.Get("config.BuildWithShelterWood.name"),
            tooltip: () => t.Get("config.BuildWithShelterWood.tooltip")
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.StackUnusedTackle,
            setValue: value => this.config.StackUnusedTackle = value,
            name: () => t.Get("config.StackUnusedTackle.name"),
            tooltip: () => t.Get("config.StackUnusedTackle.tooltip")
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.FastTitleScreen,
            setValue: value => this.config.FastTitleScreen = value,
            name: () => t.Get("config.FastTitleScreen.name"),
            tooltip: () => t.Get("config.FastTitleScreen.tooltip")
        );
        gmcm.AddTextOption(
            this.ModManifest,
            getValue: () => this.config.TitleSlideMilliseconds.ToString(),
            setValue: value => this.config.TitleSlideMilliseconds = int.Parse(value),
            name: () => t.Get("config.TitleSlideMilliseconds.name"),
            tooltip: () => t.Get("config.TitleSlideMilliseconds.tooltip"),
            allowedValues: new[] { "0", "400", "700", "1000" },
            formatAllowedValue: value => value == "0" ? t.Get("config.TitleSlideMilliseconds.vanilla") : t.Get("config.TitleSlideMilliseconds.ms", new { ms = value })
        );
        gmcm.AddTextOption(
            this.ModManifest,
            getValue: () => this.config.TitleUiScale.ToString(),
            setValue: value => this.config.TitleUiScale = int.Parse(value),
            name: () => t.Get("config.TitleUiScale.name"),
            tooltip: () => t.Get("config.TitleUiScale.tooltip"),
            allowedValues: new[] { "0", "100", "125", "150", "175", "200" },
            formatAllowedValue: value => value == "0" ? t.Get("config.TitleUiScale.auto") : value + "%"
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.ExtendedScaleRanges,
            setValue: value => this.config.ExtendedScaleRanges = value,
            name: () => t.Get("config.ExtendedScaleRanges.name"),
            tooltip: () => t.Get("config.ExtendedScaleRanges.tooltip")
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.KeyboardMenus,
            setValue: value => this.config.KeyboardMenus = value,
            name: () => t.Get("config.KeyboardMenus.name"),
            tooltip: () => t.Get("config.KeyboardMenus.tooltip")
        );
        gmcm.AddTextOption(
            this.ModManifest,
            getValue: () => this.config.FadeSpeed.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture),
            setValue: value => this.config.FadeSpeed = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
            name: () => t.Get("config.FadeSpeed.name"),
            tooltip: () => t.Get("config.FadeSpeed.tooltip"),
            allowedValues: new[] { "1", "1.5", "2", "3" },
            formatAllowedValue: value => value == "1" ? t.Get("config.FadeSpeed.vanilla") : t.Get("config.FadeSpeed.times", new { speed = value })
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.NoEatPromptOnCrops,
            setValue: value => this.config.NoEatPromptOnCrops = value,
            name: () => t.Get("config.NoEatPromptOnCrops.name"),
            tooltip: () => t.Get("config.NoEatPromptOnCrops.tooltip")
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.NoGiftsThroughDoors,
            setValue: value => this.config.NoGiftsThroughDoors = value,
            name: () => t.Get("config.NoGiftsThroughDoors.name"),
            tooltip: () => t.Get("config.NoGiftsThroughDoors.tooltip")
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.RangePreview,
            setValue: value => this.config.RangePreview = value,
            name: () => t.Get("config.RangePreview.name"),
            tooltip: () => t.Get("config.RangePreview.tooltip")
        );
    }
}
