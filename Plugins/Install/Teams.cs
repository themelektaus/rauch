using System.Text.RegularExpressions;

namespace Rauch.Plugins.Install;

[Name("teams")]
[Description("Install Microsoft Teams via remote PowerShell script")]
public class Teams : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetService<ILogger>();
        logger?.Info("Checking installed versions");

        if (!EnsureAdministrator(logger))
        {
            return;
        }

        var installedVersions = new List<string>();

        var windowsAppsFolder = new DirectoryInfo(@"C:\Program Files\WindowsApps");
        var windowsAppsFolders = windowsAppsFolder.GetDirectories();
        
        foreach (var folder in windowsAppsFolders)
        {
            if (folder.Name.StartsWith("MSTeams_"))
            {
                var match = Regex.Match(folder.Name, @"_([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)_");
                if (match.Success)
                {
                    installedVersions.Add(match.Groups[1].Value);
                }
            }
        }
        
        var targetVersion = args.FirstOrDefault() ?? "25306.804.4102.7193";
        
        if (installedVersions.Contains(targetVersion))
        {
            logger?.Success("Already installed");
            logger?.Exit(0);
        }
        else
        {
            logger?.Info("Downloading and executing Teams installation script...");

            var command = $"irm 'https://raw.githubusercontent.com/mohammedha/Posh/refs/heads/main/O365/Teams/Install_TeamsV2.0.ps1' | iex";
            var exitCode = await ExecutePowershellCommand(command, CommandFlags.NoProfile, logger, ct);

            logger?.Exit(exitCode);
        }
    }
}
