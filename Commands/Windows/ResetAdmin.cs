namespace Rauch.Commands.Windows;

[Name("resetadmin")]
public class ResetAdmin : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        if (!EnsureAdministrator(logger))
        {
            return;
        }

        switch (logger?.Choice("Enable local administrator", ["yes", "no"]))
        {
            case 0:
                await ExecutePowershellCommand($"net user administrator /active:yes", logger: logger, ct: ct);
                var password = logger?.Question("Enter Password:", allowEmpty: false);
                await ExecutePowershellCommand($"net user administrator \"{password}\"", logger: logger, ct: ct);
                break;

            case 1:
                await ExecutePowershellCommand($"net user administrator /active:no", logger: logger, ct: ct);
                break;
        }
    }
}
