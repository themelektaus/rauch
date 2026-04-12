namespace Rauch.Plugins.Windows;

[Name("veeam")]
public class Veeam : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        if (!EnsureAdministrator(logger))
        {
            return;
        }

        switch (logger?.Choice("Action", ["list", "restart"], 0))
        {
            case 0:
                await ExecutePowershellCommand("Get-Service | Where-Object {$_.DisplayName -like \"Veeam*\"}", logger: logger, ct: ct);
                break;

            case 1:
                await ExecutePowershellCommand("Get-Service | Where-Object {$_.DisplayName -like \"Veeam*\"} | Restart-Service -Force", logger: logger, ct: ct);
                break;
        }
    }
}
