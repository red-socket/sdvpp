using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SDVPP.Features;

/// <summary>Speed up the fade to and from black when walking between locations.</summary>
/// <remarks>
/// Client-only; the fade state is per screen. Only the warp fade is touched: global fades (sleeping, the end of
/// the day) and fades during events and farm events keep their pace, since those scripts are timed around them.
/// </remarks>
internal sealed class FastFades
{
    /// <summary>The game's warp fade speed, in alpha per millisecond (about 0.6 s each way).</summary>
    private const float VanillaAlphaPerMs = 0.0019f;

    private readonly Func<ModConfig> config;

    public FastFades(IModHelper helper, Func<ModConfig> config)
    {
        this.config = config;
        helper.Events.GameLoop.UpdateTicking += this.OnUpdateTicking;
    }

    private void OnUpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        float speed = this.config().FadeSpeed;
        if (speed <= 1f || !Context.IsWorldReady || !Game1.fadeToBlack || Game1.globalFade)
            return;
        if (Game1.eventUp || Game1.farmEvent != null || Game1.pauseTime != 0f)
            return;

        // add the extra on top of the game's own step this tick, the same way it moves the alpha
        float extra = VanillaAlphaPerMs * (speed - 1f) * (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
        if (Game1.fadeIn)
            Game1.fadeToBlackAlpha += extra;
        else if (!Game1.messagePause && !Game1.dialogueUp)
            Game1.fadeToBlackAlpha -= extra;
    }
}
