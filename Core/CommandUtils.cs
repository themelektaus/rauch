namespace Rauch.Core;

public static class CommandUtils
{
    public static void SetWorkingDirectory(string path, ILogger logger = null)
    {
        if (!Directory.Exists(path))
        {
            logger?.Info($"Creating directory: {path}");
            Directory.CreateDirectory(path);
        }

        Environment.CurrentDirectory = path;
        logger?.Info($"Working directory: {path}");
    }

    public static async Task StartProcess(string filePath, ILogger logger = null)
    {
        logger?.Info($"Starting {Path.GetFileName(filePath)}...");

        var startInfo = new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        };

        var process = Process.Start(startInfo);
        
        logger?.Success($"{Path.GetFileName(filePath)} started successfully.");
        
        await process.WaitForExitAsync();
    }

    public static async Task DownloadFile(string url, string filePath, CancellationToken ct = default, ILogger logger = null)
    {
        var fileName = Path.GetFileName(filePath);

        if (File.Exists(filePath))
        {
            logger?.Info($"{fileName} already exists. Skipping download.");
            return;
        }

        logger?.Info($"Downloading {fileName}...");

        using var httpClient = new HttpClient();

        httpClient.Timeout = TimeSpan.FromMinutes(5);
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsByteArrayAsync(ct);
        await File.WriteAllBytesAsync(filePath, content, ct);
        logger?.Success($"Downloaded {fileName} ({content.Length / 1024} KB)");
    }

    public static async Task Unzip(string zipPath, string destinationPath, CancellationToken ct = default, ILogger logger = null)
    {
        var zipName = Path.GetFileName(zipPath);

        logger?.Info($"Extracting {zipName}...");

        using (var fileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
        {
            foreach (var entry in archive.Entries)
            {
                var path = Path.Combine(destinationPath, entry.FullName);

                if (string.IsNullOrEmpty(entry.Name))
                {
                    // It's a directory
                    Directory.CreateDirectory(path);
                }
                else
                {
                    // It's a file
                    var directoryPath = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Manual extraction
                    using (var entryStream = entry.Open())
                    using (var outputStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        await entryStream.CopyToAsync(outputStream, ct);
                    }
                }
            }
        }

        logger?.Success($"Extracted {zipName}");
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static bool EnsureAdministrator(ILogger logger = null)
    {
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        
        if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
        {
            logger?.Success("Running as administrator");
            return true;
        }
        
        logger?.Error("Not running as administrator");
        logger?.Warning("Please run rauch as administrator to continue");
        return false;
    }
}
