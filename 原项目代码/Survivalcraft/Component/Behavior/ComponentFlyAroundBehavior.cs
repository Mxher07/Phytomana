using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentFlyAroundBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTerrain m_subsystemTerrain;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public StateMachine m_stateMachine = new();

        public float m_angle;

        public float m_importanceLevel = 1f;

        public Random m_random = new();

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            if (string.IsNullOrEmpty(m_stateMachine.CurrentState)) {
                m_stateMachine.TransitionTo("Inactive");
            }
            if (m_random.Float(0f, 1f) < 0.05f * dt) {
                m_importanceLevel = m_random.Float(1f, 2f);
            }
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
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_stateMachine.AddState(
                "Inactive",
                null,
                delegate {
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Fly");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Stuck",
                delegate {
                    m_stateMachine.TransitionTo("Fly");
                    if (m_random.Float(0f, 1f) < 0.5f) {
                        m_componentCreature.ComponentCreatureSounds.PlayIdleSound(false);
                        m_importanceLevel = 1f;
                    }
                },
                null,
                null
            );
            m_stateMachine.AddState(
                "Fly",
                delegate {
                    m_angle = m_random.Float(0f, (float)Math.PI * 2f);
                    m_componentPathfinding.Stop();
                },
                delegate {
                    Vector3 position = m_componentCreature.ComponentBody.Position;
                    if (!m_componentPathfinding.Destination.HasValue) {
                        float num = m_random.Float(0f, 1f) < 0.2f ? m_random.Float(0.4f, 0.6f) : 0f - m_random.Float(0.4f, 0.6f);
                        m_angle = MathUtils.NormalizeAngle(m_angle + num);
                        Vector2 vector = Vector2.CreateFromAngle(m_angle);
                        Vector3 value = position + new Vector3(vector.X, 0f, vector.Y) * 10f;
                        value.Y = EstimateHeight(new Vector2(value.X, value.Z), 8) + m_random.Float(3f, 5f);
                        m_componentPathfinding.SetDestination(
                            value,
                            m_random.Float(0.6f, 1.05f),
                            6f,
                            0,
                            false,
                            true,
                            false,
                            null
                        );
                        if (m_random.Float(0f, 1f) < 0.15f) {
                            m_componentCreature.ComponentCreatureSounds.PlayIdleSound(false);
                        }
                    }
                    else if (m_componentPathfinding.IsStuck) {
                        m_stateMachine.TransitionTo("Stuck");
                    }
                },
                null
            );
        }

        public virtual float EstimateHeight(Vector2 position, int radius) {
            int num = 0;
            for (int i = 0; i < 15; i++) {
                int x = Terrain.ToCell(position.X) + m_random.Int(-radius, radius);
                int z = Terrain.ToCell(position.Y) + m_random.Int(-radius, radius);
                num = MathUtils.Max(num, m_subsystemTerrain.Terrain.GetTopHeight(x, z));
            }
            return num;
        }
    }
}