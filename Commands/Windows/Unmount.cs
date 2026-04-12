namespace Rauch.Commands.Windows;

[Name("unmount")]
public class Unmount : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        var driveLetter = logger?.Question("Drive Letter", defaultValue: "*");

        if (driveLetter == string.Empty)
        {
            return;
        }

        if (driveLetter.Length == 1 && "abcdefghijklmnopqrstuvwxyz".Contains(driveLetter, StringComparison.OrdinalIgnoreCase))
        {
            driveLetter += ":";
        }

        await ExecutePowershellCommand($"net use {driveLetter} /delete", logger: logger, ct: ct);
    }
}
