namespace Rauch.Plugins.Install;

[Command("rauchmelder", "Install Rauchmelder application with .NET 9 runtime", Parameters = "https|http")]
public class Rauchmelder : ICommand
{
    const string DOTNET_RUNTIME_URL = "://cloud.it-guards.at/download/dotnet-runtime-9.0.4-win-x64.exe";
    const string RAUCHMELDER_URL = "://cloud.it-guards.at/download/rauchmelder/windows/Rauchmelder.exe";
    const string INSTALL_DIR = @"C:\ProgramData\Rauchmelder";

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var scheme = args.FirstOrDefault() == "http" ? "http" : "https";

        var logger = services.GetService<ILogger>();

        try
        {
            // Check for administrator privileges
            if (!EnsureAdministrator(logger))
            {
                return;
            }

            logger?.Success("Running as administrator");

            // Set working directory
            SetWorkingDirectory(INSTALL_DIR, logger);

            // Check if .NET 9 runtime is installed
            if (!IsDotNetRuntimeInstalled(logger, 9))
            {
                logger?.Warning($".NET 9 runtime not found. Installing...");

                var runtimeInstaller = "dotnet-runtime-9.0.4-win-x64.exe";
                await DownloadFile(scheme + DOTNET_RUNTIME_URL, runtimeInstaller, logger, ct);

                logger?.Info("Installing .NET runtime (this may take a few minutes)...");

                var exitCode = await StartProcess(
                    runtimeInstaller,
                    "/install /quiet /norestart",
                    CommandFlags.None,
                    logger,
                    ct
                );

                if (exitCode == 0)
                {
                    logger?.Success(".NET runtime installed successfully");
                }
                else
                {
                    logger?.Error($".NET runtime installation failed with exit code {exitCode}");
                    return;
                }
            }
            else
            {
                logger?.Info(".NET 9 runtime is already installed");
            }

            await StartProcess("net", "stop rauchmelder", logger: logger, ct: ct);
            await Task.Delay(1);

            // Download Rauchmelder.exe (always force download)
            var rauchmelderExe = "Rauchmelder.exe";

            // Delete existing file to force download
            if (File.Exists(rauchmelderExe))
            {
                logger?.Info("Removing old Rauchmelder.exe...");
                File.Delete(rauchmelderExe);
            }

            await DownloadFile(scheme + RAUCHMELDER_URL, rauchmelderExe, logger, ct);

            // Create Config.ini
            logger?.Info("Creating Config.ini...");
            var configPath = Path.Combine(INSTALL_DIR, "Config.ini");
            await File.WriteAllLinesAsync(configPath, [
                "[General]",
                "InformUrl=https://feuerwehr.cloud.it-guards.at/inform",
                "DownloadUrl=https://cloud.it-guards.at/download/rauchmelder",
                "TunnelUrl=https://feuerwehr.cloud.it-guards.at/api/tunnel"
            ], ct);
            logger?.Success("Config.ini created");

            logger?.Success("Rauchmelder installation completed successfully");
            logger?.Info($"Installation directory: {INSTALL_DIR}");

            await StartProcess(rauchmelderExe, "interactive", logger: logger, ct: ct);
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed to install Rauchmelder: {ex.Message}");
        }
    }

    private bool IsDotNetRuntimeInstalled(ILogger logger, uint version)
    {
        var dotnetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");

        if (!Directory.Exists(dotnetPath))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-runtimes",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Check for Microsoft.NETCore.App
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith($"Microsoft.NETCore.App {version}."))
                {
                    logger?.Info($"Found: {line.Trim()}");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            logger?.Warning($"Failed to check .NET runtime: {ex.Message}");
        }

        return false;
    }
}
