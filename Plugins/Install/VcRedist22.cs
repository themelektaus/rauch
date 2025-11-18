namespace Rauch.Commands.Install;

[Command("vcredist22")]
public class VcRedist22 : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        var exitCode = await ExecutePowershellFile(@"plugins\install\vcredist22.ps1", flags: CommandFlags.NoProfile, logger: logger, ct: ct);

        if (exitCode == 0)
        {
            logger?.Success("Visual C++ Redistributable installation completed successfully");
        }
        else
        {
            logger?.Error($"Visual C++ Redistributable installation failed with exit code {exitCode}");
        }
    }
}
