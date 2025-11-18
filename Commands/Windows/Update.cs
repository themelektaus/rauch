namespace Rauch.Commands.Windows;

[Command("update")]
public class Update : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        var exitCode = await ExecutePowershellFile<Update>(flags: CommandFlags.NoProfile, logger: logger, ct: ct);

        if (exitCode == 0)
        {
            logger?.Success("Windows Update completed successfully");
        }
        else
        {
            logger?.Error($"Windows Update failed with exit code {exitCode}");
        }
    }
}
