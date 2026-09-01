using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentAvoidPlayerBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemSky m_subsystemSky;

        public SubsystemBodies m_subsystemBodies;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public StateMachine m_stateMachine = new();

        public DynamicArray<ComponentBody> m_componentBodies = [];

        public Random m_random = new();

        public float m_importanceLevel;

        public float m_dayRange;

        public float m_nightRange;

        public float m_dt;

        public ComponentCreature m_target;

        public double m_nextUpdateTime;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            if (m_subsystemTime.GameTime >= m_nextUpdateTime) {
                m_dt = m_random.Float(0.4f, 0.6f) + MathUtils.Min((float)(m_subsystemTime.GameTime - m_nextUpdateTime), 0.1f);
                m_nextUpdateTime = m_subsystemTime.GameTime + m_dt;
                m_stateMachine.Update();
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_dayRange = valuesDictionary.GetValue<float>("DayRange");
            m_nightRange = valuesDictionary.GetValue<float>("NightRange");
            m_stateMachine.AddState(
                "Inactive",
                delegate {
                    m_importanceLevel = 0f;
                    m_target = null;
                },
                delegate {
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Move");
                    }
                    m_target = FindTarget(out float targetScore);
                    if (m_target != null) {
                        Vector3.Distance(m_target.ComponentBody.Position, m_componentCreature.ComponentBody.Position);
                        SetImportanceLevel(targetScore);
                    }
                    else {
                        m_importanceLevel = 0f;
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Move",
                null,
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    else if (m_target == null
                        || m_componentPathfinding.IsStuck
                        || !m_componentPathfinding.Destination.HasValue) {
                        m_importanceLevel = 0f;
                    }
                    else {
                        float num = ScoreTarget(m_target);
                        SetImportanceLevel(num);
                        Vector3 vector = Vector3.Normalize(m_componentCreature.ComponentBody.Position - m_target.ComponentBody.Position);
                        Vector3 value = m_componentCreature.ComponentBody.Position + 10f * Vector3.Normalize(new Vector3(vector.X, 0f, vector.Z));
                        m_componentPathfinding.SetDestination(
                            value,
                            MathUtils.Lerp(0.6f, 1f, num),
                            1f,
                            0,
                            false,
                            true,
                            false,
                            null
                        );
                        m_componentCreature.ComponentCreatureModel.LookRandomOrder = true;
                        if (m_random.Float(0f, 1f) < 0.1f * m_dt) {
                            m_componentCreature.ComponentCreatureSounds.PlayIdleSound(true);
                        }
                    }
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public virtual void SetImportanceLevel(float score) {
            m_importanceLevel = MathUtils.Lerp(4f, 8f, MathF.Sqrt(score));
        }

        public virtual ComponentCreature FindTarget(out float targetScore) {
            Vector3 position = m_componentCreature.ComponentBody.Position;
            ComponentCreature result = null;
            float num = 0f;
            m_componentBodies.Clear();
            m_subsystemBodies.FindBodiesAroundPoint(new Vector2(position.X, position.Z), MathUtils.Max(m_nightRange, m_dayRange), m_componentBodies);
            for (int i = 0; i < m_componentBodies.Count; i++) {
                ComponentCreature componentCreature = m_componentBodies.Array[i].Entity.FindComponent<ComponentCreature>();
                if (componentCreature != null) {
                    float num2 = ScoreTarget(componentCreature);
                    if (num2 > num) {
                        num = num2;
                        result = componentCreature;
                    }
                }
            }
            targetScore = num;
            return result;
        }

        public virtual float ScoreTarget(ComponentCreature target) {
            float num = m_subsystemSky.SkyLightIntensity > 0.2f ? m_dayRange : m_nightRange;
            if (num > 0f) {
                if (!target.IsAddedToProject
                    || target.ComponentHealth.Health <= 0f
                    || target.Entity.FindComponent<ComponentPlayer>() == null) {
                    return 0f;
                }
                float num2 = Vector3.Distance(target.ComponentBody.Position, m_componentCreature.ComponentBody.Position);
                return MathUtils.Saturate(1f - num2 / num);
            }
            return 0f;
        }
    }
}