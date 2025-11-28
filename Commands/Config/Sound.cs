namespace Rauch.Commands.Config;

[Command("sound")]
public class Sound : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        var soundEnabled = ConfigIni.Read(data => data["Sound"]["Enabled"], logger);
        var choiceDefaultIndex = soundEnabled == "1" ? 0 : 1;

        if (logger?.Choice($"Sounds are currently {(choiceDefaultIndex == 0 ? "enabled" : "disabled")}. Change to", ["enabled", "disabled"], choiceDefaultIndex) == 0)
        {
            ConfigIni.Edit(data => data["Sound"]["Enabled"] = "1", logger);
            logger?.Info("Sounds have been enabled.");
        }
        else
        {
            ConfigIni.Edit(data => data["Sound"]["Enabled"] = "0", logger);
            logger?.Info("Sounds have been disabled.");
        }
    }
}
