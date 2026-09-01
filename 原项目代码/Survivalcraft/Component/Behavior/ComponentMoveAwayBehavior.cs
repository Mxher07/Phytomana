using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentMoveAwayBehavior : ComponentBehavior, IUpdateable {
        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public StateMachine m_stateMachine = new();

        public Random m_random = new();

        public float m_importanceLevel;

        public ComponentBody m_target;

        public bool m_isFast;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_componentCreature.ComponentBody.CollidedWithBody += delegate(ComponentBody body) {
                m_target = body;
                m_isFast = MathUtils.Max(body.Velocity.Length(), m_componentCreature.ComponentBody.Velocity.Length()) > 3f;
            };
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
                    if (m_target != null) {
                        m_importanceLevel = 6f;
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Move",
                delegate {
                    if (m_random.Float(0f, 1f) < 0.5f) {
                        m_componentCreature.ComponentCreatureSounds.PlayIdleSound(true);
                    }
                    if (m_target != null) {
                        Vector3 vector = m_target.Position + 0.5f * m_target.Velocity;
                        Vector2 v = Vector2.Normalize(m_componentCreature.ComponentBody.Position.XZ - vector.XZ);
                        Vector2 vector2 = Vector2.Zero;
                        float num = float.MinValue;
                        for (float num2 = 0f; num2 < (float)Math.PI * 2f; num2 += 0.1f) {
                            Vector2 vector3 = Vector2.CreateFromAngle(num2);
                            if (Vector2.Dot(vector3, v) > 0.2f) {
                                float num3 = Vector2.Dot(m_componentCreature.ComponentBody.Matrix.Forward.XZ, vector3);
                                if (num3 > num) {
                                    vector2 = vector3;
                                    num = num3;
                                }
                            }
                        }
                        float s = m_random.Float(1.5f, 2f);
                        float speed = m_isFast ? 0.7f : 0.35f;
                        m_componentPathfinding.SetDestination(
                            m_componentCreature.ComponentBody.Position + s * new Vector3(vector2.X, 0f, vector2.Y),
                            speed,
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
                    else if (m_componentPathfinding.IsStuck
                        || !m_componentPathfinding.Destination.HasValue) {
                        m_importanceLevel = 0f;
                    }
                    m_componentCreature.ComponentCreatureModel.LookRandomOrder = true;
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }
    }
}