namespace Rauch.Commands.Windows;

[Name("reboot")]
public class Reboot : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        await ExecutePowershellCommand("shutdown /r /f /t 0", logger: services.GetService<ILogger>(), ct: ct);
    }
}
