namespace Rauch.Plugins.Run;

[Command("treesize")]
public class TreeSize : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        if (!EnsureAdministrator(logger))
        {
            return;
        }

        SetWorkingDirectory(@"data\TreeSize", logger);

        if (!File.Exists("TreeSizePortable.exe"))
        {
            await DownloadFile("https://cloud.it-guards.at/download/treesize.zip", "treesize.zip", logger, ct);
            await Unzip("treesize.zip", ".", logger, ct);

            File.Delete("treesize.zip");
            logger?.Info("Cleaned up treesize.zip");
        }

        _ = StartProcess("TreeSizePortable.exe", logger: logger, ct: ct);
    }
}
