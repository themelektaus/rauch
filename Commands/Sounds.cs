namespace Rauch.Commands;

[Command("sounds")]
public class Sounds : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        if (File.Exists("no-sounds"))
        {
            File.Delete("no-sounds");
            logger?.Success("Sounds have been enabled.");
        }
        else
        {
            File.Create("no-sounds").Dispose();
            logger?.Success("Sounds have been disabled.");
        }
    }
}
