using NAudio.Wave;

namespace Rauch.Core;

public static class SoundPlayer
{
    static readonly HashSet<SoundEffect> soundEffects =
    [
        new("Enter") { duration = 0.7f },
        new("Error1") { duration = 0.4f },
        new("Error2") { duration = 0.6f },
        new("Error3") { duration = 0.7f },
        new("Granted") { duration = 0.7f },
        new("LevelUp") { duration = 1.4f },
        new("Nope") { duration = 0.8f },
        new("Reject") { duration = 0.7f },
        new("Success") { duration = 0.9f },
        new("Whip") { duration = 0.6f },
    ];

    public class SoundEffect(string name) : IDisposable
    {
        public readonly string name = name;

        readonly Stream stream = typeof(Program).Assembly.GetManifestResourceStream($"rauch.Sounds.{name}.wav");
        readonly WaveOutEvent outputDevice = new();

        public float volume = .8f;
        public float? duration;

        TaskCompletionSource tcs;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                outputDevice?.Dispose();
                stream?.Dispose();
            }
        }

        public async Task Play()
        {
            while (tcs is not null)
            {
                await Task.Delay(50);
            }

            tcs = new();

            using var stream = new MemoryStream();
            this.stream.CopyTo(stream);
            this.stream.Position = 0;
            stream.Position = 0;

            using var reader = new WaveFileReader(stream);
            outputDevice.Init(reader);
            outputDevice.Volume = volume;
            outputDevice.PlaybackStopped += OnPlaybackStopped;
            outputDevice.Play();

            if (duration is not null)
            {
                await Task.Delay(TimeSpan.FromSeconds(duration.Value));
                outputDevice.Stop();
            }

            await tcs.Task;

            tcs = null;

            void OnPlaybackStopped(object sender, StoppedEventArgs e)
            {
                outputDevice.PlaybackStopped -= OnPlaybackStopped;
                tcs.SetResult();
            }
        }
    }

    public static Task PlayError() => PlayInternal("Error1");
    public static Task PlayWarning() => PlayInternal("Nope");
    public static Task PlaySuccess() => PlayInternal("Success");
    public static Task PlayHelp() => PlayInternal("Whip");

    static readonly HashSet<Task> tasks = [];

    static Task PlayInternal(string name)
    {
        var soundEffect = soundEffects.FirstOrDefault(x => x.name == name);
        var task = soundEffect.Play();
        tasks.Add(task);
        return task;
    }

    public static async Task Wait()
    {
        await Task.WhenAll(tasks);
    }
}
