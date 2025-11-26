namespace Rauch.Core;

public static class SoundPlayer
{
    public static Task PlayEnter() => Play("Enter", 700);
    public static Task PlayError1() => Play("Error1", 400);
    public static Task PlayError2() => Play("Error2", 600);
    public static Task PlayError3() => Play("Error3", 700);
    public static Task PlayGranted() => Play("Granted", 700);
    public static Task PlayLevelUp() => Play("LevelUp", 1400);
    public static Task PlayNope() => Play("Nope", 800);
    public static Task PlayReject() => Play("Reject", 700);
    public static Task PlaySuccess() => Play("Success", 900);
    public static Task PlayWhip() => Play("Whip", 600);

    static readonly HashSet<Task> tasks = new();

    static Task Play(string name, int duration = 0)
    {
        var task = Task.Run(async () =>
        {
            using (var stream = typeof(Program).Assembly.GetManifestResourceStream($"rauch.Sounds.{name}.wav"))
            using (var player = new System.Media.SoundPlayer(stream))
            {
                if (duration > 0)
                {
                    player.Play();
                    await Task.Delay(duration);
                }
                else
                {
                    player.PlaySync();
                }
            }
        });
        tasks.Add(task);
        return task;
    }

    public static async Task Wait()
    {
        await Task.WhenAll(tasks);
    }
}
