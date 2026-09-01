using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentAvoidFireBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemSky m_subsystemSky;

        public SubsystemCampfireBlockBehavior m_subsystemCampfireBlockBehavior;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public StateMachine m_stateMachine = new();

        public Random m_random = new();

        public float m_importanceLevel;

        public float m_dayRange;

        public float m_nightRange;

        public Vector3? m_target;

        public float m_circlingDirection = 1f;

        public float m_periodicEventOffset;

        public double m_ignoreFireUntil;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public override string DebugInfo {
            get {
                if (!(m_ignoreFireUntil < m_subsystemTime.GameTime)) {
                    return string.Empty;
                }
                return $"ifu={m_ignoreFireUntil - m_subsystemTime.GameTime:0}";
            }
        }

        public virtual void Update(float dt) {
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_subsystemCampfireBlockBehavior = Project.FindSubsystem<SubsystemCampfireBlockBehavior>(true);
            m_dayRange = valuesDictionary.GetValue<float>("DayRange");
            m_nightRange = valuesDictionary.GetValue<float>("NightRange");
            m_periodicEventOffset = m_random.Float(0f, 10f);
            m_stateMachine.AddState(
                "Inactive",
                delegate {
                    m_importanceLevel = 0f;
                    m_target = null;
                },
                delegate {
                    if (IsActive) {
                        m_stateMachine.TransitionTo(m_importanceLevel < 10f ? "Circle" : "Move");
                    }
                    else if (m_subsystemTime.PeriodicGameTimeEvent(1.0, m_periodicEventOffset)) {
                        m_target = FindTarget(out float targetScore);
                        if (m_target.HasValue) {
                            if (m_random.Float(0f, 1f) < 0.015f) {
                                m_ignoreFireUntil = m_subsystemTime.GameTime + 20.0;
                            }
                            Vector3.Distance(m_target.Value, m_componentCreature.ComponentBody.Position);
                            m_importanceLevel = m_subsystemTime.GameTime < m_ignoreFireUntil ? 0f :
                                targetScore > 0.5f ? 250f : m_random.Float(1f, 5f);
                        }
                        else {
                            m_importanceLevel = 0f;
                        }
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Move",
                delegate {
                    if (m_target.HasValue) {
                        Vector3 vector2 = Vector3.Normalize(
                            Vector3.Normalize(m_componentCreature.ComponentBody.Position - m_target.Value) + m_random.Vector3(0.5f)
                        );
                        Vector3 value2 = m_componentCreature.ComponentBody.Position
                            + m_random.Float(6f, 8f) * Vector3.Normalize(new Vector3(vector2.X, 0f, vector2.Z));
                        m_componentPathfinding.SetDestination(
                            value2,
                            m_random.Float(0.6f, 0.8f),
                            1f,
                            0,
                            false,
                            true,
                            false,
                            null
                        );
                    }
                },
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    else if (!m_target.HasValue
                        || m_componentPathfinding.IsStuck
                        || !m_componentPathfinding.Destination.HasValue
                        || ScoreTarget(m_target.Value) <= 0f) {
                        m_importanceLevel = 0f;
                    }
                    if (m_random.Float(0f, 1f) < 0.1f * m_subsystemTime.GameTimeDelta) {
                        m_componentCreature.ComponentCreatureSounds.PlayIdleSound(true);
                    }
                    m_componentCreature.ComponentCreatureModel.LookRandomOrder = true;
                },
                null
            );
            m_stateMachine.AddState(
                "Circle",
                delegate {
                    if (m_target.HasValue) {
                        Vector3 vector = Vector3.Cross(Vector3.Normalize(m_componentCreature.ComponentBody.Position - m_target.Value), Vector3.UnitY)
                            * m_circlingDirection;
                        Vector3 value = m_componentCreature.ComponentBody.Position
                            + m_random.Float(6f, 8f) * Vector3.Normalize(new Vector3(vector.X, 0f, vector.Z));
                        m_componentPathfinding.SetDestination(
                            value,
                            m_random.Float(0.4f, 0.9f),
                            1f,
                            0,
                            false,
                            true,
                            false,
                            null
                        );
                    }
                },
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    else if (m_componentPathfinding.IsStuck) {
                        m_circlingDirection = 0f - m_circlingDirection;
                        m_importanceLevel = 0f;
                    }
                    else if (!m_target.HasValue
                        || !m_componentPathfinding.Destination.HasValue
                        || ScoreTarget(m_target.Value) <= 0f) {
                        m_importanceLevel = 0f;
                    }
                    if (m_random.Float(0f, 1f) < 0.1f * m_subsystemTime.GameTimeDelta) {
                        m_componentCreature.ComponentCreatureSounds.PlayIdleSound(true);
                    }
                    m_componentCreature.ComponentCreatureModel.LookAtOrder = m_target;
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public virtual Vector3? FindTarget(out float targetScore) {
            _ = m_componentCreature.ComponentBody.Position;
            Vector3? result = null;
            float num = 0f;
            foreach (Point3 campfire in m_subsystemCampfireBlockBehavior.Campfires) {
                Vector3 vector = new(campfire.X, campfire.Y, campfire.Z);
                float num2 = ScoreTarget(vector);
                if (num2 > num) {
                    num = num2;
                    result = vector;
                }
            }
            targetScore = num;
            return result;
        }

        public virtual float ScoreTarget(Vector3 target) {
            float num = m_subsystemSky.SkyLightIntensity > 0.2f ? m_dayRange : m_nightRange;
            if (num > 0f) {
                float num2 = Vector3.Distance(target, m_componentCreature.ComponentBody.Position);
                return MathUtils.Saturate(1f - num2 / num);
            }
            return 0f;
        }
    }
}