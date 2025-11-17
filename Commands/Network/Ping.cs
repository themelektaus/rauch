namespace Rauch.Commands.Network;

[Command("ping", "Perform a ping", Parameters = "hostname")]
[ExactArguments(1)]
public class Ping : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        var hostname = args[0];

        try
        {
            logger?.Info($"Pinging {hostname}...");

            using var ping = new System.Net.NetworkInformation.Ping();

            var reply = await ping.SendPingAsync(
                hostNameOrAddress: hostname,
                timeout: TimeSpan.FromSeconds(5),
                cancellationToken: ct
            );

            if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
            {
                logger?.Success($"Reply from {reply.Address}: Time={reply.RoundtripTime}ms TTL={reply.Options?.Ttl}");
            }
            else
            {
                logger?.Error($"Ping failed: {reply.Status}");
            }
        }
        catch (Exception ex)
        {
            logger?.Error($"Error during ping: {ex.Message}");
        }
    }
}
