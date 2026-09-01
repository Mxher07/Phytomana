using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentLookAroundBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public ComponentCreature m_componentCreature;

        public StateMachine m_stateMachine = new();

        public Random m_random = new();

        public float m_lookAroundTime;

        public float m_importanceLevel;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_stateMachine.AddState(
                "Inactive",
                delegate { m_importanceLevel = m_random.Float(0f, 1f); },
                delegate {
                    if (m_componentCreature.ComponentBody.StandingOnValue.HasValue
                        && m_random.Float(0f, 1f) < 0.05f * m_subsystemTime.GameTimeDelta) {
                        m_importanceLevel = m_random.Float(1f, 5f);
                    }
                    if (IsActive) {
                        m_stateMachine.TransitionTo("LookAround");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "LookAround",
                delegate { m_lookAroundTime = m_random.Float(8f, 15f); },
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    else if (m_lookAroundTime <= 0f) {
                        m_importanceLevel = 0f;
                    }
                    else if (m_random.Float(0f, 1f) < 0.1f * m_subsystemTime.GameTimeDelta) {
                        m_componentCreature.ComponentCreatureSounds.PlayIdleSound(false);
                    }
                    m_componentCreature.ComponentCreatureModel.LookRandomOrder = true;
                    m_lookAroundTime -= m_subsystemTime.GameTimeDelta;
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }
    }
}