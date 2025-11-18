namespace Rauch.Plugins.Install;

[Command("teams", "Install Microsoft Teams via remote PowerShell script")]
public class Teams : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        logger?.Info("Downloading and executing Teams installation script...");

        var command = $"irm 'https://raw.githubusercontent.com/mohammedha/Posh/refs/heads/main/O365/Teams/Install_TeamsV2.0.ps1' | iex";
        var exitCode = await ExecutePowershellCommand(command, CommandFlags.NoProfile, logger, ct);

        if (exitCode == 0)
        {
            logger?.Success("Teams installation completed successfully");
        }
        else
        {
            logger?.Error($"Teams installation failed with exit code {exitCode}");
        }
    }
}
