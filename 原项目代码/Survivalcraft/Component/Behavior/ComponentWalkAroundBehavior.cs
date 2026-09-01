using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentWalkAroundBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemTime m_subsystemTime;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public StateMachine m_stateMachine = new();

        public Random m_random = new();

        public float m_importanceLevel;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_stateMachine.AddState(
                "Inactive",
                delegate { m_importanceLevel = m_random.Float(0f, 1f); },
                delegate {
                    if (m_random.Float(0f, 1f) < 0.05f * m_subsystemTime.GameTimeDelta) {
                        m_importanceLevel = m_random.Float(1f, 2f);
                    }
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Walk");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Walk",
                delegate {
                    float speed = m_componentCreature.ComponentBody.ImmersionFactor > 0.5f ? 1f : m_random.Float(0.25f, 0.35f);
                    m_componentPathfinding.SetDestination(
                        FindDestination(),
                        speed,
                        1f,
                        0,
                        false,
                        true,
                        false,
                        null
                    );
                },
                delegate {
                    if (m_componentPathfinding.IsStuck
                        || !IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    if (!m_componentPathfinding.Destination.HasValue) {
                        if (m_random.Float(0f, 1f) < 0.5f) {
                            m_stateMachine.TransitionTo("Inactive");
                        }
                        else {
                            m_stateMachine.TransitionTo(null);
                            m_stateMachine.TransitionTo("Walk");
                        }
                    }
                    if (m_random.Float(0f, 1f) < 0.1f * m_subsystemTime.GameTimeDelta) {
                        m_componentCreature.ComponentCreatureSounds.PlayIdleSound(false);
                    }
                    m_componentCreature.ComponentCreatureModel.LookRandomOrder = true;
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public virtual Vector3 FindDestination() {
            Vector3 position = m_componentCreature.ComponentBody.Position;
            float num = 0f;
            Vector3 result = position;
            for (int i = 0; i < 16; i++) {
                Vector2 vector = Vector2.Normalize(m_random.Vector2(1f)) * m_random.Float(6f, 12f);
                Vector3 vector2 = new(position.X + vector.X, 0f, position.Z + vector.Y);
                vector2.Y = m_subsystemTerrain.Terrain.GetTopHeight(Terrain.ToCell(vector2.X), Terrain.ToCell(vector2.Z)) + 1;
                float num2 = ScoreDestination(vector2);
                if (num2 > num) {
                    num = num2;
                    result = vector2;
                }
            }
            return result;
        }

        public virtual float ScoreDestination(Vector3 destination) {
            float num = 8f - MathF.Abs(m_componentCreature.ComponentBody.Position.Y - destination.Y);
            if (m_subsystemTerrain.Terrain.GetCellContents(
                    Terrain.ToCell(destination.X),
                    Terrain.ToCell(destination.Y) - 1,
                    Terrain.ToCell(destination.Z)
                )
                == 18) {
                num -= 5f;
            }
            return num;
        }
    }
}