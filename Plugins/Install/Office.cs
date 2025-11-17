namespace Rauch.Plugins.Install;

[Command("office", "Download and install Microsoft Office")]
public class Office : ICommand
{
    const string DOWNLOAD_URL = "https://cloud.it-guards.at/download/OInstall_x64.exe";
    const string INSTALL_DIR = @"data\Office";
    const string FILE_NAME = "OInstall_x64.exe";

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        try
        {
            if (!EnsureAdministrator(logger))
            {
                return;
            }

            SetWorkingDirectory(INSTALL_DIR, logger);

            await DownloadFile(DOWNLOAD_URL, FILE_NAME, logger, ct);

            StartProcess(FILE_NAME, logger);
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed: {ex.Message}");
        }
    }
}
