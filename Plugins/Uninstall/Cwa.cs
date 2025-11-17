namespace Rauch.Plugins.Uninstall;

[Command("cwa", "Uninstall ConnectWise Automate agents")]
public class Cwa : ICommand
{
    const string CWA_UNINSTALL_URL = "https://cloud.it-guards.at/download/cwa-uninstall.exe";
    const string AGENT_UNINSTALLER_URL = "https://s3.amazonaws.com/assets-cp/assets/Agent_Uninstaller.zip";
    const string INSTALL_DIR = @"data\CWA";

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        try
        {
            if (!EnsureAdministrator(logger))
            {
                return;
            }

            // Set working directory
            SetWorkingDirectory(INSTALL_DIR, logger);

            // Download and run LT Agent uninstaller
            await DownloadAndRunLTAgentUninstaller(logger, ct);

            // Uninstall ScreenConnect Client via WMIC
            await UninstallScreenConnectClient(logger, ct);

            // Download and run CWA uninstaller
            await DownloadAndRunCWAUninstaller(logger, ct);

            logger?.Success("ConnectWise Automate uninstallers completed successfully.");
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed: {ex.Message}");
        }
    }

    async Task DownloadAndRunLTAgentUninstaller(ILogger logger, CancellationToken ct)
    {
        var zipPath = "Agent_Uninstaller.zip";
        var uninstallerExe = "Agent_Uninstall.exe";

        try
        {
            // Download Agent Uninstaller ZIP
            await DownloadFile(AGENT_UNINSTALLER_URL, zipPath, logger, ct);

            // Extract ZIP
            await Unzip(zipPath, ".", logger, ct);

            // Start LT Agent Uninstaller
            await StartProcess(uninstallerExe, logger, ct);

            // Cleanup ZIP file
            File.Delete(zipPath);
            logger?.Info($"Cleaned up {zipPath}");
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed to run LT Agent uninstaller: {ex.Message}");

            // Cleanup on error
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            throw;
        }
    }

    async Task DownloadAndRunCWAUninstaller(ILogger logger, CancellationToken ct)
    {
        var cwaUninstallExe = "cwa-uninstall.exe";

        // Download CWA uninstaller
        await DownloadFile(CWA_UNINSTALL_URL, cwaUninstallExe, logger, ct);

        // Start CWA uninstaller
        await StartProcess(cwaUninstallExe, logger);
    }

    async Task UninstallScreenConnectClient(ILogger logger, CancellationToken ct)
    {
        logger?.Info("Uninstalling ScreenConnect Client via WMIC...");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = "product where \"name like 'ScreenConnect Client%%'\" call uninstall /nointeractive",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is not null)
            {
                var output = await process.StandardOutput.ReadToEndAsync(ct);
                var error = await process.StandardError.ReadToEndAsync(ct);

                await process.WaitForExitAsync(ct);

                if (!string.IsNullOrWhiteSpace(output))
                {
                    logger?.Info(output.Trim());
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    logger?.Warning(error.Trim());
                }

                if (process.ExitCode == 0)
                {
                    logger?.Success("ScreenConnect Client uninstalled successfully");
                }
                else
                {
                    logger?.Warning($"WMIC exited with code {process.ExitCode}");
                }
            }
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed to uninstall ScreenConnect Client: {ex.Message}");
            throw;
        }
    }
}
