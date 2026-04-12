namespace Rauch.Commands.Windows;

[Name("gpupdate")]
public class GpUpdate : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        await StartProcess("gpupdate", "/force", flags: CommandFlags.None, ct: ct);
    }
}
