namespace Rauch.Plugins.Other;

[Command("test2", "Displays a test message 2")]
public class Test2 : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetService<ILogger>();

        logger?.Success("Testing Nr. 2");
        logger?.Info("This is a dynamically loaded plugin command!");

        await Task.CompletedTask;
    }
}
