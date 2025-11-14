[Command("hello", "Displays a greeting message", Parameters = "[name]")]
public class Hello : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetService<ILogger>();

        var name = args.Length > 0 ? args[0] : "World";

        logger?.Success($"Hello, {name}!");
        logger?.Info("This is a dynamically loaded plugin command!");

        await Task.CompletedTask;
    }
}
