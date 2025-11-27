using IniParser;
using NAudio.Wave;
using System.Text.RegularExpressions;

namespace Rauch.Core;

public static class SoundPlayer
{
    static int business;

    static readonly HashSet<SoundEffect> soundEffects = [];

    public static void LoadSounds()
    {
        foreach (var resourceName in typeof(SoundPlayer).Assembly.GetManifestResourceNames())
        {
            if (Regex.Match(resourceName, @"^rauch.Sounds\.(.+?)\.wav$").Groups[1].Value is string name && !string.IsNullOrEmpty(name))
            {
                soundEffects.Add(new(name));
            }
        }
    }

    public static void Play(string name)
    {
        soundEffects.FirstOrDefault(x => x.name == name)?.Play();
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

    public class SoundEffect : IDisposable
    {
        public readonly string name;

        public float volume = .8f;
        public float? duration;

        DateTime lastPlaytime = DateTime.MinValue;

        readonly Stream stream;
        readonly WaveOutEvent outputDevice = new();

        public SoundEffect(string name)
        {
            this.name = name;

            var assembly = typeof(Program).Assembly;
            stream = assembly.GetManifestResourceStream($"rauch.Sounds.{name}.wav");

            using var iniStream = assembly.GetManifestResourceStream($"rauch.Sounds.{name}.wav.ini");
            using var reader = new StreamReader(iniStream);
            var parser = new FileIniDataParser();
            var properties = parser.ReadData(reader).Sections["Properties"];
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            this.volume = float.TryParse(properties["Volume"], culture, out var volume) ? volume : 0.8f;
            this.duration = float.TryParse(properties["Duration"], culture, out var duration) ? duration : null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                stream?.Dispose();
                outputDevice?.Dispose();
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
}
