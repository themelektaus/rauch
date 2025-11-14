using Rauch.Core;
using Rauch.Core.Attributes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rauch.Commands.Standalone;

[Command("debug", "Internal debug command", Hidden = true)]
public class Debug : ICommand
{
    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetService<ILogger>();
        logger?.Debug("This is a hidden debug command!");
        return Task.CompletedTask;
    }
}
