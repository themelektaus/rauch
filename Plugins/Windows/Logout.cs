namespace Rauch.Plugins.Windows;

[Name("logout")]
public class Logout : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetService<ILogger>();

        var exitCode = await ExecutePowershellFile(
            @"plugins\windows\logout.ps1",
            arguments: args.FirstOrDefault() ?? string.Empty,
            logger: logger,
            ct: ct
        );

        logger?.Exit(exitCode);
    }
}
