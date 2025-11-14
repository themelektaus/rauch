using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Rauch.Commands;
using Rauch.Core;
using Rauch.Core.Attributes;

namespace Rauch.Commands.Standalone;

[Command("update", "Updates rauch to the latest version from GitHub")]
public class Update : ICommand
{
    private const string GitHubRawUrl = "https://github.com/themelektaus/rauch/raw/main/Build/Windows/rauch.exe";
    private const string TempFileName = "rauch.exe.new";

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
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

            logger?.Info($"Downloading latest version from GitHub...");
            logger?.Debug($"URL: {GitHubRawUrl}");

            // Download new version
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            var response = await httpClient.GetAsync(GitHubRawUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger?.Error($"Failed to download update: HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                return;
            }

            var newFileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            logger?.Info($"Downloaded {newFileBytes.Length:N0} bytes");
            logger?.Warning("Note: Update will always be applied. Version checking not yet implemented.");

            // Save to temp file
            await File.WriteAllBytesAsync(tempFilePath, newFileBytes, cancellationToken);
            logger?.Debug($"Saved to: {tempFilePath}");

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

            await File.WriteAllTextAsync(scriptPath, script, cancellationToken);

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
}
