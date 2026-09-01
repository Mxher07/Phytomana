using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentDumpRiderBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public ComponentMount m_componentMount;

        public StateMachine m_stateMachine = new();

        public float m_importanceLevel;

        public bool m_isEnabled;

        public Random m_random = new();

        public ComponentRider m_rider;

        public double m_dumpStartTime;

        public Vector2 m_walkOrder;

        public Vector2 m_turnOrder;

        public Vector2 m_lookOrder;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_componentMount = Entity.FindComponent<ComponentMount>(true);
            m_isEnabled = !Entity.ValuesDictionary.DatabaseObject.Name.EndsWith("_Saddled");
            m_stateMachine.AddState(
                "Inactive",
                delegate {
                    m_importanceLevel = 0f;
                    m_rider = null;
                },
                delegate {
                    if (m_isEnabled
                        && m_random.Float(0f, 1f) < 1f * m_subsystemTime.GameTimeDelta
                        && m_componentMount.Rider != null) {
                        m_importanceLevel = 220f;
                        m_dumpStartTime = m_subsystemTime.GameTime;
                        m_rider = m_componentMount.Rider;
                    }
                    if (IsActive) {
                        m_stateMachine.TransitionTo("WildJumping");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "WildJumping",
                delegate {
                    m_componentCreature.ComponentCreatureSounds.PlayPainSound();
                    m_componentPathfinding.Stop();
                },
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    else if (m_componentMount.Rider == null) {
                        m_importanceLevel = 0f;
                        RunAway();
                    }
                    if (m_random.Float(0f, 1f) < 1f * m_subsystemTime.GameTimeDelta) {
                        m_componentCreature.ComponentCreatureSounds.PlayPainSound();
                    }
                    if (m_random.Float(0f, 1f) < 3f * m_subsystemTime.GameTimeDelta) {
                        m_walkOrder = new Vector2(m_random.Float(-0.5f, 0.5f), m_random.Float(-0.5f, 1.5f));
                    }
                    if (m_random.Float(0f, 1f) < 2.5f * m_subsystemTime.GameTimeDelta) {
                        m_turnOrder.X = m_random.Float(-1f, 1f);
                    }
                    if (m_random.Float(0f, 1f) < 2f * m_subsystemTime.GameTimeDelta) {
                        m_componentCreature.ComponentLocomotion.JumpOrder = m_random.Float(0.9f, 1f);
                        if (m_componentMount.Rider != null
                            && m_subsystemTime.GameTime - m_dumpStartTime > 3.0) {
                            if (m_random.Float(0f, 1f) < 0.05f) {
                                m_componentMount.Rider.StartDismounting();
                                m_componentMount.Rider.ComponentCreature.ComponentHealth.Injure(
                                    m_random.Float(0.05f, 0.2f),
                                    m_componentCreature,
                                    false,
                                    "Thrown from a mount"
                                );
                            }
                            if (m_random.Float(0f, 1f) < 0.25f) {
                                m_componentMount.Rider.ComponentCreature.ComponentHealth.Injure(
                                    0.05f,
                                    m_componentCreature,
                                    false,
                                    "Thrown from a mount"
                                );
                            }
                        }
                    }
                    if (m_random.Float(0f, 1f) < 4f * m_subsystemTime.GameTimeDelta) {
                        m_lookOrder = new Vector2(m_random.Float(-3f, 3f), m_lookOrder.Y);
                    }
                    if (m_random.Float(0f, 1f) < 0.25f * m_subsystemTime.GameTimeDelta) {
                        TransitionToRandomDumpingBehavior();
                    }
                    m_componentCreature.ComponentLocomotion.WalkOrder = m_walkOrder;
                    m_componentCreature.ComponentLocomotion.TurnOrder = m_turnOrder;
                    m_componentCreature.ComponentLocomotion.LookOrder = m_lookOrder;
                },
                null
            );
            m_stateMachine.AddState(
                "BlindRacing",
                delegate {
                    m_componentCreature.ComponentCreatureSounds.PlayPainSound();
                    m_componentPathfinding.SetDestination(
                        m_componentCreature.ComponentBody.Position + new Vector3(m_random.Float(-15f, 15f), 0f, m_random.Float(-15f, 15f)),
                        1f,
                        2f,
                        0,
                        false,
                        true,
                        false,
                        null
                    );
                },
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    else if (m_componentMount.Rider == null) {
                        m_importanceLevel = 0f;
                        RunAway();
                    }
                    else if (!m_componentPathfinding.Destination.HasValue
                        || m_componentPathfinding.IsStuck) {
                        TransitionToRandomDumpingBehavior();
                    }
                    if (m_random.Float(0f, 1f) < 0.5f * m_subsystemTime.GameTimeDelta) {
                        m_componentCreature.ComponentLocomotion.JumpOrder = 1f;
                        m_componentCreature.ComponentCreatureSounds.PlayPainSound();
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Stupor",
                delegate {
                    m_componentCreature.ComponentCreatureSounds.PlayPainSound();
                    m_componentPathfinding.Stop();
                },
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    else if (m_componentMount.Rider == null) {
                        m_importanceLevel = 0f;
                    }
                    if (m_subsystemTime.PeriodicGameTimeEvent(2.0, 0.0)) {
                        TransitionToRandomDumpingBehavior();
                    }
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public virtual void TransitionToRandomDumpingBehavior() {
            float num = m_random.Float(0f, 1f);
            if (num < 0.5f) {
                m_stateMachine.TransitionTo("WildJumping");
            }
            else if (num < 0.8f) {
                m_stateMachine.TransitionTo("BlindRacing");
            }
            else {
                m_stateMachine.TransitionTo("Stupor");
            }
        }

        public virtual void RunAway() {
            if (m_rider != null) {
                Entity.FindComponent<ComponentRunAwayBehavior>()?.RunAwayFrom(m_rider.ComponentCreature.ComponentBody);
            }
        }
    }
}