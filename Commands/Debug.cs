#if DEBUG
namespace Rauch.Commands;

[Name("debug")]
[Description("Internal debug command")]
public class Debug : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetService<ILogger>();

        if (args.Length == 0)
        {
            logger?.Debug("rauch debug clear");
            logger?.Debug("rauch debug build");
            logger?.Debug("rauch debug publish");
            return;
        }

        switch (args[0])
        {
            case "clear":
                Run("dotnet clean && rmdir /S /Q .vs && rmdir /S /Q bin && rmdir /S /Q Build && rmdir /S /Q obj && del rauch.csproj.user && del Properties\\PublishProfiles\\FolderProfile.pubxml.user && del Properties\\launchSettings.json && dotnet build");
                break;

            case "build":
                Run("dotnet build");
                break;

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
