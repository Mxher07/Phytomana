#if BROWSER
using DistanceModelEnum = Engine.Browser.AL.DistanceModelEnum;
using SourceFloat = Engine.Browser.AL.SourceFloat;
using SourceVector3 = Engine.Browser.AL.SourceVector3;
#else
using Silk.NET.OpenAL;
using DistanceModelEnum = Silk.NET.OpenAL.DistanceModel;
#endif

namespace Engine.Audio {
    public abstract class BaseSound : IDisposable {
        float m_volume = 1f;

        float m_pitch = 1f;

        float m_pan;

        internal object m_lock = new();

        internal bool m_isLooped;

        internal bool m_disposeOnStop;

        public int m_source;

        public SoundState State { get; set; }

        public int ChannelsCount { get; set; }

        public int SamplingFrequency { get; set; }

        public float Volume {
            get => m_volume;
            set {
                value = MathUtils.Saturate(value);
                if (value != m_volume) {
                    m_volume = value;
                    InternalSetVolume(value);
                }
            }
        }

        public float Pitch {
            get => m_pitch;
            set {
                value = Math.Clamp(value, 0.5f, 2f);
                if (value != m_pitch) {
                    m_pitch = value;
                    InternalSetPitch(value);
                }
            }
        }

        public float Pan {
            get => m_pan;
            set {
                if (ChannelsCount == 1) {
                    value = Math.Clamp(value, -1f, 1f);
                    if (value != m_pan) {
                        m_pan = value;
                        InternalSetPan(value);
                    }
                }
            }
        }

        public bool IsLooped {
            get => m_isLooped;
            set {
                lock (m_lock) {
                    if (State == SoundState.Stopped) {
                        m_isLooped = value;
                    }
                }
            }
        }

        public bool DisposeOnStop {
            get => m_disposeOnStop;
            set {
                lock (m_lock) {
                    if (State == SoundState.Stopped) {
                        m_disposeOnStop = value;
                    }
                }
            }
        }

        public void Play() {
            lock (m_lock) {
                if (State == SoundState.Stopped
                    || State == SoundState.Paused) {
                    State = SoundState.Playing;
                    InternalPlay(Vector3.Zero);
                }
            }
        }

        public void Play(Vector3 direction) {
            lock (m_lock) {
                if (State == SoundState.Stopped
                    || State == SoundState.Paused) {
                    State = SoundState.Playing;
                    InternalPlay(direction);
                }
            }
        }

        public void Pause() {
            lock (m_lock) {
                if (State == SoundState.Playing) {
                    State = SoundState.Paused;
                    InternalPause();
                }
            }
        }

        public void Stop() {
            if (m_disposeOnStop) {
                Dispose();
            }
            lock (m_lock) {
                if (State == SoundState.Playing
                    || State == SoundState.Paused) {
                    State = SoundState.Stopped;
                    InternalStop();
                }
            }
        }

        public virtual void Dispose() {
            if (State != SoundState.Disposed) {
                State = SoundState.Disposed;
                InternalDispose();
            }
        }

        internal BaseSound() {
            uint source = Mixer.AL.GenSource();
            m_source = (int)source;
            Mixer.CheckALError();
            Mixer.AL.DistanceModel(DistanceModelEnum.None);
            Mixer.CheckALError();
        }

        void InternalSetVolume(float volume) {
            if (m_source != 0) {
                Mixer.AL.SetSourceProperty((uint)m_source, SourceFloat.Gain, volume);
                Mixer.CheckALError();
            }
        }

        void InternalSetPitch(float pitch) {
            if (m_source != 0) {
                Mixer.AL.SetSourceProperty((uint)m_source, SourceFloat.Pitch, pitch);
                Mixer.CheckALError();
            }
        }

        void InternalSetPan(float pan) {
            if (m_source != 0) {
                float value = 0f;
                float value2 = -0.1f;
                Mixer.AL.SetSourceProperty((uint)m_source, SourceVector3.Position, pan, value, value2);
                Mixer.CheckALError();
            }
        }

        internal abstract void InternalPlay(Vector3 direction);
        internal abstract void InternalPause();

        internal abstract void InternalStop();

        internal virtual void InternalDispose() {
            if (m_source != 0) {
                uint source = (uint)m_source;
                Mixer.AL.SourceStop(source);
                Mixer.CheckALError();
                Mixer.AL.DeleteSource(source);
                Mixer.CheckALError();
                m_source = 0;
            }
        }
    }
}