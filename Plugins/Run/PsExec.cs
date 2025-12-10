namespace Rauch.Plugins.Run;

[Name("psexec")]
[Description("Download and run PsExec")]
public class PsExec : ICommand
{
    const string DOWNLOAD_URL = "https://cloud.it-guards.at/download/psexec64.exe";
    const string DATA_DIRECTORY = "data";
    const string FILE = "psexec64.exe";

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
            await StartProcess(FILE, string.Join(' ', args), logger: logger, ct: ct);
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed: {ex.Message}");
        }
    }
}
