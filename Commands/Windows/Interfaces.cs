namespace Rauch.Commands.Windows;

[Name("interfaces")]
[Keywords("network adapters")]
public class Interfaces : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        _ = StartProcess("ncpa.cpl", flags: CommandFlags.UseShellExecute, ct: ct);
    }
}
