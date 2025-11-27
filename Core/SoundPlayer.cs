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

    static int business;

    public class SoundEffect(string name) : IDisposable
    {
        public readonly string name = name;

        public float volume = .8f;
        public float? duration;

        DateTime lastPlaytime = DateTime.MinValue;

        readonly Stream stream = typeof(Program).Assembly.GetManifestResourceStream($"rauch.Sounds.{name}.wav");
        readonly WaveOutEvent outputDevice = new();

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

        public void Play()
        {
            if ((DateTime.Now - lastPlaytime).TotalMilliseconds < 100)
            {
                return;
            }

            lastPlaytime = DateTime.Now;

            business++;
            outputDevice.Stop();

            var stream = new MemoryStream();
            this.stream.CopyTo(stream);
            this.stream.Position = 0;
            stream.Position = 0;

            var reader = new WaveFileReader(stream);
            outputDevice.Init(reader);
            outputDevice.Volume = volume;
            outputDevice.Play();
            
            _ = Task.Run(async () =>
            {
                await Task.Delay(
                    duration.HasValue
                    ? TimeSpan.FromSeconds(duration.Value)
                    : reader.TotalTime
                );
                stream.Dispose();
                reader.Dispose();
                business--;
            });
        }
    }

    public static void PlayError() => PlayInternal("Error1");

    public static void PlayWarning() => PlayInternal("Nope");
    
    public static void PlaySuccess() => PlayInternal("Success");
    
    public static void PlayHelp() => PlayInternal("Whip");

    static void PlayInternal(string name)
    {
        soundEffects.FirstOrDefault(x => x.name == name).Play();
    }

    public static async Task WaitAndDispose()
    {
        while (business != 0)
        {
            await Task.Delay(100);
        }

        foreach (var soundEffect in soundEffects)
        {
            soundEffect.Dispose();
        }
    }
}
