namespace Rauch.Commands.Windows;

[Command("winrm", "Enable WinRM and configure remote management")]
public class WinRm : ICommand
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        logger?.Info("Extracting and executing WinRM configuration script...");

        var exitCode = await ExecutePowershellFile<WinRm>(logger: logger, ct: ct);

        logger?.Exit(exitCode);
    }
}
