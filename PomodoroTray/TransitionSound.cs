using System;
using System.IO;
using System.Media;

namespace PomodoroTray
{
    /// <summary>
    /// Plays the sound for a phase transition: a user-chosen .wav file when one is set
    /// and playable, otherwise the default system sound.
    /// </summary>
    internal static class TransitionSound
    {
        // Kept alive while it plays; replaced on the next sound.
        private static SoundPlayer? _player;

        /// <summary>The .wav files that ship with the app, installed next to the exe.</summary>
        public static string BundledSoundsFolder => Path.Combine(AppContext.BaseDirectory, "Sounds");

        public static void Play(string? wavPath)
        {
            if (!TryPlayFile(wavPath))
            {
                PlayDefault();
            }
        }

        public static void PlayDefault() => SystemSounds.Exclamation.Play();

        /// <summary>
        /// Starts playing the file asynchronously. Returns false if no file is set, it no
        /// longer exists, or Windows can't play it (e.g. not a PCM/ADPCM/float WAV).
        /// </summary>
        public static bool TryPlayFile(string? wavPath)
        {
            if (string.IsNullOrWhiteSpace(wavPath) || !File.Exists(wavPath))
            {
                return false;
            }

            try
            {
                _player?.Stop();
                _player?.Dispose();
                _player = new SoundPlayer(wavPath);
                _player.Play();
                return true;
            }
            catch (Exception)
            {
                // Unsupported or unreadable file: the caller falls back to the default sound.
                return false;
            }
        }
    }
}
