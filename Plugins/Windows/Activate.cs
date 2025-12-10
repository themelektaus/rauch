namespace Rauch.Plugins.Windows;

[Name("activate")]
public class Activate : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetService<ILogger>();
        var exitCode = await ExecutePowershellCommand("irm https://get.activated.win | iex", logger: logger, ct: ct);
        logger?.Exit(exitCode);
    }
}
