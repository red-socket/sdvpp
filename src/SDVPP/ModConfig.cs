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
}
