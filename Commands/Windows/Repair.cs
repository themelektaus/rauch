namespace Rauch.Commands.Windows;

[Name("repair")]
[Keywords("sfc dism")]
public class Repair : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        if (!EnsureAdministrator(logger))
        {
            return;
        }

        async Task Run(string powershellCommand)
        {
            await ExecutePowershellCommand(powershellCommand, CommandFlags.NoProfile, logger: logger, ct: ct);
        }

        if (logger?.Choice("Run DISM", ["yes", "no"], 1) == 0)
        {
            await Run("DISM /Online /Cleanup-Image /ScanHealth");
            await Run("DISM /Online /Cleanup-Image /RestoreHealth");
        }

        await Run("sfc /scannow");
    }
}
