namespace Rauch.Plugins.Install;

[Command("vcredist22")]
public class VcRedist22 : ICommand
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        if (!EnsureAdministrator(logger))
        {
            return;
        }

        var exitCode = await ExecutePowershellFile(@"plugins\install\vcredist22.ps1", flags: CommandFlags.NoProfile, logger: logger, ct: ct);

        logger?.Exit(exitCode);
    }
}
