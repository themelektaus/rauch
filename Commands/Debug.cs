namespace Rauch.Commands;

[Command("debug", "Internal debug command", Hidden = true)]
public class Debug : ICommand
{
    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();
        logger?.Debug("This is a hidden debug command!");
        return Task.CompletedTask;
    }
}
