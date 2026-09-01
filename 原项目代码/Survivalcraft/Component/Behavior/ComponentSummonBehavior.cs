using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentSummonBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public StateMachine m_stateMachine = new();

        public float m_importanceLevel;

        public Random m_random = new();

        public bool m_isEnabled;

        public double m_summonedTime;

        public double m_stoppedTime;

        public ComponentBody SummonTarget { get; set; }

        public bool IsEnabled => m_isEnabled;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_isEnabled = valuesDictionary.GetValue<bool>("IsEnabled");
            m_stateMachine.AddState(
                "Inactive",
                delegate {
                    m_importanceLevel = 0f;
                    SummonTarget = null;
                    m_summonedTime = 0.0;
                },
                delegate {
                    if (m_isEnabled
                        && SummonTarget != null
                        && m_summonedTime == 0.0) {
                        m_subsystemTime.QueueGameTimeDelayedExecution(
                            m_subsystemTime.GameTime + 0.5,
                            delegate {
                                m_componentCreature.ComponentCreatureSounds.PlayIdleSound(true);
                                m_importanceLevel = 270f;
                                m_summonedTime = m_subsystemTime.GameTime;
                            }
                        );
                    }
                    if (IsActive) {
                        m_stateMachine.TransitionTo("FollowTarget");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "FollowTarget",
                delegate { FollowTarget(true); },
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    else if (SummonTarget == null
                        || m_componentPathfinding.IsStuck
                        || m_subsystemTime.GameTime - m_summonedTime > 30.0) {
                        m_importanceLevel = 0f;
                    }
                    else if (!m_componentPathfinding.Destination.HasValue) {
                        if (m_stoppedTime < 0.0) {
                            m_stoppedTime = m_subsystemTime.GameTime;
                        }
                        if (m_subsystemTime.GameTime - m_stoppedTime > 6.0) {
                            m_importanceLevel = 0f;
                        }
                    }
                    FollowTarget(false);
                    m_componentCreature.ComponentCreatureModel.LookRandomOrder = true;
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public virtual void FollowTarget(bool noDelay) {
            if (SummonTarget != null
                && (noDelay || m_random.Float(0f, 1f) < 5f * m_subsystemTime.GameTimeDelta)) {
                float num = Vector3.Distance(m_componentCreature.ComponentBody.Position, SummonTarget.Position);
                if (num > 4f) {
                    Vector3 v = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, SummonTarget.Position - m_componentCreature.ComponentBody.Position));
                    v *= 0.75f * (GetHashCode() % 2 != 0 ? 1 : -1) * (1 + GetHashCode() % 3);
                    float speed = MathUtils.Lerp(0.4f, 1f, MathUtils.Saturate(0.25f * (num - 5f)));
                    m_componentPathfinding.SetDestination(
                        SummonTarget.Position + v,
                        speed,
                        3.75f,
                        2000,
                        true,
                        false,
                        true,
                        null
                    );
                    m_stoppedTime = -1.0;
                }
            }
        }
    }
}