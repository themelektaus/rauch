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

        var windowsLanguage = (logger?.Choice("UI and Keyboard Language", ["de-AT", "de-DE", "de-US", "custom"], 0)) switch
        {
            0 => "de-AT",
            1 => "de-DE",
            2 => "en-US",
            _ => logger?.Question("Enter UI and Keyboard Language:", allowEmpty: true),
        };

        if (windowsLanguage != string.Empty)
        {
            await Run($"Set-WinUserLanguageList {windowsLanguage} -Force");
        }

        if (logger?.Choice("Remove Content Delivery Manager", ["yes", "no"]) == 0)
        {
            await Run(@"Remove-Item -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" -Force -Recurse");
        }

        if (logger?.Choice("Enable Classic right Click for W11", ["yes", "no"]) == 0)
        {
            await Run(@"New-Item -Path ""HKCU:\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"" -Value """" -Force");
        }

        if (logger?.Choice("Adjust Windows Search", ["yes", "no"]) == 0)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""AllowCortana"" -Type DWord -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""CortanaConsent"" -Type DWord -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""BingSearchEnabled"" -Type DWord -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""AllowSearchToUseLocation"" -Type DWord -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""DeviceHistoryEnabled"" -Type DWord -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""HistoryViewEnabled"" -Type DWord -Value 0");
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Search"" -Name ""SearchboxTaskbarMode"" -Type DWord -Value 0");
        }

        var showHiddenFiles = logger?.Choice("Show Hidden Files", ["yes", "no"], 1);
        if (showHiddenFiles == 0)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" -Name ""Hidden"" -Type DWord -Value 1");
        }
        else if (showHiddenFiles == 1)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" -Name ""Hidden"" -Type DWord -Value 2");
        }

        if (logger?.Choice("Launch to \"This PC\"", ["yes", "no"]) == 0)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" -Name ""LaunchTo"" -Type DWord -Value 1");
        }

        var showFileExtensions = logger?.Choice("Show File Extensions", ["yes", "no"]);
        if (showFileExtensions == 0)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" -Name ""HideFileExt"" -Type DWord -Value 0");
        }
        else if (showFileExtensions == 1)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" -Name ""HideFileExt"" -Type DWord -Value 1");
        }

        var enableTransperency = logger?.Choice("Enable Transparency", ["yes", "no"], 1);
        if (enableTransperency == 0)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"" -Name ""EnableTransparency"" -Type DWord -Value 1");
        }
        else if (enableTransperency == 1)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"" -Name ""EnableTransparency"" -Type DWord -Value 0");
        }

        var systemTheme = logger?.Choice("System Theme", ["light", "dark"], 1);
        if (systemTheme == 0)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"" -Name ""SystemUsesLightTheme"" -Type DWord -Value 1");
        }
        else if (systemTheme == 1)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"" -Name ""SystemUsesLightTheme"" -Type DWord -Value 0");
        }

        var appTheme = logger?.Choice("App Theme", ["light", "dark"], 0);
        if (appTheme == 0)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"" -Name ""AppsUseLightTheme"" -Type DWord -Value 1");
        }
        else if (appTheme == 1)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"" -Name ""AppsUseLightTheme"" -Type DWord -Value 0");
        }

        if (logger?.Choice("Disable Mouse Acceleration", ["yes", "no"]) == 0)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Control Panel\Mouse"" -Name ""MouseSpeed"" -Value 0");
        }

        if (logger?.Choice("Restart Explorer", ["yes", "no"]) == 0)
        {
            await Run(@"Stop-Process -Name ""Explorer"" -Force");
        }
    }
}
