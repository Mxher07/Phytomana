using Engine;
using Engine.Audio;
using Engine.Media;

namespace Game {
    public static class MusicManager {
        public enum Mix {
            None = 0, //没有正在播放的音乐，停止音乐
            Menu = 1, //主菜单音乐
            InGame = 2, //游戏游玩时的音乐，由模组自定义(API1.72新增)
            Other = 3 //其他，由模组自己来定义(API1.72新增)
        }

        public static float m_fadeSpeed = 0.33f;

        public static float m_fadeWait = 2f;

        public static StreamingSound m_fadeSound;

        public static StreamingSound m_sound;

        public static double m_fadeStartTime;

        public static Mix m_currentMix;

        public static double m_nextSongTime;

        public static Random m_random = new();

        public static float? m_volume;

        public static Mix CurrentMix {
            get => m_currentMix;
            set {
                if (value != m_currentMix) {
                    m_currentMix = value;
                    m_nextSongTime = 0.0;
                }
            }
        }

        public static bool IsPlaying {
            get {
                if (m_sound != null) {
                    return m_sound.State != SoundState.Stopped;
                }
                return false;
            }
        }

        public static float Volume {
            get {
                if (m_volume.HasValue) {
                    return m_volume.Value;
                }
                return SettingsManager.MusicVolume * 0.6f;
            }
            set => m_volume = value;
        }

        public static void ChangeMenuMusic() {
            float startPercentage = IsPlaying ? m_random.Float(0f, 0.75f) : 0f;
            string ContentMusicPath = string.Empty;
            ModsManager.HookAction(
                "MenuPlayMusic",
                loader => {
                    loader.MenuPlayMusic(out ContentMusicPath);
                    return false;
                }
            );
            if (!string.IsNullOrEmpty(ContentMusicPath)) {
                PlayMusic(ContentMusicPath, startPercentage);
                m_nextSongTime = Time.FrameStartTime + m_random.Float(40f, 60f);
                return;
            }
            switch (m_random.Int(0, 5)) {
                case 0: PlayMusic("Music/NativeAmericanFluteSpirit", startPercentage); break;
                case 1: PlayMusic("Music/AloneForever", startPercentage); break;
                case 2: PlayMusic("Music/NativeAmerican", startPercentage); break;
                case 3: PlayMusic("Music/NativeAmericanHeart", startPercentage); break;
                case 4: PlayMusic("Music/NativeAmericanPeaceFlute", startPercentage); break;
                case 5: PlayMusic("Music/NativeIndianChant", startPercentage); break;
            }
            m_nextSongTime = Time.FrameStartTime + m_random.Float(40f, 60f);
        }

        public static void Update() {
            if (m_fadeSound != null) {
                m_fadeSound.Volume = MathUtils.Min(m_fadeSound.Volume - m_fadeSpeed * Volume * Time.FrameDuration, Volume);
                if (m_fadeSound.Volume <= 0f) {
                    m_fadeSound.Dispose();
                    m_fadeSound = null;
                }
            }
            if (m_sound != null
                && Time.FrameStartTime >= m_fadeStartTime) {
                m_sound.Volume = MathUtils.Min(m_sound.Volume + m_fadeSpeed * Volume * Time.FrameDuration, Volume);
            }
            if (m_currentMix == Mix.None
                || Volume == 0f) {
                StopMusic();
            }
            else if (m_currentMix == Mix.Menu
                && (Time.FrameStartTime >= m_nextSongTime || !IsPlaying)) {
                ChangeMenuMusic();
            }
            else if (m_currentMix == Mix.InGame) {
                ModsManager.HookAction(
                    "PlayInGameMusic",
                    loader => {
                        loader.PlayInGameMusic();
                        return false;
                    }
                );
            }
            else if (m_currentMix == Mix.Other) { }
        }

        public static void Initialize() {
            Window.Closed += delegate {
                try {
                    Utilities.Dispose(ref m_sound);
                    Utilities.Dispose(ref m_fadeSound);
                }
                catch {
                    // ignored
                }
            };
        }

        public static void PlayMusic(string name, float startPercentage) {
            if (string.IsNullOrEmpty(name)) {
                StopMusic();
            }
            else {
                try {
                    StopMusic();
                    m_fadeStartTime = Time.FrameStartTime + m_fadeWait;
                    float volume = m_fadeSound != null ? 0f : Volume;
                    StreamingSource streamingSource = ContentManager.Get<StreamingSource>(name);
                    streamingSource = streamingSource.Duplicate();
                    streamingSource.Position = (long)(MathUtils.Saturate(startPercentage)
                            * (streamingSource.BytesCount / streamingSource.ChannelsCount / 2))
                        / 16
                        * 16;
                    m_sound = new StreamingSound(
                        streamingSource,
                        volume,
                        1f,
                        0f,
                        false,
                        true,
                        1f
                    );
                    m_sound.Play();
                }
                catch {
                    Log.Warning($"Error playing music \"{name}\".");
                }
            }
        }

        public static void StopMusic() {
            if (m_sound != null) {
                if (m_fadeSound != null) {
                    m_fadeSound.Dispose();
                }
                m_sound.Stop();
                m_fadeSound = m_sound;
                m_sound = null;
            }
        }
    }
}