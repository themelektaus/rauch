namespace Rauch.Commands.Run;

[Command("ping", Parameters = "<host1> <host2> ...")]
[MinArguments(1)]
public class Ping : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        var exitCode = await ExecutePowershellFile<Ping>(string.Join(' ', args), logger: logger, ct: ct);

        logger?.Exit(exitCode);
    }
}
