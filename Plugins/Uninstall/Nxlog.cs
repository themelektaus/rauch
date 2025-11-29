namespace Rauch.Plugins.Uninstall;

[Command("nxlog")]
public class Nxlog : ICommand
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        if (!EnsureAdministrator(logger))
        {
            return;
        }

        var exitCode = await ExecutePowershellFile(@"plugins\uninstall\nxlog.ps1", flags: CommandFlags.NoProfile, logger: logger, ct: ct);

        logger?.Exit(exitCode);
    }
}
