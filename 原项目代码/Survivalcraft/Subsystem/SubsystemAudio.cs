using Engine;
using Engine.Audio;
using GameEntitySystem;
using TemplatesDatabase;
using Vector3 = Engine.Vector3;

namespace Game {
    public class SubsystemAudio : Subsystem, IUpdateable {
        public class Congestion {
            public double LastUpdateTime;

            public double LastPlayedTime;

            public float LastPlayedVolume;

            public float Value;
        }

        public struct SoundInfo {
            public double Time;

            public string Name;

            public float Volume;

            public float Pitch;

            public float Pan;

            public Vector3 direction = Vector3.Zero;

            public SoundInfo() { }
        }

        public SubsystemTime m_subsystemTime;

        public SubsystemGameWidgets m_subsystemViews;

        public Random m_random = new();

        public List<Vector3> m_listenerPositions = [];

        public Dictionary<string, Congestion> m_congestions = [];

        public double m_nextSoundTime;

        public List<SoundInfo> m_queuedSounds = [];

        public List<Sound> m_sounds = [];

        public Dictionary<Sound, bool> m_mutedSounds = [];

        public ReadOnlyList<Vector3> ListenerPositions => new(m_listenerPositions);

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public float CalculateListenerDistanceSquared(Vector3 p) {
            float num = float.MaxValue;
            for (int i = 0; i < m_listenerPositions.Count; i++) {
                float num2 = Vector3.DistanceSquared(m_listenerPositions[i], p);
                if (num2 < num) {
                    num = num2;
                }
            }
            return num;
        }

        public float CalculateListenerDistance(Vector3 p) => MathF.Sqrt(CalculateListenerDistanceSquared(p));

        public void Mute() {
            foreach (Sound sound in m_sounds) {
                if (sound.State == SoundState.Playing) {
                    m_mutedSounds[sound] = true;
                    sound.Pause();
                }
            }
        }

        public void Unmute() {
            foreach (Sound key in m_mutedSounds.Keys) {
                key.Play();
            }
            m_mutedSounds.Clear();
        }

        public void PlaySound(string name, float volume, float pitch, float pan, float delay) {
            double num = m_subsystemTime.GameTime + delay;
            m_nextSoundTime = Math.Min(m_nextSoundTime, num);
            m_queuedSounds.Add(new SoundInfo { Time = num, Name = name, Volume = volume, Pitch = pitch, Pan = pan });
        }

        public void PlaySound(string name, float volume, float pitch, float pan, float delay, Vector3 direction) {
            double num = m_subsystemTime.GameTime + delay;
            m_nextSoundTime = Math.Min(m_nextSoundTime, num);
            m_queuedSounds.Add(new SoundInfo { Time = num, Name = name, Volume = volume, Pitch = pitch, Pan = pan, direction = direction });
        }

        public virtual void PlaySound(string name, float volume, float pitch, Vector3 position, float minDistance, float delay) {
            float num = CalculateVolume(CalculateListenerDistance(position), minDistance);
            PlaySound(name, volume * num, pitch, 0f, delay);
        }

        public virtual void PlaySound(string name, float volume, float pitch, Vector3 position, float minDistance, bool autoDelay) {
            float num = CalculateVolume(CalculateListenerDistance(position), minDistance);
            PlaySound(name, volume * num, pitch, 0f, autoDelay ? CalculateDelay(position) : 0f);
        }

        public void PlayRandomSound(string directory, float volume, float pitch, float pan, float delay) {
            ReadOnlyList<ContentInfo> readOnlyList = ContentManager.List(directory);
            if (readOnlyList.Count > 0) {
                int index = m_random.Int(0, readOnlyList.Count - 1);
                PlaySound(readOnlyList[index].ContentPath, volume, pitch, pan, delay);
            }
            else {
                Log.Warning("Sounds directory \"{0}\" not found or empty.", directory);
            }
        }

        public virtual void PlayRandomSound(string directory, float volume, float pitch, Vector3 position, float minDistance, float delay) {
            float num = CalculateVolume(CalculateListenerDistance(position), minDistance);
            PlayRandomSound(directory, volume * num, pitch, 0f, delay);
        }

        public virtual void PlayRandomSound(string directory, float volume, float pitch, Vector3 position, float minDistance, bool autoDelay) {
            float num = CalculateVolume(CalculateListenerDistance(position), minDistance);
            PlayRandomSound(directory, volume * num, pitch, 0f, autoDelay ? CalculateDelay(position) : 0f);
        }

        public Sound CreateSound(string name) {
            Sound sound = new(ContentManager.Get<SoundBuffer>(name));
            m_sounds.Add(sound);
            return sound;
        }

        public float CalculateVolume(float distance, float minDistance, float rolloffFactor = 2f) => distance > minDistance
            ? minDistance / (minDistance + Math.Max(rolloffFactor * (distance - minDistance), 0f))
            : 1f;

        public float CalculateDelay(Vector3 position) => CalculateDelay(CalculateListenerDistance(position));

        public float CalculateDelay(float distance) => Math.Min(distance / 120f, 3f);

        public virtual void Update(float dt) {
            m_listenerPositions.Clear();
            foreach (GameWidget gameWidget in m_subsystemViews.GameWidgets) {
                m_listenerPositions.Add(gameWidget.ActiveCamera.ViewPosition);
            }
            if (!(m_subsystemTime.GameTime >= m_nextSoundTime)) {
                return;
            }
            m_nextSoundTime = double.MaxValue;
            int num = 0;
            while (num < m_queuedSounds.Count) {
                SoundInfo soundInfo = m_queuedSounds[num];
                if (m_subsystemTime.GameTime >= soundInfo.Time) {
                    if (m_subsystemTime.GameTimeFactor == 1f
                        && !m_subsystemTime.FixedTimeStep.HasValue
                        && soundInfo.Volume * SettingsManager.SoundsVolume > AudioManager.MinAudibleVolume
                        && UpdateCongestion(soundInfo.Name, soundInfo.Volume)) {
                        AudioManager.PlaySound(soundInfo.Name, soundInfo.Volume, soundInfo.Pitch, soundInfo.Pan, soundInfo.direction);
                    }
                    m_queuedSounds.RemoveAt(num);
                }
                else {
                    m_nextSoundTime = Math.Min(m_nextSoundTime, soundInfo.Time);
                    num++;
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemViews = Project.FindSubsystem<SubsystemGameWidgets>(true);
        }

        public override void Dispose() {
            foreach (Sound sound in m_sounds) {
                sound.Dispose();
            }
        }

        public bool UpdateCongestion(string name, float volume) {
            if (!m_congestions.TryGetValue(name, out Congestion value)) {
                value = new Congestion();
                m_congestions.Add(name, value);
            }
            double realTime = Time.RealTime;
            double lastUpdateTime = value.LastUpdateTime;
            double lastPlayedTime = value.LastPlayedTime;
            float num = lastUpdateTime > 0.0 ? (float)(realTime - lastUpdateTime) : 0f;
            value.Value = Math.Max(value.Value - 10f * num, 0f);
            value.LastUpdateTime = realTime;
            if (value.Value <= 6f
                && (lastPlayedTime == 0.0 || volume > value.LastPlayedVolume || realTime - lastPlayedTime >= 0.0)) {
                value.LastPlayedTime = realTime;
                value.LastPlayedVolume = volume;
                value.Value += 1f;
                return true;
            }
            return false;
        }
    }
}