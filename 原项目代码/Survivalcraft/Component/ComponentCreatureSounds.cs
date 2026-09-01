using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentCreatureSounds : Component {
        public SubsystemTime m_subsystemTime;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemSoundMaterials m_subsystemSoundMaterials;

        public ComponentCreature m_componentCreature;

        public Random m_random = new();

        public string m_idleSound;

        public string m_rareIdleSound;
        public string m_painSound;

        public string m_moanSound;

        public string m_sneezeSound;

        public string m_coughSound;

        public string m_pukeSound;

        public string m_attackSound;

        public float m_idleSoundMinDistance;

        public float m_rareIdleSoundMinDistance;

        public float m_rareIdleSoundProbability;

        public float m_painSoundMinDistance;

        public float m_moanSoundMinDistance;

        public float m_sneezeSoundMinDistance;

        public float m_coughSoundMinDistance;

        public float m_pukeSoundMinDistance;

        public float m_attackSoundMinDistance;

        public double m_lastSoundTime = -1000.0;

        public double m_lastCoughingSoundTime = -1000.0;

        public double m_lastPukeSoundTime = -1000.0;

        public virtual void PlayIdleSound(bool skipIfRecentlyPlayed) {
            bool flag = !string.IsNullOrEmpty(m_rareIdleSound) && m_random.Bool(m_rareIdleSoundProbability);
            string text = (flag ? m_rareIdleSound : m_idleSound);
            float num = (flag ? m_rareIdleSoundMinDistance : m_idleSoundMinDistance);
            if (!string.IsNullOrEmpty(text)
                && m_subsystemTime.GameTime > m_lastSoundTime + (double)(skipIfRecentlyPlayed ? 12f : 1f)) {
                m_lastSoundTime = m_subsystemTime.GameTime;
                m_subsystemAudio.PlayRandomSound(
                    text,
                    1f,
                    m_random.Float(-0.1f, 0.1f),
                    m_componentCreature.ComponentBody.Position,
                    num,
                    false
                );
            }
        }

        public virtual void PlayPainSound() {
            if (!string.IsNullOrEmpty(m_painSound)
                && m_subsystemTime.GameTime > m_lastSoundTime + 1.0) {
                m_lastSoundTime = m_subsystemTime.GameTime;
                m_subsystemAudio.PlayRandomSound(
                    m_painSound,
                    1f,
                    m_random.Float(-0.1f, 0.1f),
                    m_componentCreature.ComponentBody.Position,
                    m_painSoundMinDistance,
                    false
                );
            }
        }

        public virtual void PlayMoanSound() {
            if (!string.IsNullOrEmpty(m_moanSound)
                && m_subsystemTime.GameTime > m_lastSoundTime + 1.0) {
                m_lastSoundTime = m_subsystemTime.GameTime;
                m_subsystemAudio.PlayRandomSound(
                    m_moanSound,
                    1f,
                    m_random.Float(-0.1f, 0.1f),
                    m_componentCreature.ComponentBody.Position,
                    m_moanSoundMinDistance,
                    false
                );
            }
        }

        public virtual void PlaySneezeSound() {
            if (!string.IsNullOrEmpty(m_sneezeSound)
                && m_subsystemTime.GameTime > m_lastSoundTime + 1.0) {
                m_lastSoundTime = m_subsystemTime.GameTime;
                m_subsystemAudio.PlayRandomSound(
                    m_sneezeSound,
                    1f,
                    m_random.Float(-0.1f, 0.1f),
                    m_componentCreature.ComponentBody.Position,
                    m_sneezeSoundMinDistance,
                    false
                );
            }
        }

        public void PlayCoughSound() {
            if (!string.IsNullOrEmpty(m_coughSound)
                && m_subsystemTime.GameTime > m_lastCoughingSoundTime + 1.0) {
                m_lastCoughingSoundTime = m_subsystemTime.GameTime;
                m_subsystemAudio.PlayRandomSound(
                    m_coughSound,
                    1f,
                    m_random.Float(-0.1f, 0.1f),
                    m_componentCreature.ComponentBody.Position,
                    m_coughSoundMinDistance,
                    false
                );
            }
        }

        public virtual void PlayPukeSound() {
            if (!string.IsNullOrEmpty(m_pukeSound)
                && m_subsystemTime.GameTime > m_lastPukeSoundTime + 1.0) {
                m_lastPukeSoundTime = m_subsystemTime.GameTime;
                m_subsystemAudio.PlayRandomSound(
                    m_pukeSound,
                    1f,
                    m_random.Float(-0.1f, 0.1f),
                    m_componentCreature.ComponentBody.Position,
                    m_pukeSoundMinDistance,
                    false
                );
            }
        }

        public virtual void PlayAttackSound() {
            if (!string.IsNullOrEmpty(m_attackSound)
                && m_subsystemTime.GameTime > m_lastSoundTime + 1.0) {
                m_lastSoundTime = m_subsystemTime.GameTime;
                m_subsystemAudio.PlayRandomSound(
                    m_attackSound,
                    1f,
                    m_random.Float(-0.1f, 0.1f),
                    m_componentCreature.ComponentBody.Position,
                    m_attackSoundMinDistance,
                    false
                );
            }
        }

        public virtual bool PlayFootstepSound(float loudnessMultiplier) =>
            m_subsystemSoundMaterials.PlayFootstepSound(m_componentCreature, loudnessMultiplier);

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_idleSound = valuesDictionary.GetValue<string>("IdleSound");
            m_rareIdleSound = valuesDictionary.GetValue<string>("RareIdleSound");
            m_painSound = valuesDictionary.GetValue<string>("PainSound");
            m_moanSound = valuesDictionary.GetValue<string>("MoanSound");
            m_sneezeSound = valuesDictionary.GetValue<string>("SneezeSound");
            m_coughSound = valuesDictionary.GetValue<string>("CoughSound");
            m_pukeSound = valuesDictionary.GetValue<string>("PukeSound");
            m_attackSound = valuesDictionary.GetValue<string>("AttackSound");
            m_idleSoundMinDistance = valuesDictionary.GetValue<float>("IdleSoundMinDistance");
            m_rareIdleSoundMinDistance = valuesDictionary.GetValue<float>("RareIdleSoundMinDistance");
            m_rareIdleSoundProbability = valuesDictionary.GetValue<float>("RareIdleSoundProbability");
            m_painSoundMinDistance = valuesDictionary.GetValue<float>("PainSoundMinDistance");
            m_moanSoundMinDistance = valuesDictionary.GetValue<float>("MoanSoundMinDistance");
            m_sneezeSoundMinDistance = valuesDictionary.GetValue<float>("SneezeSoundMinDistance");
            m_coughSoundMinDistance = valuesDictionary.GetValue<float>("CoughSoundMinDistance");
            m_pukeSoundMinDistance = valuesDictionary.GetValue<float>("PukeSoundMinDistance");
            m_attackSoundMinDistance = valuesDictionary.GetValue<float>("AttackSoundMinDistance");
        }
    }
}