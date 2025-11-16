namespace Rauch.Plugins.Install;

[Command("claude", "Install Claude Code and portable Git Bash")]
public class Claude : ICommand
{
    const string PORTABLE_GIT_ZIP_URL = "https://cloud.it-guards.at/download/PortableGit.zip";
    const string CLAUDE_INSTALL_SCRIPT_URL = "https://claude.ai/install.ps1";

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
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
            await InstallGitBash(claudeGitPath, claudeGitBashExe, logger, cancellationToken);

            // Install Claude if needed
            await InstallClaude(claudePath, claudeExe, logger, cancellationToken);

            // Create project directory
            CreateProjectDirectory(claudeProjectPath, logger);

            logger?.Success("Claude Code installation completed successfully.");
            logger?.Info($"Project directory: {claudeProjectPath}");

            // Start the process
            StartProcess(claudeExe, logger);
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

    private async Task InstallGitBash(string claudeGitPath, string claudeGitBashExe, ILogger logger, CancellationToken cancellationToken)
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
            await DownloadFile(PORTABLE_GIT_ZIP_URL, zipPath, cancellationToken, logger);

            // Extract PortableGit.zip
            await Unzip(zipPath, claudeGitPath, cancellationToken, logger);

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

    private async Task InstallClaude(string claudePath, string claudeExe, ILogger logger, CancellationToken cancellationToken)
    {
        if (File.Exists(claudeExe))
        {
            logger?.Info($"Claude is already installed at {claudePath}");
            return;
        }

        logger?.Info("Installing Claude Code...");

        try
        {
            // Download install script
            var tempScriptPath = Path.GetTempFileName() + ".ps1";
            await DownloadFile(CLAUDE_INSTALL_SCRIPT_URL, tempScriptPath, cancellationToken, logger);
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            var scriptContent = await httpClient.GetStringAsync(CLAUDE_INSTALL_SCRIPT_URL, cancellationToken);

            // Execute PowerShell script
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{tempScriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);

                await process.WaitForExitAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(output))
                {
                    logger?.Info(output.Trim());
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    logger?.Warning(error.Trim());
                }

                if (process.ExitCode == 0)
                {
                    logger?.Success("Claude Code installed successfully");
                }
                else
                {
                    logger?.Error($"Claude installation script exited with code {process.ExitCode}");
                }
            }

            // Cleanup temp script
            if (File.Exists(tempScriptPath))
            {
                File.Delete(tempScriptPath);
            }
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed to install Claude: {ex.Message}");
            throw;
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
