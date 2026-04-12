namespace Rauch.Commands.Windows;

[Name("cancelshutdown")]
[Description("Cancel a pending shutdown or reboot")]
public class CancelShutdown : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        await ExecutePowershellCommand("shutdown /a", logger: services.GetService<ILogger>(), ct: ct);
    }
}
