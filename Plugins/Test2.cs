[Command("test2", "Displays a test2 message", Parameters = "[name]")]
public class Test2 : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetService<ILogger>();

        var name = args.Length > 0 ? args[0] : "World";

        logger?.Success($"Testing Nr. 2... {name}!");
        logger?.Info("This is a dynamically loaded plugin command!");

        await Task.CompletedTask;
    }
}
