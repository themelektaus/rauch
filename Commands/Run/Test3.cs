using Rauch.Core;
using Rauch.Core.Attributes;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Rauch.Commands.Run;

[Command("test3", "Performs a ping to nockal.com")]
public class Test3 : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetService<ILogger>();

        try
        {
            logger?.Info("Pinging nockal.com...");
            using (var ping = new Ping())
            {
                var reply = await ping.SendPingAsync("nockal.com", 5000);

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
