using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentStubbornSteedBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemGameInfo m_subsystemGameInfo;

        public ComponentCreature m_componentCreature;

        public ComponentMount m_componentMount;

        public ComponentSteedBehavior m_componentSteedBehavior;

        public ComponentEatPickableBehavior m_componentEatPickableBehavior;

        public StateMachine m_stateMachine = new();

        public float m_importanceLevel;

        public bool m_isSaddled;

        public Random m_random = new();

        public float m_periodicEventOffset;

        public float m_stubbornProbability;

        public double m_stubbornEndTime;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            m_stateMachine.Update();
            if (!IsActive) {
                m_stateMachine.TransitionTo("Inactive");
            }
            if (m_subsystemTime.PeriodicGameTimeEvent(1.0, m_periodicEventOffset)) {
                m_importanceLevel = m_subsystemGameInfo.TotalElapsedGameTime < m_stubbornEndTime
                    && m_componentEatPickableBehavior.Satiation <= 0f
                    && m_componentMount.Rider != null
                        ? 210f
                        : 0f;
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentMount = Entity.FindComponent<ComponentMount>(true);
            m_componentSteedBehavior = Entity.FindComponent<ComponentSteedBehavior>(true);
            m_componentEatPickableBehavior = Entity.FindComponent<ComponentEatPickableBehavior>(true);
            m_stubbornProbability = valuesDictionary.GetValue<float>("StubbornProbability");
            m_stubbornEndTime = valuesDictionary.GetValue<double>("StubbornEndTime");
            m_periodicEventOffset = m_random.Float(0f, 100f);
            m_isSaddled = Entity.ValuesDictionary.DatabaseObject.Name.EndsWith("_Saddled");
            m_stateMachine.AddState(
                "Inactive",
                null,
                delegate {
                    if (m_subsystemTime.PeriodicGameTimeEvent(1.0, m_periodicEventOffset)
                        && m_componentMount.Rider != null
                        && m_random.Float(0f, 1f) < m_stubbornProbability
                        && (!m_isSaddled || m_componentEatPickableBehavior.Satiation <= 0f)) {
                        m_stubbornEndTime = m_subsystemGameInfo.TotalElapsedGameTime + m_random.Float(60f, 120f);
                    }
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Stubborn");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Stubborn",
                null,
                delegate {
                    if (m_componentSteedBehavior.WasOrderIssued) {
                        m_componentCreature.ComponentCreatureModel.HeadShakeOrder = m_random.Float(0.6f, 1f);
                        m_componentCreature.ComponentCreatureSounds.PlayPainSound();
                    }
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("StubbornEndTime", m_stubbornEndTime);
        }
    }
}