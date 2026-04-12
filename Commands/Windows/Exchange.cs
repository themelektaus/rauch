namespace Rauch.Commands.Windows;

[Name("exchange")]
public class Exchange : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        if (!EnsureAdministrator(logger))
        {
            return;
        }

        _ = ExecutePowershellCommand("Install-Module -Name ExchangeOnlineManagement; Connect-ExchangeOnline", CommandFlags.UseShellExecute | CommandFlags.NoExit, logger, ct);
    }
}
