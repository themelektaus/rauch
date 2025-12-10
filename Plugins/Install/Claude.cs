namespace Rauch.Plugins.Install;

[Name("claude")]
[Description("Install Claude Code and portable Git Bash")]
public class Claude : ICommand
{
    const string PORTABLE_GIT_ZIP_URL = "https://cloud.it-guards.at/download/PortableGit.zip";

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetService<ILogger>();

        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var claudePath = Path.Combine(userProfile, ".local", "bin");
            var claudeExe = Path.Combine(claudePath, "claude.exe");
            var claudeProjectPath = Path.Combine(claudePath, "Project");
            var claudeGitPath = Path.Combine(claudePath, "PortableGit");
            var claudeGitBashExe = Path.Combine(claudeGitPath, "bin", "bash.exe");

            // Update environment variables
            UpdateEnvironmentVariables(claudePath, claudeGitBashExe, logger);

            // Install Git Bash if needed
            await InstallGitBash(claudeGitPath, claudeGitBashExe, logger, ct);

            // Install Claude if needed
            await InstallClaude(claudePath, claudeExe, logger, ct);

            // Create project directory
            CreateProjectDirectory(claudeProjectPath, logger);

            logger?.Success("Claude Code installation completed successfully.");
            logger?.Info($"Project directory: {claudeProjectPath}");

            // Start the process
            _ = StartProcess(claudeExe, flags: CommandFlags.UseShellExecute, logger: logger);
        }
        catch (Exception ex)
        {
            logger?.Error($"Installation failed: {ex.Message}");
        }
    }

    private void UpdateEnvironmentVariables(string claudePath, string claudeGitBashExe, ILogger logger)
    {
        var currentPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Process);
        if (!currentPath.Contains(claudePath))
        {
            Environment.SetEnvironmentVariable("Path", $"{currentPath};{claudePath}", EnvironmentVariableTarget.Process);
            logger?.Info($"Added {claudePath} to PATH");
        }

        Environment.SetEnvironmentVariable("CLAUDE_CODE_GIT_BASH_PATH", claudeGitBashExe, EnvironmentVariableTarget.Process);
        logger?.Info($"Set CLAUDE_CODE_GIT_BASH_PATH to {claudeGitBashExe}");
    }

    private async Task InstallGitBash(string claudeGitPath, string claudeGitBashExe, ILogger logger, CancellationToken ct)
    {
        if (File.Exists(claudeGitBashExe))
        {
            logger?.Info($"git-bash is already installed at {claudeGitPath}");
            return;
        }

        logger?.Info("Installing git-bash...");

        if (!Directory.Exists(claudeGitPath))
        {
            Directory.CreateDirectory(claudeGitPath);
        }

        var zipPath = Path.Combine(claudeGitPath, "PortableGit.zip");

        try
        {
            // Download PortableGit.zip
            await DownloadFile(PORTABLE_GIT_ZIP_URL, zipPath, logger, ct);

            // Extract PortableGit.zip
            await Unzip(zipPath, claudeGitPath, logger, ct);

            // Delete PortableGit.zip
            File.Delete(zipPath);
            logger?.Info("Cleaned up PortableGit.zip");
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed to install git-bash: {ex.Message}");
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            throw;
        }
    }

    private async Task InstallClaude(string claudePath, string claudeExe, ILogger logger, CancellationToken ct)
    {
        if (File.Exists(claudeExe))
        {
            logger?.Info($"Claude is already installed at {claudePath}");
            return;
        }

        logger?.Info("Downloading and executing Claude Code installation script...");

        var exitCode = await ExecutePowershellCommand("https://claude.ai/install.ps1", logger: logger, ct: ct);

        if (exitCode == 0)
        {
            logger?.Success("Claude Code installed successfully");
        }
        else
        {
            logger?.Error($"Claude installation script exited with code {exitCode}");
        }
    }

    private void CreateProjectDirectory(string claudeProjectPath, ILogger logger)
    {
        if (!Directory.Exists(claudeProjectPath))
        {
            Directory.CreateDirectory(claudeProjectPath);
            logger?.Info($"Created project directory: {claudeProjectPath}");
        }
        else
        {
            logger?.Info($"Project directory already exists: {claudeProjectPath}");
        }
    }
}
