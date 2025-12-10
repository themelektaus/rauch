namespace Rauch.Commands.Windows;

[Name("manager")]
[Keywords("computer management")]
public class Manager : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        _ = StartProcess("compmgmt.msc", "/s", flags: CommandFlags.UseShellExecute);
    }
}
