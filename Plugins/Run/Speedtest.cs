namespace Rauch.Plugins.Run;

[Command("speedtest")]
public class Speedtest : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        var exitCode = await ExecutePowershellFile(@"plugins\run\speedtest.ps1", flags: CommandFlags.NoProfile, logger: logger, ct: ct);

        if (exitCode == 0)
        {
            logger?.Success("Speedtest completed successfully");
        }
        else
        {
            logger?.Error($"Speedtest failed with exit code {exitCode}");
        }
    }
}
