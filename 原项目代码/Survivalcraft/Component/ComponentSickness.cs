using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentSickness : Component, IUpdateable {
        public SubsystemGameInfo m_subsystemGameInfo;
        public SubsystemTerrain m_subsystemTerrain;
        public SubsystemTime m_subsystemTime;
        public SubsystemParticles m_subsystemParticles;
        public ComponentPlayer m_componentPlayer;
        public PukeParticleSystem m_pukeParticleSystem;

        public float m_sicknessDuration;
        public float m_greenoutDuration;
        public float m_greenoutFactor;
        public double? m_lastNauseaTime;
        public double? m_lastMessageTime;
        public double? m_lastPukeTime;
        public const string fName = "ComponentSickness";

        public bool IsSick => m_sicknessDuration > 0f;

        public bool IsPuking => m_pukeParticleSystem != null;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public virtual void StartSickness() {
            if (m_sicknessDuration == 0f) {
                m_componentPlayer.PlayerStats.TimesWasSick++;
            }
            m_sicknessDuration = 900f;
        }

        public virtual void NauseaEffect() {
            m_lastNauseaTime = m_subsystemTime.GameTime;
            m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
            float injury = MathUtils.Min(0.1f, m_componentPlayer.ComponentHealth.Health - 0.075f);
            if (injury > 0f) {
                m_subsystemTime.QueueGameTimeDelayedExecution(
                    m_subsystemTime.GameTime + 0.75,
                    delegate { m_componentPlayer.ComponentHealth.Injure(injury, null, false, LanguageControl.Get(fName, 1)); }
                );
            }
            if (m_pukeParticleSystem == null
                && (!m_lastPukeTime.HasValue || m_subsystemTime.GameTime - m_lastPukeTime > 50.0)) {
                m_lastPukeTime = m_subsystemTime.GameTime;
                m_pukeParticleSystem = new PukeParticleSystem(m_subsystemTerrain);
                m_subsystemParticles.AddParticleSystem(m_pukeParticleSystem);
                m_componentPlayer.ComponentCreatureSounds.PlayPukeSound();
                Project.FindSubsystem<SubsystemNoise>(true).MakeNoise(m_componentPlayer.ComponentBody.Position, 0.25f, 10f);
                m_greenoutDuration = 0.8f;
                m_componentPlayer.PlayerStats.TimesPuked++;
            }
            else {
                m_greenoutDuration = MathUtils.Lerp(4f, 2f, m_componentPlayer.ComponentHealth.Health);
                if (!m_lastMessageTime.HasValue
                    || Time.FrameStartTime - m_lastMessageTime > 60.0) {
                    m_lastMessageTime = Time.FrameStartTime;
                    m_subsystemTime.QueueGameTimeDelayedExecution(
                        m_subsystemTime.GameTime + 1.5,
                        delegate {
                            m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 2), Color.White, true, true);
                        }
                    );
                }
            }
        }

        public virtual void Update(float dt) {
            if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative
                || !m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled) {
                m_sicknessDuration = 0f;
                return;
            }
            if (m_sicknessDuration > 0f) {
                m_sicknessDuration = MathUtils.Max(m_sicknessDuration - dt, 0f);
                if (m_componentPlayer.ComponentHealth.Health > 0f
                    && !m_componentPlayer.ComponentSleep.IsSleeping
                    && m_subsystemTime.PeriodicGameTimeEvent(3.0, -0.0099999997764825821)
                    && (!m_lastNauseaTime.HasValue || m_subsystemTime.GameTime - m_lastNauseaTime > 15.0)) {
                    NauseaEffect();
                }
            }
            if (m_pukeParticleSystem != null) {
                float num = MathUtils.DegToRad(
                    MathUtils.Lerp(-35f, -60f, SimplexNoise.Noise(2f * (float)MathUtils.Remainder(m_subsystemTime.GameTime, 10000.0)))
                );
                m_componentPlayer.ComponentLocomotion.LookOrder = new Vector2(
                    m_componentPlayer.ComponentLocomotion.LookOrder.X,
                    Math.Clamp(num - m_componentPlayer.ComponentLocomotion.LookAngles.Y, -2f, 2f)
                );
                Vector3 upVector = m_componentPlayer.ComponentCreatureModel.EyeRotation.GetUpVector();
                Vector3 forwardVector = m_componentPlayer.ComponentCreatureModel.EyeRotation.GetForwardVector();
                m_pukeParticleSystem.Position = m_componentPlayer.ComponentCreatureModel.EyePosition - 0.08f * upVector + 0.3f * forwardVector;
                m_pukeParticleSystem.Direction = Vector3.Normalize(forwardVector + 0.5f * upVector);
                if (m_pukeParticleSystem.IsStopped) {
                    m_pukeParticleSystem = null;
                }
            }
            if (m_greenoutDuration > 0f) {
                m_greenoutDuration = MathUtils.Max(m_greenoutDuration - dt, 0f);
                m_greenoutFactor = MathUtils.Min(m_greenoutFactor + 0.5f * dt, 0.95f);
            }
            else if (m_greenoutFactor > 0f) {
                m_greenoutFactor = MathUtils.Max(m_greenoutFactor - 0.5f * dt, 0f);
            }
            m_componentPlayer.ComponentScreenOverlays.GreenoutFactor = MathUtils.Max(
                m_greenoutFactor,
                m_componentPlayer.ComponentScreenOverlays.GreenoutFactor
            );
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>(true);
            m_sicknessDuration = valuesDictionary.GetValue<float>("SicknessDuration");
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("SicknessDuration", m_sicknessDuration);
        }
    }
}