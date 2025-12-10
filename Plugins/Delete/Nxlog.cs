namespace Rauch.Plugins.Delete;

[Name("nxlog")]
public class Nxlog : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetService<ILogger>();

        if (!EnsureAdministrator(logger))
        {
            return;
        }

        var exitCode = await ExecutePowershellFile(@"plugins\delete\nxlog.ps1", flags: CommandFlags.NoProfile, logger: logger, ct: ct);

        logger?.Exit(exitCode);
    }
}
