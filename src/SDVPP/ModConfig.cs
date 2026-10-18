using SDVPP.Features;

namespace SDVPP;

/// <summary>Player settings, saved in the mod's <c>config.json</c>. Each feature has its own toggle.</summary>
internal sealed class ModConfig
{
    /// <summary>Sprinklers that cover the pet bowl's water tile fill it each morning. Uses the host's setting.</summary>
    public bool SprinklersFillPetBowl { get; set; } = true;

    /// <summary>Ask before a warp totem warps you. Each player's own setting applies.</summary>
    public bool ConfirmWarpTotems { get; set; } = true;

    /// <summary>Ask before the Return Scepter warps you home. Each player's own setting applies.</summary>
    public bool ConfirmReturnScepter { get; set; } = true;

    /// <summary>When the clock stops for decorating at home. Uses the host's setting.</summary>
    public DecorFreezeMode DecoratingTimeFreeze { get; set; } = DecorFreezeMode.HoldingFurniture;

    /// <summary>Leave a letter on the day before your wedding. Each player's own setting applies.</summary>
    public bool WeddingReminder { get; set; } = true;

    /// <summary>The wood shelter beside the farmhouse door stores Wood and Hardwood. Each player's own setting applies.</summary>
    public bool WoodShelterStorage { get; set; } = true;

    /// <summary>When crafting at home, ingredients can come from the wood shelter. Each player's own setting applies.</summary>
    public bool CraftWithShelterWood { get; set; } = true;

    /// <summary>Robin's buildings and house upgrades can use Wood and Hardwood from the wood shelter. Each player's own setting applies.</summary>
    public bool BuildWithShelterWood { get; set; } = true;

    /// <summary>Unused fishing tackle stacks. Every player in co-op should have the same setting.</summary>
    public bool StackUnusedTackle { get; set; } = true;

    /// <summary>Skip the title screen intro and speed up its transitions.</summary>
    public bool FastTitleScreen { get; set; } = true;

    /// <summary>How long the title screen's slide to New, Load and Co-op takes, in milliseconds; 0 keeps vanilla (1 px per ms, about 1.7 s at 2560 px and 150% UI).</summary>
    public int TitleSlideMilliseconds { get; set; } = 700;

    /// <summary>UI scale on the title screen, in percent; 0 uses your in-game UI scale (vanilla is always 100%).</summary>
    public int TitleUiScale { get; set; } = 0;

    /// <summary>Offer UI scales up to what fits the screen, and zoom up to 250%.</summary>
    public bool ExtendedScaleRanges { get; set; } = true;

    /// <summary>Arrow keys, Enter, Space and Backspace control menus; number keys answer questions; Escape leaves title sub-menus.</summary>
    public bool KeyboardMenus { get; set; } = true;

    /// <summary>How fast the fade between locations runs compared to vanilla; 1 keeps vanilla.</summary>
    public float FadeSpeed { get; set; } = 2f;

    /// <summary>Don't offer to eat the held food when clicking a crop or forage.</summary>
    public bool NoEatPromptOnCrops { get; set; } = true;

    /// <summary>Don't hand the held item to an NPC standing below a door you're opening.</summary>
    public bool NoGiftsThroughDoors { get; set; } = true;

    /// <summary>Show the area sprinklers, scarecrows, bee houses and Junimo huts cover.</summary>
    public bool RangePreview { get; set; } = true;

    /// <summary>Pick a birthday; on the day villagers wish you well and friends send letters. Each player's own setting applies.</summary>
    public bool PlayerBirthday { get; set; } = true;
}
