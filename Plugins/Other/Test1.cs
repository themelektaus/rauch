namespace Rauch.Plugins.Other;

[Command("test1", "Displays a test message 1")]
public class Test1 : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetService<ILogger>();

        logger?.Success("Testing Nr. 1");
        logger?.Info("This is a dynamically loaded plugin command!");

        await Task.CompletedTask;
    }
}
