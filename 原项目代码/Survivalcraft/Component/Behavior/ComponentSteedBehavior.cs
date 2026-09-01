using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentSteedBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemBodies m_subsystemBodies;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public ComponentMount m_componentMount;

        public StateMachine m_stateMachine = new();

        public float m_importanceLevel;

        public bool m_isEnabled;

        public Random m_random = new();

        public DynamicArray<ComponentBody> m_bodies = [];

        public float[] m_speedLevels = [-0.33f, 0f, 0.33f, 0.66f, 1f];

        public int m_speedLevel;

        public float m_speed;

        public float m_turnSpeed;

        public float m_speedChangeFactor;

        public float m_timeToSpeedReduction;

        public double m_lastNotBlockedTime;

        public int SpeedOrder { get; set; }

        public float TurnOrder { get; set; }

        public float JumpOrder { get; set; }

        public bool WasOrderIssued { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            m_stateMachine.Update();
            if (SpeedOrder != 0
                || TurnOrder != 0f
                || JumpOrder != 0f) {
                SpeedOrder = 0;
                TurnOrder = 0f;
                JumpOrder = 0f;
                WasOrderIssued = true;
            }
            else {
                WasOrderIssued = false;
            }
            if (m_subsystemTime.PeriodicGameTimeEvent(1.0, GetHashCode() % 100 * 0.01f)) {
                m_importanceLevel = 0f;
                if (m_isEnabled) {
                    if (m_componentMount.Rider != null) {
                        m_importanceLevel = 275f;
                    }
                    else if (FindNearbyRider(7f) != null) {
                        m_importanceLevel = 7f;
                    }
                }
            }
            if (!IsActive) {
                m_stateMachine.TransitionTo("Inactive");
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_componentMount = Entity.FindComponent<ComponentMount>(true);
            m_isEnabled = Entity.ValuesDictionary.DatabaseObject.Name.EndsWith("_Saddled");
            m_stateMachine.AddState(
                "Inactive",
                null,
                delegate {
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Wait");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Wait",
                delegate {
                    ComponentRider componentRider = FindNearbyRider(6f);
                    if (componentRider != null) {
                        m_componentPathfinding.SetDestination(
                            componentRider.ComponentCreature.ComponentBody.Position,
                            m_random.Float(0.2f, 0.3f),
                            3.25f,
                            0,
                            false,
                            true,
                            false,
                            null
                        );
                        if (m_random.Float(0f, 1f) < 0.5f) {
                            m_componentCreature.ComponentCreatureSounds.PlayIdleSound(true);
                        }
                    }
                },
                delegate {
                    if (m_componentMount.Rider != null) {
                        m_stateMachine.TransitionTo("Steed");
                    }
                    m_componentCreature.ComponentCreatureModel.LookRandomOrder = true;
                },
                null
            );
            m_stateMachine.AddState(
                "Steed",
                delegate {
                    m_componentPathfinding.Stop();
                    m_speed = 0f;
                    m_speedLevel = 1;
                },
                delegate { ProcessRidingOrders(); },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public virtual ComponentRider FindNearbyRider(float range) {
            m_bodies.Clear();
            m_subsystemBodies.FindBodiesAroundPoint(
                new Vector2(m_componentCreature.ComponentBody.Position.X, m_componentCreature.ComponentBody.Position.Z),
                range,
                m_bodies
            );
            foreach (ComponentBody body in m_bodies) {
                if (Vector3.DistanceSquared(m_componentCreature.ComponentBody.Position, body.Position) < range * range) {
                    ComponentRider componentRider = body.Entity.FindComponent<ComponentRider>();
                    if (componentRider != null) {
                        return componentRider;
                    }
                }
            }
            return null;
        }

        public virtual void ProcessRidingOrders() {
            m_speedLevel = Math.Clamp(m_speedLevel + SpeedOrder, 0, m_speedLevels.Length - 1);
            if (m_speedLevel == m_speedLevels.Length - 1
                && SpeedOrder > 0) {
                m_timeToSpeedReduction = m_random.Float(8f, 12f);
            }
            if (m_speedLevel == 0
                && SpeedOrder < 0) {
                m_timeToSpeedReduction = 1.25f;
            }
            m_timeToSpeedReduction -= m_subsystemTime.GameTimeDelta;
            if (m_timeToSpeedReduction <= 0f
                && m_speedLevel == m_speedLevels.Length - 1) {
                m_speedLevel--;
                m_speedChangeFactor = 0.25f;
            }
            else if (m_timeToSpeedReduction <= 0f
                && m_speedLevel == 0) {
                m_speedLevel = 1;
                m_speedChangeFactor = 100f;
            }
            else {
                m_speedChangeFactor = 100f;
            }
            if (m_subsystemTime.PeriodicGameTimeEvent(0.25, 0.0)) {
                float num = new Vector2(
                    m_componentCreature.ComponentBody.CollisionVelocityChange.X,
                    m_componentCreature.ComponentBody.CollisionVelocityChange.Z
                ).Length();
                if (m_speedLevel == 0
                    || num < 0.1f
                    || m_componentCreature.ComponentBody.Velocity.Length()
                    > MathF.Abs(0.5f * m_speed * m_componentCreature.ComponentLocomotion.WalkSpeed)) {
                    m_lastNotBlockedTime = m_subsystemTime.GameTime;
                }
                else if (m_subsystemTime.GameTime - m_lastNotBlockedTime > 0.75) {
                    m_speedLevel = 1;
                }
            }
            m_speed += MathUtils.Saturate(m_speedChangeFactor * m_subsystemTime.GameTimeDelta) * (m_speedLevels[m_speedLevel] - m_speed);
            m_turnSpeed += 2f * m_subsystemTime.GameTimeDelta * (Math.Clamp(TurnOrder, -0.5f, 0.5f) - m_turnSpeed);
            m_componentCreature.ComponentLocomotion.TurnOrder = new Vector2(m_turnSpeed, 0f);
            m_componentCreature.ComponentLocomotion.WalkOrder = new Vector2(0f, m_speed);
            if (MathF.Abs(m_speed) > 0.01f
                || MathF.Abs(m_turnSpeed) > 0.01f) {
                m_componentCreature.ComponentLocomotion.LookOrder = new Vector2(2f * m_turnSpeed, 0f)
                    - m_componentCreature.ComponentLocomotion.LookAngles;
            }
            m_componentCreature.ComponentLocomotion.JumpOrder = MathF.Max(m_componentCreature.ComponentLocomotion.JumpOrder, JumpOrder);
        }
    }
}