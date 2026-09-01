using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentRandomPeckBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTerrain m_subsystemTerrain;

        public ComponentCreature m_componentCreature;

        public ComponentBirdModel m_componentBirdModel;

        public ComponentPathfinding m_componentPathfinding;

        public StateMachine m_stateMachine = new();

        public float m_importanceLevel = 1f;

        public float m_dt;

        public float m_peckTime;

        public float m_waitTime;

        public Random m_random = new();

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            if (string.IsNullOrEmpty(m_stateMachine.CurrentState)) {
                m_stateMachine.TransitionTo("Move");
            }
            if (m_random.Float(0f, 1f) < 0.033f * dt) {
                m_importanceLevel = m_random.Float(1f, 2.5f);
            }
            m_dt = dt;
            if (IsActive) {
                m_stateMachine.Update();
            }
            else {
                m_stateMachine.TransitionTo("Inactive");
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentBirdModel = Entity.FindComponent<ComponentBirdModel>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_stateMachine.AddState(
                "Inactive",
                null,
                delegate {
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Move");
                    }
                },
                null
            );
            m_stateMachine.AddState("Stuck", delegate { m_stateMachine.TransitionTo("Move"); }, null, null);
            m_stateMachine.AddState(
                "Move",
                delegate {
                    Vector3 position = m_componentCreature.ComponentBody.Position;
                    float num = m_random.Float(0f, 1f) < 0.2f ? 8f : 3f;
                    Vector3 value = position + new Vector3(num * m_random.Float(-1f, 1f), 0f, num * m_random.Float(-1f, 1f));
                    value.Y = m_subsystemTerrain.Terrain.GetTopHeight(Terrain.ToCell(value.X), Terrain.ToCell(value.Z)) + 1;
                    m_componentPathfinding.SetDestination(
                        value,
                        m_random.Float(0.5f, 0.7f),
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
                        if (m_random.Float(0f, 1f) < 0.33f) {
                            m_stateMachine.TransitionTo("Wait");
                        }
                        else {
                            m_stateMachine.TransitionTo("Peck");
                        }
                    }
                    else if (m_componentPathfinding.IsStuck) {
                        m_stateMachine.TransitionTo("Stuck");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Wait",
                delegate { m_waitTime = m_random.Float(0.75f, 1f); },
                delegate {
                    m_waitTime -= m_dt;
                    if (m_waitTime <= 0f) {
                        if (m_random.Float(0f, 1f) < 0.25f) {
                            m_stateMachine.TransitionTo("Move");
                            if (m_random.Float(0f, 1f) < 0.33f) {
                                m_componentCreature.ComponentCreatureSounds.PlayIdleSound(false);
                            }
                        }
                        else {
                            m_stateMachine.TransitionTo("Peck");
                        }
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Peck",
                delegate { m_peckTime = m_random.Float(2f, 6f); },
                delegate {
                    m_peckTime -= m_dt;
                    if (m_componentCreature.ComponentBody.StandingOnValue.HasValue) {
                        m_componentBirdModel.FeedOrder = true;
                    }
                    if (m_peckTime <= 0f) {
                        if (m_random.Float(0f, 1f) < 0.25f) {
                            m_stateMachine.TransitionTo("Move");
                        }
                        else {
                            m_stateMachine.TransitionTo("Wait");
                        }
                    }
                },
                null
            );
        }
    }
}