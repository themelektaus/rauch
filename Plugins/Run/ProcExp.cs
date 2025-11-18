namespace Rauch.Plugins.Run;

[Command("procexp", "Download and run Process Explorer")]
public class ProcExp : ICommand
{
    const string DOWNLOAD_URL = "https://cloud.it-guards.at/download/procexp64.exe";
    const string DATA_DIRECTORY = "data";
    const string FILE = "procexp64.exe";

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        try
        {
            // Create and navigate to working directory
            SetWorkingDirectory(DATA_DIRECTORY, logger);

            // Download the file if it doesn't exist
            await DownloadFile(DOWNLOAD_URL, FILE, logger, ct);

            // Start the process
            _ = StartProcess(FILE, logger: logger);
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed: {ex.Message}");
        }
    }
}
