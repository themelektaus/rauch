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

    public static async Task DownloadFile(string url, string filePath, ILogger logger = null, CancellationToken ct = default)
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

    public static async Task Unzip(string zipPath, string destinationPath, ILogger logger = null, CancellationToken ct = default)
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

    [OS("windows")]
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
        logger?.Warning("Please run rauch as administrator to continue", preventSound: true);
        return false;
    }

    [Flags]
    public enum CommandFlags
    {
        None = 0,
        NoProfile = 1,
        UseShellExecute = 2,
        CreateNoWindow = 4
    }

    public static Task<int> ExecutePowershellCommand(string command, CommandFlags flags = CommandFlags.None, ILogger logger = null, CancellationToken ct = default)
    {
        return ExecutePowershellInternal(command, $"-Command \"{command.Replace("\"", "\\\"")}\"", flags, logger, ct);
    }

    public static Task<int> ExecutePowershellFile(string file, string arguments = "", CommandFlags flags = CommandFlags.None, ILogger logger = null, CancellationToken ct = default)
    {
        return ExecutePowershellInternal($"{file} {arguments}", $"-File \"{file}\" {arguments}", flags, logger, ct);
    }

    public static async Task<int> ExecutePowershellFile<T>(string arguments = "", CommandFlags flags = CommandFlags.None, ILogger logger = null, CancellationToken ct = default)
    {
        var t = typeof(T);
        var n = t.Namespace;
        n = n[0].ToString().ToLowerInvariant() + n[1..];

        var file = $"{t.Name}.ps1";
        var name = $"{n}.{file}";

        using var stream = t.Assembly.GetManifestResourceStream(name);

        if (stream is null)
        {
            logger?.Error($"Failed to load embedded resource: {name}");
            return -3;
        }

        using var reader = new StreamReader(stream);
        var scriptContent = await reader.ReadToEndAsync(ct);

        var tempScriptPath = Path.Combine(Path.GetTempFileName() + ".ps1");
        await File.WriteAllTextAsync(tempScriptPath, scriptContent, ct);

        var exitCode = await ExecutePowershellInternal($"{file} {arguments}", $"-File \"{tempScriptPath}\" {arguments}", flags, logger, ct);

        try { File.Delete(tempScriptPath); } catch { }

        return exitCode;
    }

    static async Task<int> ExecutePowershellInternal(string info, string arguments, CommandFlags flags = CommandFlags.None, ILogger logger = null, CancellationToken ct = default)
    {
        return await StartProcessInternal(
            info,
            "powershell.exe",
            $"{(flags.HasFlag(CommandFlags.NoProfile) ? "-NoProfile" : "")} -ExecutionPolicy Bypass {arguments}",
            flags,
            logger,
            ct
        );
    }

    public static Task<int> StartProcess(string filePath, string arguments = "", CommandFlags flags = CommandFlags.None, ILogger logger = null, CancellationToken ct = default)
    {
        return StartProcessInternal($"{filePath} {arguments}", filePath, arguments, flags, logger, ct);
    }

    static async Task<int> StartProcessInternal(string info, string filePath, string arguments = "", CommandFlags flags = CommandFlags.None, ILogger logger = null, CancellationToken ct = default)
    {
        try
        {
            logger?.Info($"PS> {info}");

            var startInfo = new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = arguments,
                UseShellExecute = flags.HasFlag(CommandFlags.UseShellExecute),
                CreateNoWindow = flags.HasFlag(CommandFlags.CreateNoWindow)
            };

            var process = Process.Start(startInfo);

            if (process is null)
            {
                logger?.Error($"Failed");
                return -2;
            }

            logger?.Success("OK");

            await process.WaitForExitAsync(ct);

            return process.ExitCode;
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed: {ex.Message}");
            return -1;
        }
    }
}
