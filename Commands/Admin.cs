namespace Rauch.Commands;

[Name("admin")]
public class Admin : ICommand
{
    [OS("windows")]
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetService<ILogger>();

        if (EnsureAdministrator())
        {
            logger?.Warning("Already running as administrator");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = $"/k cd /d \"{Environment.CurrentDirectory}\"",
                Verb = "runas",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            logger?.Error(ex.Message);
        }
    }
}
