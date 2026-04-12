namespace Rauch.Commands.Run;

[Name("lookup")]
[MinArguments(1)]
public class Lookup : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        var domain = args.FirstOrDefault() ?? string.Empty;
        if (domain == string.Empty)
        {
            return;
        }

        var logger = services.GetService<ILogger>();

        foreach (var type in new[] { "MX", "A", "AAAA", "TXT", "CNAME" })
        {
            logger?.Write();
            logger?.Info($" - [{type}] -");
            await ExecutePowershellCommand($"nslookup -type={type} {domain}", ct: ct);
            logger?.Write();
        }
    }
}
