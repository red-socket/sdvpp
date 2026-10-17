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
        _ = new WoodShelter(helper, this.Monitor, () => this.config);
        _ = new TackleStacking(this.Monitor, () => this.config, harmony);

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
            getValue: () => this.config.WoodShelterStorage,
            setValue: value => this.config.WoodShelterStorage = value,
            name: () => t.Get("config.WoodShelterStorage.name"),
            tooltip: () => t.Get("config.WoodShelterStorage.tooltip")
        );
        gmcm.AddBoolOption(
            this.ModManifest,
            getValue: () => this.config.StackUnusedTackle,
            setValue: value => this.config.StackUnusedTackle = value,
            name: () => t.Get("config.StackUnusedTackle.name"),
            tooltip: () => t.Get("config.StackUnusedTackle.tooltip")
        );
    }
}
