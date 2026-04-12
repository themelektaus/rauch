namespace Rauch.Plugins.Install;

[Name("rauchmelder")]
[Description("Install Rauchmelder application with .NET 10 runtime")]
public class Rauchmelder : ICommand
{
    const string RAUCHMELDER_URL = "://feuerwehr.cloud.it-guards.at/download/rauchmelder/windows/Rauchmelder.exe";
    const string INSTALL_DIR = @"C:\ProgramData\Rauchmelder";

    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
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
                "FeuerwehrUrl=https://feuerwehr.cloud.it-guards.at"
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
}
