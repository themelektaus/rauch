using Microsoft.Win32;

namespace Rauch.Plugins.Outlook;

[Command("fix")]
public class Fix : ICommand
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        var _EnableADAL = logger?.Question("Enable ADAL", ["yes", "no", "null"], "null");
        SetCurrentUser(
            Registry.CurrentUser,
            @"Software\Microsoft\Office\16.0\Common\Identity",
            "EnableADAL",
            _EnableADAL == "yes" ? 1 : (_EnableADAL == "no" ? 0 : null),
            logger
        );

        var _DisableADALatopWAMOverride = logger?.Question("Disable ADAL-atop WAM Override", ["yes", "no", "null"], "null");
        SetCurrentUser(
            Registry.CurrentUser,
            @"Software\Microsoft\Office\16.0\Common\Identity",
            "DisableADALatopWAMOverride",
            _DisableADALatopWAMOverride == "yes" ? 1 : (_DisableADALatopWAMOverride == "no" ? 0 : null),
            logger
        );

        var _ExcludeExplicitO365Endpoint = logger?.Question("Exclude Explicit O365 Endpoint", ["yes", "no", "null"], "null");
        SetCurrentUser(
            Registry.CurrentUser,
            @"Software\Microsoft\Office\16.0\Outlook\Autodiscover",
            "ExcludeExplicitO365Endpoint",
            _ExcludeExplicitO365Endpoint == "yes" ? 1 : (_ExcludeExplicitO365Endpoint == "no" ? 0 : null),
            logger
        );

        var _ExcludeHttpsRootDomain = logger?.Question("Exclude Https Root Domain", ["yes", "no", "null"], "null");
        SetCurrentUser(
            Registry.CurrentUser,
            @"Software\Microsoft\Office\16.0\Outlook\Autodiscover",
            "ExcludeHttpsRootDomain",
            _ExcludeHttpsRootDomain == "yes" ? 1 : (_ExcludeHttpsRootDomain == "no" ? 0 : null),
            logger
        );
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    static void SetCurrentUser(RegistryKey root, string path, string name, int? value, ILogger logger)
    {
        try
        {
            using var key = root.CreateSubKey(path);

            if (value.HasValue)
            {
                key.SetValue(name, value.Value, RegistryValueKind.DWord);
            }
            else
            {
                key.DeleteValue(name);
            }

            logger?.Success(nameof(logger.Success));
        }
        catch (ArgumentException ex)
        {
            logger?.Warning(ex.Message);
        }
        catch (Exception ex)
        {
            logger?.Error(ex.Message);
        }
    }
}
