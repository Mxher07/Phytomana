using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentSwimAroundBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTerrain m_subsystemTerrain;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public StateMachine m_stateMachine = new();

        public float m_importanceLevel = 1f;

        public Random m_random = new();

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            if (string.IsNullOrEmpty(m_stateMachine.CurrentState)) {
                m_stateMachine.TransitionTo("Inactive");
            }
            if (m_random.Float(0f, 1f) < 0.05f * dt) {
                m_importanceLevel = m_random.Float(1f, 3f);
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
                        m_stateMachine.TransitionTo("Swim");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Stuck",
                delegate {
                    if (m_random.Float(0f, 1f) < 0.5f) {
                        m_importanceLevel = 1f;
                    }
                    m_stateMachine.TransitionTo("Swim");
                },
                null,
                null
            );
            m_stateMachine.AddState(
                "Swim",
                delegate { m_componentPathfinding.Stop(); },
                delegate {
                    _ = m_componentCreature.ComponentBody.Position;
                    if (!m_componentPathfinding.Destination.HasValue) {
                        Vector3? destination = FindDestination();
                        if (destination.HasValue) {
                            m_componentPathfinding.SetDestination(
                                destination,
                                m_random.Float(0.3f, 0.4f),
                                1f,
                                0,
                                false,
                                true,
                                false,
                                null
                            );
                        }
                        else {
                            m_importanceLevel = 1f;
                        }
                    }
                    else if (m_componentPathfinding.IsStuck) {
                        m_stateMachine.TransitionTo("Stuck");
                    }
                },
                null
            );
        }

        public virtual Vector3? FindDestination() {
            Vector3 vector = 0.5f * (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max);
            float num = 2f;
            Vector3? result = null;
            float num2 = m_random.Float(10f, 16f);
            for (int i = 0; i < 16; i++) {
                Vector2 vector2 = m_random.Vector2(1f, 1f);
                float y = 0.3f * m_random.Float(-0.9f, 1f);
                Vector3 v = Vector3.Normalize(new Vector3(vector2.X, y, vector2.Y));
                Vector3 vector3 = vector + num2 * v;
                TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(
                    vector,
                    vector3,
                    false,
                    false,
                    delegate(int value, float _) {
                        int num3 = Terrain.ExtractContents(value);
                        return !(BlocksManager.Blocks[num3] is WaterBlock);
                    }
                );
                if (!terrainRaycastResult.HasValue) {
                    if (num2 > num) {
                        result = vector3;
                        num = num2;
                    }
                }
                else if (terrainRaycastResult.Value.Distance > num) {
                    result = vector + v * terrainRaycastResult.Value.Distance;
                    num = terrainRaycastResult.Value.Distance;
                }
            }
            return result;
        }
    }
}