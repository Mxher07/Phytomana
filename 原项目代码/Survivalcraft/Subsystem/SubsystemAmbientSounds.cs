using Engine;
using Engine.Audio;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemAmbientSounds : Subsystem, IUpdateable {
        public Sound m_fireSound;

        public Sound m_waterSound;

        public Sound m_magmaSound;

        public Random m_random = new();

        public SubsystemAudio SubsystemAudio { get; set; }

        public float FireSoundVolume { get; set; }

        public float WaterSoundVolume { get; set; }

        public float MagmaSoundVolume { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public virtual void Update(float dt) {
            m_fireSound.Volume = MathUtils.Lerp(
                m_fireSound.Volume,
                SettingsManager.SoundsVolume * FireSoundVolume,
                MathUtils.Saturate(3f * Time.FrameDuration)
            );
            if (m_fireSound.Volume > 0.5f * AudioManager.MinAudibleVolume) {
                m_fireSound.Play();
            }
            else {
                m_fireSound.Pause();
            }
            m_waterSound.Volume = MathUtils.Lerp(
                m_waterSound.Volume,
                SettingsManager.SoundsVolume * WaterSoundVolume,
                MathUtils.Saturate(3f * Time.FrameDuration)
            );
            if (m_waterSound.Volume > 0.5f * AudioManager.MinAudibleVolume) {
                m_waterSound.Play();
            }
            else {
                m_waterSound.Pause();
            }
            m_magmaSound.Volume = MathUtils.Lerp(
                m_magmaSound.Volume,
                SettingsManager.SoundsVolume * MagmaSoundVolume,
                MathUtils.Saturate(3f * Time.FrameDuration)
            );
            if (m_magmaSound.Volume > 0.5f * AudioManager.MinAudibleVolume) {
                m_magmaSound.Play();
            }
            else {
                m_magmaSound.Pause();
            }
            if (m_magmaSound.State == SoundState.Playing
                && m_random.Bool(0.2f * dt)) {
                SubsystemAudio.PlayRandomSound("Audio/Sizzles", m_magmaSound.Volume, m_random.Float(-0.2f, 0.2f), 0f, 0f);
            }
            FireSoundVolume = 0f;
            WaterSoundVolume = 0f;
            MagmaSoundVolume = 0f;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            SubsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_fireSound = SubsystemAudio.CreateSound("Audio/Fire");
            m_fireSound.IsLooped = true;
            m_fireSound.Volume = 0f;
            m_waterSound = SubsystemAudio.CreateSound("Audio/Water");
            m_waterSound.IsLooped = true;
            m_waterSound.Volume = 0f;
            m_magmaSound = SubsystemAudio.CreateSound("Audio/Magma");
            m_magmaSound.IsLooped = true;
            m_magmaSound.Volume = 0f;
        }
    }
}