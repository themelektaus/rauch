namespace Rauch.Plugins.Gump;

[Command("basic")]
public class Basic : ICommand
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
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

        var windowsLanguage = logger?.Question("Windows Lanugage", null, "de-AT");
        {
            await Run($"Set-WinSystemLocale {windowsLanguage}");
            await Run($"Set-WinUILanguageOverride -Language {windowsLanguage}");
        }

        if (logger?.Question("Enable local administrator", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"Enable-LocalUser ""Administrator""");
            await Run(@"Get-LocalUser | Set-LocalUser -PasswordNeverExpires $true");
        }

        if (logger?.Question("Disable users W10 and W11", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"Disable-LocalUser ""W10"" -ErrorAction SilentlyContinue");
            await Run(@"Disable-LocalUser ""W11"" -ErrorAction SilentlyContinue");
            await Run(@"Get-LocalUser | Set-LocalUser -PasswordNeverExpires $true");
        }

        if (logger?.Question("Disable IPv6", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"Disable-NetAdapterBinding -Name * -ComponentID ""ms_tcpip6""");
        }

        if (logger?.Question("Delay NLA service", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"sc.exe config NlaSvc start=delayed-auto");
        }

        if (logger?.Question("Enable firewall rules (RDP, SMB, ...)", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"Enable-NetFirewallRule -DisplayGroup ""Remotedesktop""");
            await Run(@"Enable-NetFirewallRule -DisplayGroup ""Netzwerkerkennung""");
            await Run(@"Enable-NetFirewallRule -DisplayGroup ""Datei- und Druckerfreigabe""");
        }

        if (logger?.Question("Add \"Jeder\" to remote desktop users", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"Add-LocalGroupMember -Group ""Remotedesktopbenutzer"" -Member ""Jeder"" -ErrorAction SilentlyContinue");
        }

        if (logger?.Question("Disable some telemetry", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"New-Item -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" -ErrorAction SilentlyContinue");
            await Run(@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" -Name ""AllowTelemetry"" -Value 0 -Force -ErrorAction SilentlyContinue");
            await Run(@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" -Name ""DisableOneSettingsDownloads"" -Value 1 -Force -ErrorAction SilentlyContinue");
            await Run(@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" -Name ""DoNotShowFeedbackNotifications"" -Value 1 -Force -ErrorAction SilentlyContinue");
        }

        if (logger?.Question("Adjust Windows Search", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"New-Item -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" -ErrorAction SilentlyContinue");
            await Run(@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" -Name ""AllowCloudSearch"" - Value 0 -Force -ErrorAction SilentlyContinue");
            await Run(@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" -Name ""AllowCortana"" -Value 0 -Force -ErrorAction SilentlyContinue");
            await Run(@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" -Name ""AllowCortanaAboveLock"" - Value 0 -Force -ErrorAction SilentlyContinue");
            await Run(@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" -Name ""AllowSearchToUseLocation"" -Value 0 -Force -ErrorAction SilentlyContinue");
            await Run(@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" -Name ""ConnectedSearchUseWeb"" - Value 0 -Force -ErrorAction SilentlyContinue");
            await Run(@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" - Name ""DisableWebSearch"" -Value 1 -Force -ErrorAction SilentlyContinue");
        }

        if (logger?.Question("Disable search box suggestions", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"New-Item -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\Explorer"" -ErrorAction SilentlyContinue");
            await Run(@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\Explorer"" -Name ""DisableSearchBoxSuggestions"" -Value 1 -Force -ErrorAction SilentlyContinue");
        }

        if (logger?.Question("Enable RDP", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"Set-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server"" -Name ""fDenyTSConnections"" -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp"" -Name ""UserAuthentication"" -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp"" -Name ""SecurityLayer"" -Value 1");
        }

        if (logger?.Question("Disable Widgets", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"New-Item -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Dsh"" -ErrorAction SilentlyContinue");
            await Run(@"Set-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Dsh"" -Name ""AllowNewsAndInterests"" -Value 0 -Force -ErrorAction SilentlyContinue");
            await Run(@"Set-ItemProperty -Path ""HKLM:\SOFTWARE\Microsoft\PolicyManager\default\NewsAndInterests\AllowNewsAndInterests"" -Name ""value"" -Value 1");
        }

        if (logger?.Question("Disable Hibernation", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"Set-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Power"" -Name ""HiberbootEnabled"" -Value 0");
        }

        if (logger?.Question("Disable Logon Background Image", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"New-Item -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\System"" -ErrorAction SilentlyContinue");
            await Run(@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\System"" -Name ""DisableLogonBackgroundImage"" -Value 1 -Force -ErrorAction SilentlyContinue");
        }

        if (logger?.Question("Disable Lockscreen", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"New-Item -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\Personalization"" -ErrorAction SilentlyContinue");
            await Run(@"New-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Windows\Personalization"" -Name ""NoLockScreen"" -Value 1 -Force -ErrorAction SilentlyContinue");
        }

        if (logger?.Question("Set Powerbutton to Shutdown", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"powercfg /setacvalueindex SCHEME_CURRENT SUB_BUTTONS PBUTTONACTION 3");
            await Run(@"powercfg /setdcvalueindex SCHEME_CURRENT SUB_BUTTONS PBUTTONACTION 3");
        }

        if (logger?.Question("Restart Explorer", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"Stop-Process -Name ""Explorer"" -Force");
        }
    }
}
