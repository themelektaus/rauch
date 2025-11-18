namespace Rauch.Commands.Run;

[Command("ping", Parameters = "<host1> <host2> ...")]
[MinArguments(1)]
public class Ping : ICommand
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        await ExecutePowershellFile<Ping>(string.Join(' ', args), logger: logger, ct: ct);
    }
}
