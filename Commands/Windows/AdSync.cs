namespace Rauch.Commands.Windows;

[Name("adsync")]
public class AdSync : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        switch (logger?.Choice("Variant", ["initial", "delta"], 1))
        {
            case 0:
                await ExecutePowershellCommand("Start-ADSyncSyncCycle -PolicyType Initial", logger: logger, ct: ct);
                break;

            case 1:
                await ExecutePowershellCommand("Start-ADSyncSyncCycle -PolicyType Delta", logger: logger, ct: ct);
                break;
        }
    }
}
