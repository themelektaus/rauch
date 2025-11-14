using Rauch.Core;
using Rauch.Core.Attributes;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Rauch.Commands.Run;

[Command("test2", "Performs a ping to google.at")]
public class Test2 : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetService<ILogger>();

        try
        {
            logger?.Info("Pinging google.at...");
            using (var ping = new Ping())
            {
                var reply = await ping.SendPingAsync("google.at", 5000);

                if (reply.Status == IPStatus.Success)
                {
                    logger?.Success($"Reply from {reply.Address}: Time={reply.RoundtripTime}ms TTL={reply.Options?.Ttl}");
                }
                else
                {
                    logger?.Error($"Ping failed: {reply.Status}");
                }
            }
        }
        catch (Exception ex)
        {
            logger?.Error($"Error during ping: {ex.Message}");
        }
    }
}
