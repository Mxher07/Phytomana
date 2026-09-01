using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentFlu : Component, IUpdateable {
        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemTime m_subsystemTime;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemParticles m_subsystemParticles;

        public ComponentPlayer m_componentPlayer;

        public Random m_random = new();

        public float m_fluOnset;
        public static string fName = "ComponentFlu";

        public float m_fluDuration;

        public float m_coughDuration;

        public float m_sneezeDuration;

        public float m_blackoutDuration;

        public float m_blackoutFactor;

        public double m_lastEffectTime = -1000.0;

        public double m_lastCoughTime = -1000.0;

        public double m_lastMessageTime = -1000.0;

        public bool HasFlu => m_fluDuration > 0f;

        public bool IsCoughing => m_coughDuration > 0f;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public virtual void StartFlu() {
            if (m_fluDuration == 0f) {
                m_componentPlayer.PlayerStats.TimesHadFlu++;
            }
            m_fluDuration = 900f;
            m_subsystemTime.QueueGameTimeDelayedExecution(
                m_subsystemTime.GameTime + 10.0,
                delegate { m_componentPlayer.ComponentVitalStats.MakeSleepy(0.2f); }
            );
        }

        public virtual void Sneeze() {
            m_sneezeDuration = 1f;
            m_componentPlayer.ComponentCreatureSounds.PlaySneezeSound();
            Project.FindSubsystem<SubsystemNoise>(true).MakeNoise(m_componentPlayer.ComponentBody.Position, 0.25f, 10f);
        }

        public virtual void Cough() {
            m_lastCoughTime = m_subsystemTime.GameTime;
            m_coughDuration = 4f;
            m_componentPlayer.ComponentCreatureSounds.PlayCoughSound();
            Project.FindSubsystem<SubsystemNoise>(true).MakeNoise(m_componentPlayer.ComponentBody.Position, 0.25f, 10f);
        }

        public virtual void Update(float dt) {
            if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative
                || !m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled) {
                m_fluDuration = 0f;
                m_fluOnset = 0f;
                return;
            }
            if (m_fluDuration > 0f) {
                m_fluOnset = 0f;
                float num = 1f;
                if (m_componentPlayer.ComponentVitalStats.Temperature > 16f) {
                    num = 2f;
                }
                else if (m_componentPlayer.ComponentVitalStats.Temperature > 12f) {
                    num = 1.5f;
                }
                else if (m_componentPlayer.ComponentVitalStats.Temperature < 8f) {
                    num = 0.5f;
                }
                m_fluDuration = MathUtils.Max(m_fluDuration - num * dt, 0f);
                if (m_componentPlayer.ComponentHealth.Health > 0f
                    && !m_componentPlayer.ComponentSleep.IsSleeping
                    && m_subsystemTime.PeriodicGameTimeEvent(5.0, -0.0099999997764825821)
                    && m_subsystemTime.GameTime - m_lastEffectTime > 13.0) {
                    FluEffect();
                }
            }
            else if (m_componentPlayer.ComponentVitalStats.Temperature < 6f) {
                float num2 = 13f;
                m_fluOnset += dt;
                if (m_fluOnset > 120f) {
                    num2 = 9f;
                    if (m_subsystemTime.PeriodicGameTimeEvent(1.0, 0.0)
                        && m_random.Bool(0.025f)) {
                        StartFlu();
                    }
                    if (m_subsystemTime.GameTime - m_lastMessageTime > 60.0) {
                        m_lastMessageTime = m_subsystemTime.GameTime;
                        m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, true, true);
                    }
                }
                if (m_fluOnset > 60f
                    && m_subsystemTime.PeriodicGameTimeEvent(num2, -0.0099999997764825821)
                    && m_random.Bool(0.75f)) {
                    Sneeze();
                }
            }
            else {
                m_fluOnset = 0f;
            }
            if ((m_coughDuration > 0f || m_sneezeDuration > 0f)
                && m_componentPlayer.ComponentHealth.Health > 0f
                && !m_componentPlayer.ComponentSleep.IsSleeping) {
                m_coughDuration = MathUtils.Max(m_coughDuration - dt, 0f);
                m_sneezeDuration = MathUtils.Max(m_sneezeDuration - dt, 0f);
                float num3 = MathUtils.DegToRad(
                    MathUtils.Lerp(-35f, -65f, SimplexNoise.Noise(4f * (float)MathUtils.Remainder(m_subsystemTime.GameTime, 10000.0)))
                );
                m_componentPlayer.ComponentLocomotion.LookOrder = new Vector2(
                    m_componentPlayer.ComponentLocomotion.LookOrder.X,
                    Math.Clamp(num3 - m_componentPlayer.ComponentLocomotion.LookAngles.Y, -3f, 3f)
                );
                if (m_random.Bool(2f * dt)) {
                    m_componentPlayer.ComponentBody.ApplyImpulse(-1.2f * m_componentPlayer.ComponentCreatureModel.EyeRotation.GetForwardVector());
                }
            }
            if (m_blackoutDuration > 0f) {
                m_blackoutDuration = MathUtils.Max(m_blackoutDuration - dt, 0f);
                m_blackoutFactor = MathUtils.Min(m_blackoutFactor + 0.5f * dt, 0.95f);
            }
            else if (m_blackoutFactor > 0f) {
                m_blackoutFactor = MathUtils.Max(m_blackoutFactor - 0.5f * dt, 0f);
            }
            m_componentPlayer.ComponentScreenOverlays.BlackoutFactor = MathUtils.Max(
                m_blackoutFactor,
                m_componentPlayer.ComponentScreenOverlays.BlackoutFactor
            );
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>(true);
            m_fluDuration = valuesDictionary.GetValue<float>("FluDuration");
            m_fluOnset = valuesDictionary.GetValue<float>("FluOnset");
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("FluDuration", m_fluDuration);
            valuesDictionary.SetValue("FluOnset", m_fluOnset);
        }

        public virtual void FluEffect() {
            m_lastEffectTime = m_subsystemTime.GameTime;
            m_blackoutDuration = MathUtils.Lerp(4f, 2f, m_componentPlayer.ComponentHealth.Health);
            float injury = MathUtils.Min(0.1f, m_componentPlayer.ComponentHealth.Health - 0.175f);
            if (injury > 0f) {
                m_subsystemTime.QueueGameTimeDelayedExecution(
                    m_subsystemTime.GameTime + 0.75,
                    delegate { m_componentPlayer.ComponentHealth.Injure(injury, null, false, LanguageControl.Get(fName, 4)); }
                );
            }
            if (Time.FrameStartTime - m_lastMessageTime > 60.0) {
                m_lastMessageTime = Time.FrameStartTime;
                m_subsystemTime.QueueGameTimeDelayedExecution(
                    m_subsystemTime.GameTime + 1.5,
                    delegate {
                        if (m_componentPlayer.ComponentVitalStats.Temperature < 8f) {
                            m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 2), Color.White, true, true);
                        }
                        else {
                            m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 3), Color.White, true, true);
                        }
                    }
                );
            }
            if (m_coughDuration == 0f
                && (m_subsystemTime.GameTime - m_lastCoughTime > 40.0 || m_random.Bool(0.5f))) {
                Cough();
            }
            else if (m_sneezeDuration == 0f) {
                Sneeze();
            }
        }
    }
}