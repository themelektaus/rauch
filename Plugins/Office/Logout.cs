namespace Rauch.Plugins.Office;

[Command("logout")]
public class Logout : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        var exitCode = await ExecutePowershellFile(
            @"plugins\office\logout.ps1",
            arguments: args.FirstOrDefault() ?? string.Empty,
            logger: logger,
            ct: ct
        );

        logger?.Exit(exitCode);
    }
}
