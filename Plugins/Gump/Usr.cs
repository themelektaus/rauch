namespace Rauch.Plugins.Gump;

[Command("usr")]
public class Usr : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        async Task Run(string powershellCommand)
        {
            await ExecutePowershellCommand(powershellCommand, CommandFlags.NoProfile, logger: logger, ct: ct);
        }

        var windowsLanguage = logger?.Question("UI and Keyboard Language", null, "de-AT");
        {
            await Run($"Set-WinUserLanguageList {windowsLanguage} -Force");
        }

        if (logger?.Question("Remove Content Delivery Manager", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"Remove-Item -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" -Force -Recurse");
        }

        if (logger?.Question("Enable Classic right Click for W11", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"New-Item -Path ""HKCU:\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"" -Value """" -Force");
        }

        if (logger?.Question("Adjust Windows Search", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""AllowCortana"" -Type DWord -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""CortanaConsent"" -Type DWord -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""BingSearchEnabled"" -Type DWord -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""AllowSearchToUseLocation"" -Type DWord -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""DeviceHistoryEnabled"" -Type DWord -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""HistoryViewEnabled"" -Type DWord -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""SearchboxTaskbarMode"" -Type DWord -Value 0");
        }

        var showHiddenFiles = logger?.Question("Show Hidden Files", ["yes", "no"], "no");
        if (showHiddenFiles == "yes")
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" -Name ""Hidden"" -Type DWord -Value 1");
        }
        else if (showHiddenFiles == "no")
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" -Name ""Hidden"" -Type DWord -Value 2");
        }

        if (logger?.Question("Launch to \"This PC\"", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" -Name ""LaunchTo"" -Type DWord -Value 1");
        }

        var showFileExtensions = logger?.Question("Show File Extensions", ["yes", "no"], "yes");
        if (showFileExtensions == "yes")
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" -Name ""HideFileExt"" -Type DWord -Value 0");
        }
        else if (showFileExtensions == "no")
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" -Name ""HideFileExt"" -Type DWord -Value 1");
        }

        var enableTransperency = logger?.Question("Enable Transparency", ["yes", "no"], "no");
        if (enableTransperency == "yes")
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"" -Name ""EnableTransparency"" -Type DWord -Value 1");
        }
        else if (enableTransperency == "no")
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"" -Name ""EnableTransparency"" -Type DWord -Value 0");
        }

        var systemTheme = logger?.Question("System Theme", ["light", "dark"], "dark");
        if (systemTheme == "light")
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"" -Name ""SystemUsesLightTheme"" -Type DWord -Value 1");
        }
        else if (systemTheme == "dark")
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"" -Name ""SystemUsesLightTheme"" -Type DWord -Value 0");
        }

        var appTheme = logger?.Question("App Theme", ["light", "dark"], "light");
        if (appTheme == "light")
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"" -Name ""AppsUseLightTheme"" -Type DWord -Value 1");
        }
        else if (appTheme == "dark")
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"" -Name ""AppsUseLightTheme"" -Type DWord -Value 0");
        }

        if (logger?.Question("Disable Mouse Acceleration", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Control Panel\Mouse"" -Name ""MouseSpeed"" -Value 0");
        }

        if (logger?.Question("Restart Explorer", ["yes", "no"], "yes") == "yes")
        {
            await Run(@"Stop-Process -Name ""Explorer"" -Force");
        }
    }
}
