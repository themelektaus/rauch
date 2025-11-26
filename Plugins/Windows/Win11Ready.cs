namespace Rauch.Plugins.Windows;

[Command("win11ready", "Bypass Windows 11 hardware requirements for upgrades and installations")]
public class Win11Ready : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        if (!EnsureAdministrator(logger))
        {
            return;
        }

        logger?.Info("Bypassing Windows 11 hardware requirements...");

        var exitCodes = new List<int>();

        // Delete compatibility markers (clear failure records)
        logger?.Info("Clearing compatibility failure records...");
        exitCodes.Add(await StartProcess("reg", "delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\CompatMarkers\" /f", logger: logger, ct: ct));
        exitCodes.Add(await StartProcess("reg", "delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Shared\" /f", logger: logger, ct: ct));
        exitCodes.Add(await StartProcess("reg", "delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\TargetVersionUpgradeExperienceIndicators\" /f", logger: logger, ct: ct));

        // Set hardware compatibility flags
        logger?.Info("Setting hardware compatibility flags...");
        exitCodes.Add(await StartProcess(
            "reg",
            "add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\HwReqChk\" /f /v HwReqChkVars /t REG_MULTI_SZ /s , /d \"SQ_SecureBootCapable=TRUE,SQ_SecureBootEnabled=TRUE,SQ_TpmVersion=2,SQ_RamMB=8192,\"",
            logger: logger,
            ct: ct
        ));

        // Allow upgrades with unsupported TPM or CPU (Microsoft's official bypass)
        logger?.Info("Enabling Microsoft's official bypass policy...");
        exitCodes.Add(await StartProcess(
            "reg",
            "add \"HKLM\\SYSTEM\\Setup\\MoSetup\" /f /v AllowUpgradesWithUnsupportedTPMOrCPU /t REG_DWORD /d 1",
            logger: logger,
            ct: ct
        ));

        // Set upgrade eligibility for PC Health Check
        logger?.Info("Setting upgrade eligibility...");
        exitCodes.Add(await StartProcess(
            "reg",
            "add \"HKCU\\Software\\Microsoft\\PCHC\" /f /v UpgradeEligibility /t REG_DWORD /d 1",
            logger: logger,
            ct: ct
        ));

        // Bypass TPM, Secure Boot, and RAM checks for clean installations
        logger?.Info("Setting LabConfig bypass flags...");
        exitCodes.Add(await StartProcess("reg", "add \"HKLM\\SYSTEM\\Setup\\LabConfig\" /f /v BypassTPMCheck /t REG_DWORD /d 1", logger: logger, ct: ct));
        exitCodes.Add(await StartProcess("reg", "add \"HKLM\\SYSTEM\\Setup\\LabConfig\" /f /v BypassSecureBootCheck /t REG_DWORD /d 1", logger: logger, ct: ct));
        exitCodes.Add(await StartProcess("reg", "add \"HKLM\\SYSTEM\\Setup\\LabConfig\" /f /v BypassRAMCheck /t REG_DWORD /d 1", logger: logger, ct: ct));

        var successCount = exitCodes.Count(x => x == 0);
        var totalCount = exitCodes.Count;

        if (successCount == totalCount)
        {
            logger?.Success($"All {totalCount} registry modifications applied successfully");
        }
        else
        {
            logger?.Warning($"{successCount}/{totalCount} registry modifications applied successfully");
        }

        logger?.Exit(successCount == totalCount ? 0 : 1);
    }
}
