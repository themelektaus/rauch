namespace Rauch.Plugins.Install;

[Command("teams", "Install Microsoft Teams via remote PowerShell script")]
public class Teams : ICommand
{
    const string SCRIPT_URL = "https://raw.githubusercontent.com/mohammedha/Posh/refs/heads/main/O365/Teams/Install_TeamsV2.0.ps1";

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetService<ILogger>();

        try
        {
            logger?.Info("Downloading and executing Teams installation script...");

            // Execute remote PowerShell script
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"irm '{SCRIPT_URL}' | iex\"",
                UseShellExecute = false,
                CreateNoWindow = false
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                logger?.Error("Failed to start PowerShell process");
                return;
            }

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                logger?.Success("Teams installation completed successfully");
            }
            else
            {
                logger?.Error($"Teams installation failed with exit code {process.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed to install Teams: {ex.Message}");
        }
    }
}
