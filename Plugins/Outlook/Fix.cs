using Microsoft.Win32;

namespace Rauch.Plugins.Outlook;

[Command("fix")]
public class Fix : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        var _EnableADAL = logger?.Choice("Enable ADAL", ["yes", "no", "none"], 2);
        SetCurrentUser(
            Registry.CurrentUser,
            @"Software\Microsoft\Office\16.0\Common\Identity",
            "EnableADAL",
            _EnableADAL == 0 ? 1 : (_EnableADAL == 1 ? 0 : null),
            logger
        );

        var _DisableADALatopWAMOverride = logger?.Choice("Disable ADAL-atop WAM Override", ["yes", "no", "none"], 2);
        SetCurrentUser(
            Registry.CurrentUser,
            @"Software\Microsoft\Office\16.0\Common\Identity",
            "DisableADALatopWAMOverride",
            _DisableADALatopWAMOverride == 0 ? 1 : (_DisableADALatopWAMOverride == 1 ? 0 : null),
            logger
        );

        var _ExcludeExplicitO365Endpoint = logger?.Choice("Exclude Explicit O365 Endpoint", ["yes", "no", "none"], 2);
        SetCurrentUser(
            Registry.CurrentUser,
            @"Software\Microsoft\Office\16.0\Outlook\Autodiscover",
            "ExcludeExplicitO365Endpoint",
            _ExcludeExplicitO365Endpoint == 0 ? 1 : (_ExcludeExplicitO365Endpoint == 1 ? 0 : null),
            logger
        );

        var _ExcludeHttpsRootDomain = logger?.Choice("Exclude Https Root Domain", ["yes", "no", "none"], 2);
        SetCurrentUser(
            Registry.CurrentUser,
            @"Software\Microsoft\Office\16.0\Outlook\Autodiscover",
            "ExcludeHttpsRootDomain",
            _ExcludeHttpsRootDomain == 0 ? 1 : (_ExcludeHttpsRootDomain == 1 ? 0 : null),
            logger
        );
    }

    [OS("windows")]
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
