namespace Rauch.Commands.Windows;

[Name("azurestatus")]
public class AzureStatus : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        await ExecutePowershellCommand("dsregcmd /status", logger: services.GetService<ILogger>(), ct: ct);
    }
}
