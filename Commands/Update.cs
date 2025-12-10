#if !DEBUG
namespace Rauch.Commands;

[Name("update")]
[Description("Update rauch to the latest version from GitHub")]
public class Update : ICommand
{
    const string GitHubRawUrl = "https://raw.githubusercontent.com/themelektaus/rauch/main/Build/Windows/rauch.exe";
    const string GitHubPluginsZipUrl = "https://raw.githubusercontent.com/themelektaus/rauch/main/Build/Plugins.zip";
    const string TempFileName = "rauch.exe.new";
    const string PluginsZipFileName = "Plugins.zip";

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetService<ILogger>();

        try
        {
            // Get current executable path
            var currentExePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentExePath))
            {
                logger?.Error("Could not determine current executable path.");
                return;
            }

            var currentDir = Path.GetDirectoryName(currentExePath);
            var tempFilePath = Path.Combine(currentDir, TempFileName);
            var pluginsDir = Path.Combine(currentDir, "plugins");

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "rauch-updater");

            // Step 1: Download rauch.exe
            logger?.Info($"Downloading latest rauch.exe from GitHub...");
            logger?.Debug($"URL: {GitHubRawUrl}");

            var response = await httpClient.GetAsync(GitHubRawUrl, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger?.Error($"Failed to download update: HTTP {(int) response.StatusCode} {response.ReasonPhrase}");
                return;
            }

            var newFileBytes = await response.Content.ReadAsByteArrayAsync(ct);

            logger?.Info($"Downloaded rauch.exe ({newFileBytes.Length:N0} bytes)");
            logger?.Warning("Note: Update will always be applied. Version checking not yet implemented.");

            // Save to temp file
            await File.WriteAllBytesAsync(tempFilePath, newFileBytes, ct);
            logger?.Debug($"Saved to: {tempFilePath}");

            // Step 2: Download and extract plugins
            logger?.Info("Downloading plugins from GitHub...");
            var pluginsSuccess = await DownloadAndExtractPlugins(httpClient, currentDir, pluginsDir, logger, ct);

            if (pluginsSuccess)
            {
                logger?.Success("Plugins downloaded and extracted successfully");
            }
            else
            {
                logger?.Warning("Plugin download failed");
            }

            try { File.Delete(Path.Combine(pluginsDir, "uninstall", "cwa.cs")); } catch { }
            try { File.Delete(Path.Combine(pluginsDir, "uninstall", "nxlog.cs")); } catch { }
            try { File.Delete(Path.Combine(pluginsDir, "uninstall", "nxlog.ps1")); } catch { }
            try { Directory.Delete(Path.Combine(pluginsDir, "uninstall")); } catch { }

            // Create update script
            var scriptPath = Path.Combine(currentDir, "update-script.bat");
            var script = $@"@echo off
timeout /t 2 /nobreak >nul
del /f /q ""{currentExePath}""
move /y ""{tempFilePath}"" ""{currentExePath}""
del /f /q ""{scriptPath}""
start """" ""{currentExePath}"" help
exit
";

            await File.WriteAllTextAsync(scriptPath, script, ct);

            logger?.Success("Update downloaded successfully!");
            logger?.Warning("Restarting application to apply update...");

            // Start update script and exit
            var processInfo = new ProcessStartInfo
            {
                FileName = scriptPath,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(processInfo);

            // Exit current process to allow update
            Environment.Exit(0);
        }
        catch (HttpRequestException ex)
        {
            logger?.Error($"Network error: {ex.Message}");
            logger?.Info("Please check your internet connection and try again.");
        }
        catch (Exception ex)
        {
            logger?.Error($"Update failed: {ex.Message}");
            logger?.Debug($"Details: {ex}");
        }
    }

    static async Task<bool> DownloadAndExtractPlugins(HttpClient httpClient, string currentDir, string pluginsDir, ILogger logger, CancellationToken ct)
    {
        try
        {
            var zipPath = Path.Combine(currentDir, PluginsZipFileName);

            // Download Plugins.zip
            logger?.Debug($"URL: {GitHubPluginsZipUrl}");
            var response = await httpClient.GetAsync(GitHubPluginsZipUrl, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger?.Warning($"Failed to download plugins: HTTP {(int) response.StatusCode}");
                return false;
            }

            var zipBytes = await response.Content.ReadAsByteArrayAsync(ct);
            await File.WriteAllBytesAsync(zipPath, zipBytes, ct);
            logger?.Debug($"Downloaded Plugins.zip ({zipBytes.Length:N0} bytes)");

            if (!Directory.Exists(pluginsDir))
            {
                Directory.CreateDirectory(pluginsDir);
            }

            if (!Directory.Exists(pluginsDir))
            {
                Directory.CreateDirectory(pluginsDir);
            }

            if (Directory.Exists(Path.Combine(pluginsDir, ".cache")))
            {
                Directory.Delete(Path.Combine(pluginsDir, ".cache"), true);
            }

            // Extract ZIP to plugins directory
            ZipFile.ExtractToDirectory(zipPath, pluginsDir, true);
            logger?.Debug($"Extracted plugins to: {pluginsDir}");

            // Clean up ZIP file
            File.Delete(zipPath);

            return true;
        }
        catch (Exception ex)
        {
            logger?.Warning($"Failed to download/extract plugins: {ex.Message}");
            return false;
        }
    }
}
#endif
