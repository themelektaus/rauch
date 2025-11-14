using Rauch.Core;
using Rauch.Core.Attributes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rauch.Commands.Install;

[Command("everything", "Installs Everything Search Engine")]
public class Everything : ICommand
{
    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetService<ILogger>();
        logger?.Info("Starting Everything installation...");
        logger?.Warning("TODO: Implementation of installation");
        // The actual installation would happen here
        // e.g. download and execute installer
        return Task.CompletedTask;
    }
}
