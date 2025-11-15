namespace Rauch.Plugins;

[Command("test", "Displays a test message")]
public class Test : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetService(typeof(ILogger)) as ILogger;

        logger?.Success("Test");
        logger?.Info("This is a dynamically loaded plugin command!");

        await Task.CompletedTask;
    }
}
