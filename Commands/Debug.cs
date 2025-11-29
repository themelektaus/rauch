#if DEBUG
namespace Rauch.Commands;

[Command("debug", "Internal debug command")]
public class Debug : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        if (args.Length == 0)
        {
            logger?.Debug("rauch debug publish");
            return;
        }

        switch (args[0])
        {
            case "publish":
                Run("dotnet publish /p:PublishProfile=Properties\\PublishProfiles\\FolderProfile.pubxml");
                break;
        }

        void Run(string command)
        {
            _ = StartProcess(
                "cmd",
                $"/c timeout 2 /nobreak >nul && cd \"..\\..\\..\" && {command}",
                flags: CommandFlags.UseShellExecute,
                logger: logger,
                ct: ct
            );
        }
    }
}
#endif
