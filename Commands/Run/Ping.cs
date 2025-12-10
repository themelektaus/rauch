namespace Rauch.Commands.Run;

[Name("ping")]
[MinArguments(1)]
public class Ping : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetService<ILogger>();

        var exitCode = await ExecutePowershellFile<Ping>(string.Join(' ', args), logger: logger, ct: ct);

        logger?.Exit(exitCode);
    }
}
