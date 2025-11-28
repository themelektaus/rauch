using IniParser;
using IniParser.Model;

namespace Rauch.Core;

public static class ConfigIni
{
    const string INI = "config.ini";

    static readonly SemaphoreSlim @lock = new(1, 1);

    static void Lock(ILogger logger = default)
    {
        logger?.Debug($"{nameof(ConfigIni)}.{nameof(Lock)}()");
        @lock.Wait();
    }

    static void Release(ILogger logger = default)
    {
        logger?.Debug($"{nameof(ConfigIni)}.{nameof(Release)}()");
        @lock.Release();
    }

    public static T Read<T>(Func<IniData, T> getter, ILogger logger = default)
    {
        Lock(logger);

        try
        {
            return getter(ReadInternal().data);
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed: {ex.Message}");
            return default;
        }
        finally
        {
            Release(logger);
        }
    }

    public static void Edit(Action<IniData> action, ILogger logger = default)
    {
        Lock(logger);

        try
        {
            var (parser, data) = ReadInternal();
            action(data);
            data.Configuration.AssigmentSpacer = string.Empty;
            parser.WriteFile(INI, data);
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed: {ex.Message}");
        }
        finally
        {
            Release(logger);
        }
    }

    static (FileIniDataParser parser, IniData data) ReadInternal()
    {
        if (!File.Exists(INI))
        {
            File.Create(INI).Dispose();
        }

        var parser = new FileIniDataParser();
        return (parser, parser.ReadFile(INI));
    }
}
