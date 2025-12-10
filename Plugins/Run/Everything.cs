namespace Rauch.Plugins.Run;

[Name("everything")]
[Description("Download and run Everything Search Engine")]
public class Everything : ICommand
{
    const string DOWNLOAD_URL = "https://cloud.it-guards.at/download/everything.exe";
    const string DATA_DIRECTORY = "data";
    const string FILE = "everything.exe";

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
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
