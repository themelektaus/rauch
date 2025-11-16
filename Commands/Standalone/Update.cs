using System.Text.Json;

namespace Rauch.Commands.Standalone;

[Command("update", "Update rauch to the latest version from GitHub")]
public class Update : ICommand
{
    private const string GitHubRawUrl = "https://raw.githubusercontent.com/themelektaus/rauch/main/Build/Windows/rauch.exe";
    private const string GitHubApiUrl = "https://api.github.com/repos/themelektaus/rauch/contents/Plugins";
    private const string GitHubRawPluginBase = "https://raw.githubusercontent.com/themelektaus/rauch/main/Plugins/";
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
            var pluginsDir = Path.Combine(currentDir, "Plugins");

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "rauch-updater");

            // Step 1: Download rauch.exe
            logger?.Info($"Downloading latest rauch.exe from GitHub...");
            logger?.Debug($"URL: {GitHubRawUrl}");

            var response = await httpClient.GetAsync(GitHubRawUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger?.Error($"Failed to download update: HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                return;
            }

            var newFileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            logger?.Info($"Downloaded rauch.exe ({newFileBytes.Length:N0} bytes)");
            logger?.Warning("Note: Update will always be applied. Version checking not yet implemented.");

            // Save to temp file
            await File.WriteAllBytesAsync(tempFilePath, newFileBytes, cancellationToken);
            logger?.Debug($"Saved to: {tempFilePath}");

            // Step 2: Download plugin files
            logger?.Info("Downloading plugin files from GitHub...");
            var pluginFiles = await DownloadPluginFiles(httpClient, pluginsDir, logger, cancellationToken);

            if (pluginFiles.Count > 0)
            {
                logger?.Success($"Downloaded {pluginFiles.Count} plugin file(s)");
            }
            else
            {
                logger?.Warning("No plugin files found or download failed");
            }

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

    private async Task<List<string>> DownloadPluginFiles(HttpClient httpClient, string pluginsDir, ILogger logger, CancellationToken cancellationToken)
    {
        var downloadedFiles = new List<string>();

        try
        {
            // Ensure Plugins directory exists
            if (!Directory.Exists(pluginsDir))
            {
                Directory.CreateDirectory(pluginsDir);
            }

            // Get list of files from GitHub API
            logger?.Debug($"Fetching plugin list from: {GitHubApiUrl}");
            var response = await httpClient.GetAsync(GitHubApiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger?.Warning($"Failed to fetch plugin list: HTTP {(int)response.StatusCode}");
                return downloadedFiles;
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var items = JsonSerializer.Deserialize<List<GitHubFile>>(jsonContent, options);

            if (items == null || items.Count == 0)
            {
                logger?.Debug("No plugin files found in repository");
                return downloadedFiles;
            }

            // Download root-level .cs files
            foreach (var file in items.Where(f => f.Type == "file" && f.Name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    logger?.Debug($"Downloading plugin: {file.Name}");
                    var downloadUrl = $"{GitHubRawPluginBase}{file.Name}";
                    var fileResponse = await httpClient.GetAsync(downloadUrl, cancellationToken);

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var content = await fileResponse.Content.ReadAsStringAsync(cancellationToken);
                        var targetPath = Path.Combine(pluginsDir, file.Name);

                        await File.WriteAllTextAsync(targetPath, content, cancellationToken);
                        downloadedFiles.Add(file.Name);
                        logger?.Debug($"  ✓ {file.Name}");
                    }
                    else
                    {
                        logger?.Warning($"  ✗ Failed to download {file.Name}: HTTP {(int)fileResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    logger?.Warning($"  ✗ Error downloading {file.Name}: {ex.Message}");
                }
            }

            // Download subdirectories (1 level)
            foreach (var dir in items.Where(d => d.Type == "dir"))
            {
                try
                {
                    logger?.Debug($"Processing subdirectory: {dir.Name}");

                    // Create subdirectory
                    var subDirPath = Path.Combine(pluginsDir, dir.Name);
                    if (!Directory.Exists(subDirPath))
                    {
                        Directory.CreateDirectory(subDirPath);
                    }

                    // Fetch contents of subdirectory
                    var subDirUrl = $"{GitHubApiUrl}/{dir.Name}";
                    var subDirResponse = await httpClient.GetAsync(subDirUrl, cancellationToken);

                    if (!subDirResponse.IsSuccessStatusCode)
                    {
                        logger?.Warning($"  ✗ Failed to fetch {dir.Name}: HTTP {(int)subDirResponse.StatusCode}");
                        continue;
                    }

                    var subDirJson = await subDirResponse.Content.ReadAsStringAsync(cancellationToken);
                    var subDirFiles = JsonSerializer.Deserialize<List<GitHubFile>>(subDirJson, options);

                    if (subDirFiles == null || subDirFiles.Count == 0)
                    {
                        logger?.Debug($"  No files in {dir.Name}");
                        continue;
                    }

                    // Download .cs files from subdirectory
                    foreach (var file in subDirFiles.Where(f => f.Type == "file" && f.Name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
                    {
                        try
                        {
                            logger?.Debug($"  Downloading: {dir.Name}/{file.Name}");
                            var downloadUrl = $"{GitHubRawPluginBase}{dir.Name}/{file.Name}";
                            var fileResponse = await httpClient.GetAsync(downloadUrl, cancellationToken);

                            if (fileResponse.IsSuccessStatusCode)
                            {
                                var content = await fileResponse.Content.ReadAsStringAsync(cancellationToken);
                                var targetPath = Path.Combine(subDirPath, file.Name);

                                await File.WriteAllTextAsync(targetPath, content, cancellationToken);
                                downloadedFiles.Add($"{dir.Name}/{file.Name}");
                                logger?.Debug($"    ✓ {dir.Name}/{file.Name}");
                            }
                            else
                            {
                                logger?.Warning($"    ✗ Failed to download {dir.Name}/{file.Name}: HTTP {(int)fileResponse.StatusCode}");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.Warning($"    ✗ Error downloading {dir.Name}/{file.Name}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger?.Warning($"  ✗ Error processing subdirectory {dir.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            logger?.Warning($"Failed to download plugins: {ex.Message}");
        }

        return downloadedFiles;
    }

    private class GitHubFile
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Download_Url { get; set; }
    }
}
