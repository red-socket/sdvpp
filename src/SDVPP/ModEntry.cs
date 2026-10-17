using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace SDVPP;

internal sealed class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.Monitor.Log(this.Helper.Translation.Get("loaded"), LogLevel.Info);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.Monitor.Log($"{Game1.player.Name} pressed {e.Button}.", LogLevel.Debug);
    }
}
