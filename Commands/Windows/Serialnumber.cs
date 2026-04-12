namespace Rauch.Commands.Windows;

[Name("serialnumber")]
public class Serialnumber : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        await ExecutePowershellCommand("wmic bios get serialnumber", logger: services.GetService<ILogger>(), ct: ct);
    }
}
