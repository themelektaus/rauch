using NetPing = System.Net.NetworkInformation.Ping;
using IPStatus = System.Net.NetworkInformation.IPStatus;

namespace Rauch.Commands.Run;

[Name("ping")]
[MinArguments(1)]
public class Ping : ICommand
{
    const string Separator = "-----------------------------------------------------------------------";
    const string StatusOk = "     OK";
    const string StatusOffline = "OFFLINE";
    const string TimestampFormat = "[dd.MM.yyyy] [HH:mm:ss]";
    static readonly TimeSpan PingTimeout = TimeSpan.FromMilliseconds(1900);
    static readonly TimeSpan KeyPollInterval = TimeSpan.FromMilliseconds(100);
    static readonly TimeSpan RoundInterval = TimeSpan.FromSeconds(2);

    sealed class HostState
    {
        public string Name = "";
        public string PaddedName = "";
        public NetPing Ping = null;
        public bool IsNew = true;
        public bool WasOnline;
        public bool IsOnline;
    }

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetService<ILogger>();

        // Pro Host eine wiederverwendete Ping-Instanz – das spart bei Multi-Tage-Laeufen
        // sehr viele Socket-/Buffer-Allokationen (eine Ping-Instanz haelt einen internen
        // Buffer + Async-State).
        var hosts = new HostState[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            hosts[i] = new HostState
            {
                Name = args[i],
                PaddedName = args[i].PadRight(35),
                Ping = new NetPing()
            };
        }

        // Vorallokiertes Task-Array – wird pro Iteration nur ueberschrieben, nie neu erzeugt.
        var pingTasks = new Task[hosts.Length];

        logger?.Write(Separator);
        logger?.Write(" Press [Escape] to quit ");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var roundTimer = new PeriodicTimer(RoundInterval);

        // Eigener Hintergrund-Task fuer die Escape-Erkennung. Wird einmal gestartet,
        // pollt allokationsfrei via PeriodicTimer und cancelt cts bei Druck.
        // Vorteile gegenueber Polling im Hot-Loop:
        //   - keine Vermischung von "warten" + "Tastatur abfragen"
        //   - kein try/catch als Control-Flow im Round-Loop
        //   - PeriodicTimer.WaitForNextTickAsync gibt ValueTask zurueck (keine Task-Allokation pro Tick)
        var keyWatcher = WatchEscapeAsync(cts);

        try
        {
            while (!cts.IsCancellationRequested)
            {
                // Alle Hosts parallel pingen – Ergebnisse landen direkt im HostState.
                for (var i = 0; i < hosts.Length; i++)
                {
                    pingTasks[i] = PingHostAsync(hosts[i], cts.Token);
                }

                try
                {
                    await Task.WhenAll(pingTasks);
                }
                catch
                {
                    // Einzelne Ping-Fehler sind bereits in IsOnline reflektiert.
                }

                if (cts.IsCancellationRequested)
                {
                    break;
                }

                Redraw(hosts, logger);

                // Auf naechste Runde warten. WaitForNextTickAsync wirft OCE bei Cancel
                // (z.B. wenn der Key-Watcher Escape erkannt hat) – sauberer Loop-Exit.
                if (!await WaitNextRoundAsync(roundTimer, cts.Token))
                {
                    break;
                }
            }
        }
        finally
        {
            cts.Cancel(); // beendet den Key-Watcher
            try { await keyWatcher; } catch { /* OCE vom Watcher ignorieren */ }

            for (var i = 0; i < hosts.Length; i++)
            {
                hosts[i].Ping.Dispose();
            }
            Console.ResetColor();
        }

        logger?.Exit(0);
    }

    static void Redraw(HostState[] hosts, ILogger logger)
    {
        // Dirty-Check ohne LINQ (keine Closure/Delegate-Allokation).
        var dirty = false;
        for (var i = 0; i < hosts.Length; i++)
        {
            var h = hosts[i];
            if (h.IsNew || h.WasOnline != h.IsOnline)
            {
                dirty = true;
                break;
            }
        }

        if (!dirty)
        {
            return;
        }

        logger?.Write(Separator);

        // Timestamp einmal pro Redraw bauen, nicht pro Host.
        var timestamp = DateTime.Now.ToString(TimestampFormat).PadRight(25);

        for (var i = 0; i < hosts.Length; i++)
        {
            var host = hosts[i];
            host.IsNew = false;
            host.WasOnline = host.IsOnline;

            var color = host.IsOnline ? ConsoleColor.Green : ConsoleColor.Red;
            var status = host.IsOnline ? StatusOk : StatusOffline;

            logger?.Write($" {timestamp} {host.PaddedName} {status}", color: color);
        }
    }

    static async Task<bool> WaitNextRoundAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    static async Task WatchEscapeAsync(CancellationTokenSource cts)
    {
        using var timer = new PeriodicTimer(KeyPollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
                {
                    cts.Cancel();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Linked Token wurde gecancelt (z.B. weil der Hauptloop endet) – sauberer Exit.
        }
    }

    static async Task PingHostAsync(HostState host, CancellationToken ct)
    {
        try
        {
            var reply = await host.Ping.SendPingAsync(host.Name, PingTimeout, cancellationToken: ct);
            host.IsOnline = reply.Status == IPStatus.Success;
        }
        catch
        {
            host.IsOnline = false;
        }
    }
}
