using System;
using System.IO;
using System.Speech.Synthesis;
using System.Windows.Media;

namespace Eco_Matic.Utilities
{
    public static class AudioService
    {
        private static readonly SpeechSynthesizer _synthesizer;
        private static readonly MediaPlayer _bgmPlayer;
        private static readonly MediaPlayer _sfxPlayer;
        private static string? _currentBgmPath;
        
        static AudioService()
        {
            _synthesizer = new SpeechSynthesizer();
            // Try to set a modern female voice if available, otherwise default
            _synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
            _synthesizer.Rate = 1; // Normal speed

            _bgmPlayer = new MediaPlayer();
            _bgmPlayer.Volume = 0.5;
            _bgmPlayer.MediaEnded += (s, e) =>
            {
                // Loop the BGM
                _bgmPlayer.Position = TimeSpan.Zero;
                _bgmPlayer.Play();
            };

            _sfxPlayer = new MediaPlayer();
            _sfxPlayer.Volume = 0.8;
        }

        /// <summary>
        /// Speaks the given text asynchronously so it doesn't block the UI thread.
        /// </summary>
        public static void SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            
            // Cancel any ongoing speech so they don't overlap awkwardly
            _synthesizer.SpeakAsyncCancelAll();
            _synthesizer.SpeakAsync(text);
        }

        /// <summary>
        /// Plays background music from a file path. Avoids restarting if already playing.
        /// </summary>
        public static void PlayBackgroundMusic(string filePath)
        {
            if (File.Exists(filePath))
            {
                string fullPath = Path.GetFullPath(filePath);
                if (_currentBgmPath == fullPath) return;

                _currentBgmPath = fullPath;
                _bgmPlayer.Open(new Uri(fullPath));
                _bgmPlayer.Play();
            }
        }

        /// <summary>
        /// Stops the background music.
        /// </summary>
        public static void StopBackgroundMusic()
        {
            _bgmPlayer.Stop();
            _currentBgmPath = null;
        }

        /// <summary>
        /// Plays a short sound effect.
        /// </summary>
        public static void PlaySfx(string filePath)
        {
            if (File.Exists(filePath))
            {
                string fullPath = Path.GetFullPath(filePath);
                _sfxPlayer.Open(new Uri(fullPath));
                _sfxPlayer.Play();
            }
        }
    }
}
