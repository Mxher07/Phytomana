using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentCattleDriveBehavior : ComponentBehavior, IUpdateable, INoiseListener {
        public SubsystemTime m_subsystemTime;

        public SubsystemCreatureSpawn m_subsystemCreatureSpawn;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public ComponentHerdBehavior m_componentHerdBehavior;

        public StateMachine m_stateMachine = new();

        public Random m_random = new();

        public float m_importanceLevel;

        public Vector3 m_driveVector;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void HearNoise(ComponentBody sourceBody, Vector3 sourcePosition, float loudness) {
            if (loudness >= 0.5f) {
                Vector3 v = m_componentCreature.ComponentBody.Position - sourcePosition;
                m_driveVector += Vector3.Normalize(v) * MathUtils.Max(8f - 0.25f * v.Length(), 1f);
                float num = 12f + m_random.Float(0f, 3f);
                if (m_driveVector.Length() > num) {
                    m_driveVector = num * Vector3.Normalize(m_driveVector);
                }
            }
        }

        public virtual void Update(float dt) {
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemCreatureSpawn = Project.FindSubsystem<SubsystemCreatureSpawn>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_componentHerdBehavior = Entity.FindComponent<ComponentHerdBehavior>(true);
            m_stateMachine.AddState(
                "Inactive",
                delegate {
                    m_importanceLevel = 0f;
                    m_driveVector = Vector3.Zero;
                },
                delegate {
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Drive");
                    }
                    if (m_driveVector.Length() > 3f) {
                        m_importanceLevel = 7f;
                    }
                    FadeDriveVector();
                },
                null
            );
            m_stateMachine.AddState(
                "Drive",
                delegate { },
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    if (m_driveVector.LengthSquared() < 1f
                        || m_componentPathfinding.IsStuck) {
                        m_importanceLevel = 0f;
                    }
                    if (m_random.Float(0f, 1f) < 0.1f * m_subsystemTime.GameTimeDelta) {
                        m_componentCreature.ComponentCreatureSounds.PlayIdleSound(true);
                    }
                    if (m_random.Float(0f, 1f) < 3f * m_subsystemTime.GameTimeDelta) {
                        Vector3 v = CalculateDriveDirectionAndSpeed();
                        float speed = MathUtils.Saturate(0.2f * v.Length());
                        m_componentPathfinding.SetDestination(
                            m_componentCreature.ComponentBody.Position + 15f * Vector3.Normalize(v),
                            speed,
                            5f,
                            0,
                            false,
                            true,
                            false,
                            null
                        );
                    }
                    FadeDriveVector();
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public virtual void FadeDriveVector() {
            float num = m_driveVector.Length();
            if (num > 0.1f) {
                m_driveVector -= m_subsystemTime.GameTimeDelta * m_driveVector / num;
            }
        }

        public virtual Vector3 CalculateDriveDirectionAndSpeed() {
            int num = 1;
            Vector3 position = m_componentCreature.ComponentBody.Position;
            Vector3 v = position;
            Vector3 driveVector = m_driveVector;
            foreach (ComponentCreature creature in m_subsystemCreatureSpawn.Creatures) {
                if (creature != m_componentCreature
                    && creature.ComponentHealth.Health > 0f) {
                    ComponentCattleDriveBehavior componentCattleDriveBehavior = creature.Entity.FindComponent<ComponentCattleDriveBehavior>();
                    if (componentCattleDriveBehavior != null
                        && componentCattleDriveBehavior.m_componentHerdBehavior.HerdName == m_componentHerdBehavior.HerdName) {
                        Vector3 position2 = creature.ComponentBody.Position;
                        if (Vector3.DistanceSquared(position, position2) < 625f) {
                            v += position2;
                            driveVector += componentCattleDriveBehavior.m_driveVector;
                            num++;
                        }
                    }
                }
            }
            v /= num;
            driveVector /= num;
            Vector3 v2 = v - position;
            float s = MathUtils.Max(1.5f * v2.Length() - 3f, 0f);
            return 0.33f * m_driveVector + 0.66f * driveVector + s * Vector3.Normalize(v2);
        }
    }
}