namespace Rauch.Commands.Uninstall;

[Command("nxlog")]
public class Nxlog : ICommand
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        var exitCode = await ExecutePowershellFile(@"plugins\uninstall\nxlog.ps1", flags: CommandFlags.NoProfile, logger: logger, ct: ct);

        if (exitCode == 0)
        {
            logger?.Success("Nxlog uninstallation completed successfully");
        }
        else
        {
            logger?.Error($"Nxlog uninstallation failed with exit code {exitCode}");
        }
    }
}
