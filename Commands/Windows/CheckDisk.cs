namespace Rauch.Commands.Windows;

[Name("checkdisk")]
public class Checkdisk : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        if (!EnsureAdministrator(logger))
        {
            return;
        }

        await ExecutePowershellCommand("chkdsk /f", logger: services.GetService<ILogger>(), ct: ct);
    }
}
