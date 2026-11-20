using System;
using System.IO;
using System.Text.Json;

namespace PomodoroTray
{
    /// <summary>
    /// User-configurable timer intervals and behavior, persisted as JSON
    /// under %AppData%\PomodoroTray\settings.json.
    /// </summary>
    public class PomodoroSettings
    {
        public int WorkMinutes { get; set; } = 25;
        public int ShortBreakMinutes { get; set; } = 5;
        public int LongBreakMinutes { get; set; } = 15;
        public int SessionsBeforeLongBreak { get; set; } = 4;
        public bool AutoStartNext { get; set; } = true;
        public bool PlaySound { get; set; } = true;

        // Off unless the user turns it on; settings files from before this option load as off too.
        public bool ShowNotification { get; set; }

        // Full paths to .wav files played at each transition; null means the default system sound.
        public string? ShortBreakStartSound { get; set; }
        public string? LongBreakStartSound { get; set; }
        public string? WorkStartSound { get; set; }

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PomodoroTray",
            "settings.json");

        public static PomodoroSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var loaded = JsonSerializer.Deserialize<PomodoroSettings>(json);
                    if (loaded != null)
                    {
                        return loaded;
                    }
                }
            }
            catch
            {
                // Corrupt or unreadable settings file: fall back to defaults.
            }

            return new PomodoroSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // Persistence is best-effort; the app keeps running with in-memory settings.
            }
        }

        public PomodoroSettings Clone() => new()
        {
            WorkMinutes = WorkMinutes,
            ShortBreakMinutes = ShortBreakMinutes,
            LongBreakMinutes = LongBreakMinutes,
            SessionsBeforeLongBreak = SessionsBeforeLongBreak,
            AutoStartNext = AutoStartNext,
            PlaySound = PlaySound,
            ShowNotification = ShowNotification,
            ShortBreakStartSound = ShortBreakStartSound,
            LongBreakStartSound = LongBreakStartSound,
            WorkStartSound = WorkStartSound
        };
    }
}
