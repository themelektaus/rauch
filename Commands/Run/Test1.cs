using Rauch.Core;
using Rauch.Core.Attributes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rauch.Commands.Run;

[Command("test1", "Outputs 'Hello, World!'")]
public class Test1 : ICommand
{
    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetService<ILogger>();
        logger?.Success("Hello, World!");
        return Task.CompletedTask;
    }
}
