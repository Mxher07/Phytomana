using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentStareBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemBodies m_subsystemBodies;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public DynamicArray<ComponentBody> m_componentBodies = [];

        public StateMachine m_stateMachine = new();

        public Random m_random = new();

        public float m_importanceLevel;

        public float m_stareRange;

        public double m_stareEndTime;

        public ComponentCreature m_target;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_stareRange = valuesDictionary.GetValue<float>("StareRange");
            m_stateMachine.AddState(
                "Inactive",
                delegate { m_importanceLevel = 0f; },
                delegate {
                    if (m_subsystemTime.GameTime > m_stareEndTime + 8.0
                        && m_random.Float(0f, 1f) < 1f * m_subsystemTime.GameTimeDelta) {
                        m_target = FindTarget();
                        if (m_target != null) {
                            float probability = m_target.Entity.FindComponent<ComponentPlayer>() != null ? 1f : 0.25f;
                            if (m_random.Bool(probability)) {
                                m_importanceLevel = m_random.Float(3f, 5f);
                            }
                        }
                    }
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Stare");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Stare",
                delegate {
                    m_stareEndTime = m_subsystemTime.GameTime + m_random.Float(6f, 12f);
                    if (m_target != null) {
                        Vector3 position = m_componentCreature.ComponentBody.Position;
                        Vector3 v = Vector3.Normalize(m_target.ComponentBody.Position - position);
                        m_componentPathfinding.SetDestination(
                            position + 1.1f * v,
                            m_random.Float(0.3f, 0.4f),
                            1f,
                            0,
                            false,
                            true,
                            false,
                            null
                        );
                        if (m_random.Float(0f, 1f) < 0.5f) {
                            m_componentCreature.ComponentCreatureSounds.PlayIdleSound(false);
                        }
                    }
                },
                delegate {
                    if (!IsActive
                        || m_target == null
                        || m_componentPathfinding.IsStuck
                        || m_subsystemTime.GameTime > m_stareEndTime) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    else if (m_random.Float(0f, 1f) < 1f * m_subsystemTime.GameTimeDelta
                        && ScoreTarget(m_target) <= 0f) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    else {
                        m_componentCreature.ComponentCreatureModel.LookAtOrder = m_target.ComponentCreatureModel.EyePosition;
                    }
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public virtual ComponentCreature FindTarget() {
            Vector3 position = m_componentCreature.ComponentBody.Position;
            m_componentBodies.Clear();
            m_subsystemBodies.FindBodiesAroundPoint(new Vector2(position.X, position.Z), m_stareRange, m_componentBodies);
            ComponentCreature result = null;
            float num = 0f;
            for (int i = 0; i < m_componentBodies.Count; i++) {
                ComponentCreature componentCreature = m_componentBodies.Array[i].Entity.FindComponent<ComponentCreature>();
                if (componentCreature != null) {
                    float num2 = ScoreTarget(componentCreature);
                    if (num2 > num) {
                        result = componentCreature;
                        num = num2;
                    }
                }
            }
            return result;
        }

        public virtual float ScoreTarget(ComponentCreature componentCreature) {
            if (componentCreature != m_componentCreature
                && componentCreature.Entity.IsAddedToProject) {
                float num = Vector3.Distance(m_componentCreature.ComponentBody.Position, componentCreature.ComponentBody.Position);
                float num2 = m_stareRange - num;
                if (m_random.Float(0f, 1f) < 0.66f
                    && componentCreature.Entity.FindComponent<ComponentPlayer>() != null) {
                    num2 *= 100f;
                }
                return num2;
            }
            return 0f;
        }
    }
}