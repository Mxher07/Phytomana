using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentHowlBehavior : ComponentBehavior, IUpdateable {
        public SubsystemSky m_subsystemSky;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemTime m_subsystemTime;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public StateMachine m_stateMachine = new();

        public float m_importanceLevel;

        public string m_howlSoundName;

        public float m_howlTime;

        public float m_howlDuration;

        public Random m_random = new();

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_howlSoundName = valuesDictionary.GetValue<string>("HowlSoundName");
            m_stateMachine.AddState(
                "Inactive",
                delegate { m_importanceLevel = 0f; },
                delegate {
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Howl");
                    }
                    if (m_subsystemSky.SkyLightIntensity < 0.1f) {
                        if (m_random.Float(0f, 1f) < 0.015f * m_subsystemTime.GameTimeDelta) {
                            m_importanceLevel = m_random.Float(1f, 3f);
                        }
                    }
                    else {
                        m_importanceLevel = 0f;
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Howl",
                delegate {
                    m_howlTime = 0f;
                    m_howlDuration = m_random.Float(5f, 6f);
                    m_componentPathfinding.Stop();
                    m_importanceLevel = 10f;
                },
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    m_componentCreature.ComponentLocomotion.LookOrder = new Vector2(m_componentCreature.ComponentLocomotion.LookOrder.X, 2f);
                    float num = m_howlTime + m_subsystemTime.GameTimeDelta;
                    if (m_howlTime <= 0.5f
                        && num > 0.5f) {
                        m_subsystemAudio.PlayRandomSound(
                            m_howlSoundName,
                            1f,
                            m_random.Float(-0.1f, 0.1f),
                            m_componentCreature.ComponentBody.Position,
                            10f,
                            true
                        );
                    }
                    m_howlTime = num;
                    if (m_howlTime >= m_howlDuration) {
                        m_importanceLevel = 0f;
                    }
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }
    }
}