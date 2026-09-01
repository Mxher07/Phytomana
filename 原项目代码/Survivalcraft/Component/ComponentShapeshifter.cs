using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentShapeshifter : Component, IUpdateable {
        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemSky m_subsystemSky;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemAudio m_subsystemAudio;

        public ComponentSpawn m_componentSpawn;

        public ComponentBody m_componentBody;

        public ComponentHealth m_componentHealth;

        public ShapeshiftParticleSystem m_particleSystem;

        public string m_nightEntityTemplateName;

        public string m_dayEntityTemplateName;

        public float m_timeToSwitch;

        public string m_spawnEntityTemplateName;

        public static Random s_random = new();

        public bool IsEnabled { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public virtual void Update(float dt) {
            bool areSupernaturalCreaturesEnabled = m_subsystemGameInfo.WorldSettings.AreSupernaturalCreaturesEnabled;
            if (IsEnabled
                && !m_componentSpawn.IsDespawning
                && m_componentHealth.Health > 0f) {
                if (!areSupernaturalCreaturesEnabled
                    && !string.IsNullOrEmpty(m_dayEntityTemplateName)) {
                    ShapeshiftTo(m_dayEntityTemplateName);
                }
                else if (m_subsystemSky.SkyLightIntensity > 0.25f
                    && !string.IsNullOrEmpty(m_dayEntityTemplateName)) {
                    m_timeToSwitch -= 2f * dt;
                    if (m_timeToSwitch <= 0f) {
                        ShapeshiftTo(m_dayEntityTemplateName);
                    }
                }
                else if (areSupernaturalCreaturesEnabled
                    && m_subsystemSky.SkyLightIntensity < 0.1f
                    && (m_subsystemSky.MoonPhase == 0 || m_subsystemSky.MoonPhase == 4)
                    && !string.IsNullOrEmpty(m_nightEntityTemplateName)) {
                    m_timeToSwitch -= dt;
                    if (m_timeToSwitch <= 0f) {
                        ShapeshiftTo(m_nightEntityTemplateName);
                    }
                }
            }
            if (!string.IsNullOrEmpty(m_spawnEntityTemplateName)) {
                if (m_particleSystem == null) {
                    m_particleSystem = new ShapeshiftParticleSystem();
                    m_subsystemParticles.AddParticleSystem(m_particleSystem);
                }
                m_particleSystem.BoundingBox = m_componentBody.BoundingBox;
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_componentSpawn = Entity.FindComponent<ComponentSpawn>(true);
            m_componentBody = Entity.FindComponent<ComponentBody>(true);
            m_componentHealth = Entity.FindComponent<ComponentHealth>(true);
            m_dayEntityTemplateName = valuesDictionary.GetValue<string>("DayEntityTemplateName");
            m_nightEntityTemplateName = valuesDictionary.GetValue<string>("NightEntityTemplateName");
            float value = valuesDictionary.GetValue<float>("Probability");
            if (!string.IsNullOrEmpty(m_dayEntityTemplateName)) {
                DatabaseManager.FindEntityValuesDictionary(m_dayEntityTemplateName, true);
            }
            if (!string.IsNullOrEmpty(m_nightEntityTemplateName)) {
                DatabaseManager.FindEntityValuesDictionary(m_nightEntityTemplateName, true);
            }
            m_timeToSwitch = s_random.Float(3f, 15f);
            IsEnabled = s_random.Float(0f, 1f) < value;
            m_componentSpawn.Despawned += ComponentSpawn_Despawned;
        }

        public virtual void ShapeshiftTo(string entityTemplateName) {
            if (string.IsNullOrEmpty(m_spawnEntityTemplateName)) {
                m_spawnEntityTemplateName = entityTemplateName;
                m_componentSpawn.DespawnDuration = 3f;
                m_componentSpawn.Despawn();
                m_subsystemAudio.PlaySound("Audio/Shapeshift", 1f, 0f, m_componentBody.Position, 3f, true);
            }
        }

        public virtual void ComponentSpawn_Despawned(ComponentSpawn componentSpawn) {
            if (m_componentHealth.Health > 0f
                && !string.IsNullOrEmpty(m_spawnEntityTemplateName)) {
                Entity entity = DatabaseManager.CreateEntity(Project, m_spawnEntityTemplateName, true);
                ComponentBody componentBody = entity.FindComponent<ComponentBody>(true);
                componentBody.Position = m_componentBody.Position;
                componentBody.Rotation = m_componentBody.Rotation;
                componentBody.Velocity = m_componentBody.Velocity;
                entity.FindComponent<ComponentSpawn>(true).SpawnDuration = 0.5f;
                ModsManager.HookAction(
                    "OnDespawned",
                    modLoader => {
                        modLoader.OnDespawned(entity, componentSpawn);
                        return false;
                    }
                );
                Project.AddEntity(entity);
            }
            if (m_particleSystem != null) {
                m_particleSystem.Stopped = true;
            }
        }
    }
}