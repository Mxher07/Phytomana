using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentRandomFeedBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemTime m_subsystemTime;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public StateMachine m_stateMachine = new();

        public Random m_random = new();

        public float m_importanceLevel;

        public float m_feedTime;

        public float m_waitTime;

        public Vector3? m_feedPosition;

        public bool m_autoFeed;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public void Feed(Vector3 feedPosition) {
            m_importanceLevel = 5f;
            m_feedPosition = feedPosition;
        }

        public virtual void Update(float dt) {
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_autoFeed = valuesDictionary.GetValue<bool>("AutoFeed");
            m_stateMachine.AddState(
                "Inactive",
                delegate { m_importanceLevel = m_random.Float(0f, 1f); },
                delegate {
                    if (m_random.Float(0f, 1f) < 0.05f * m_subsystemTime.GameTimeDelta) {
                        m_importanceLevel = m_random.Float(1f, 3f);
                    }
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Move");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Move",
                delegate {
                    Vector3 value;
                    if (m_feedPosition.HasValue) {
                        value = m_feedPosition.Value;
                    }
                    else {
                        Vector3 position = m_componentCreature.ComponentBody.Position;
                        Vector3 forward = m_componentCreature.ComponentBody.Matrix.Forward;
                        float num4 = m_random.Float(0f, 1f) < 0.2f ? 5f : 1.5f;
                        value = position + num4 * forward + 0.5f * num4 * new Vector3(m_random.Float(-1f, 1f), 0f, m_random.Float(-1f, 1f));
                    }
                    value.Y = m_subsystemTerrain.Terrain.GetTopHeight(Terrain.ToCell(value.X), Terrain.ToCell(value.Z)) + 1;
                    m_componentPathfinding.SetDestination(
                        value,
                        m_random.Float(0.25f, 0.35f),
                        1f,
                        0,
                        false,
                        true,
                        false,
                        null
                    );
                },
                delegate {
                    if (!m_componentPathfinding.Destination.HasValue) {
                        float num3 = m_random.Float(0f, 1f);
                        if (num3 < 0.33f) {
                            m_stateMachine.TransitionTo("Inactive");
                        }
                        else if (num3 < 0.66f) {
                            m_stateMachine.TransitionTo("LookAround");
                        }
                        else {
                            m_stateMachine.TransitionTo("Feed");
                        }
                    }
                    else if (!IsActive
                        || m_componentPathfinding.IsStuck) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                },
                delegate { m_feedPosition = null; }
            );
            m_stateMachine.AddState(
                "LookAround",
                delegate { m_waitTime = m_random.Float(1f, 2f); },
                delegate {
                    m_componentCreature.ComponentCreatureModel.LookRandomOrder = true;
                    m_waitTime -= m_subsystemTime.GameTimeDelta;
                    if (m_waitTime <= 0f) {
                        float num2 = m_random.Float(0f, 1f);
                        if (num2 < 0.25f) {
                            m_stateMachine.TransitionTo("Inactive");
                        }
                        if (num2 < 0.5f) {
                            m_stateMachine.TransitionTo(null);
                            m_stateMachine.TransitionTo("LookAround");
                        }
                        else if (num2 < 0.75f) {
                            m_stateMachine.TransitionTo("Move");
                            if (m_random.Float(0f, 1f) < 0.1f * m_subsystemTime.GameTimeDelta) {
                                m_componentCreature.ComponentCreatureSounds.PlayIdleSound(false);
                            }
                        }
                        else {
                            m_stateMachine.TransitionTo("Feed");
                        }
                    }
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Feed",
                delegate { m_feedTime = m_random.Float(4f, 6f); },
                delegate {
                    m_feedTime -= m_subsystemTime.GameTimeDelta;
                    if (m_componentCreature.ComponentBody.StandingOnValue.HasValue) {
                        m_componentCreature.ComponentCreatureModel.FeedOrder = true;
                        if (m_random.Float(0f, 1f) < 0.1f * m_subsystemTime.GameTimeDelta) {
                            m_componentCreature.ComponentCreatureSounds.PlayIdleSound(false);
                        }
                        if (m_random.Float(0f, 1f) < 1.5f * m_subsystemTime.GameTimeDelta) {
                            m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(2f);
                        }
                    }
                    if (m_feedTime <= 0f) {
                        if (m_autoFeed) {
                            float num = m_random.Float(0f, 1f);
                            if (num < 0.33f) {
                                m_stateMachine.TransitionTo("Inactive");
                            }
                            if (num < 0.66f) {
                                m_stateMachine.TransitionTo("Move");
                            }
                            else {
                                m_stateMachine.TransitionTo("LookAround");
                            }
                        }
                        else {
                            m_importanceLevel = 0f;
                        }
                    }
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }
    }
}