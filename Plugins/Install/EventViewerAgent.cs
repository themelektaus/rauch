namespace Rauch.Plugins.Install;

[Name("eventviewer-agent")]
public class EventViewerAgent : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetService<ILogger>();

        var exitCode = await ExecutePowershellFile(@"plugins\install\eventvieweragent.ps1", flags: CommandFlags.NoProfile, logger: logger, ct: ct);

        logger?.Exit(exitCode);
    }
}
