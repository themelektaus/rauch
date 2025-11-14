[Command("test1", "Displays a test message", Parameters = "[name]")]
public class TestTest1 : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetService<ILogger>();

        var name = args.Length > 0 ? args[0] : "World";

        logger?.Success($"Testing Nr. 1 ... {name}!");
        logger?.Info("This is a dynamically loaded plugin command!");

        await Task.CompletedTask;
    }
}
