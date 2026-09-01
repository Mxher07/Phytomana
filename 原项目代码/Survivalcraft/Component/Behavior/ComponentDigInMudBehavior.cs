using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentDigInMudBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemTime m_subsystemTime;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public ComponentMiner m_componentMiner;

        public ComponentFishModel m_componentFishModel;

        public ComponentSwimAwayBehavior m_componentSwimAwayBehavior;

        public StateMachine m_stateMachine = new();

        public Random m_random = new();

        public float m_importanceLevel;

        public double m_sinkTime;

        public double m_digInTime;

        public double m_digOutTime = double.NegativeInfinity;

        public float m_maxDigInDepth;

        public int m_digInBlockIndex;

        public ComponentBody m_collidedWithBody;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            m_stateMachine.Update();
            m_collidedWithBody = null;
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_componentMiner = Entity.FindComponent<ComponentMiner>(true);
            m_componentFishModel = Entity.FindComponent<ComponentFishModel>(true);
            m_componentSwimAwayBehavior = Entity.FindComponent<ComponentSwimAwayBehavior>(true);
            string digInBlockName = valuesDictionary.GetValue<string>("DigInBlockName");
            m_digInBlockIndex = !string.IsNullOrEmpty(digInBlockName)
                ? BlocksManager.Blocks.First(b => b.GetType().Name == digInBlockName).BlockIndex
                : 0;
            m_maxDigInDepth = valuesDictionary.GetValue<float>("MaxDigInDepth");
            m_componentCreature.ComponentBody.CollidedWithBody += delegate(ComponentBody b) { m_collidedWithBody = b; };
            m_stateMachine.AddState(
                "Inactive",
                delegate { m_importanceLevel = 0f; },
                delegate {
                    if (m_random.Float(0f, 1f) < 0.5f * m_subsystemTime.GameTimeDelta
                        && m_subsystemTime.GameTime > m_digOutTime + 15.0
                        && m_digInBlockIndex != 0) {
                        int x = Terrain.ToCell(m_componentCreature.ComponentBody.Position.X);
                        int y = Terrain.ToCell(m_componentCreature.ComponentBody.Position.Y - 0.9f);
                        int z = Terrain.ToCell(m_componentCreature.ComponentBody.Position.Z);
                        if (m_subsystemTerrain.Terrain.GetCellContents(x, y, z) == m_digInBlockIndex) {
                            m_importanceLevel = m_random.Float(1f, 3f);
                        }
                    }
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Sink");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Sink",
                delegate {
                    m_importanceLevel = 10f;
                    m_sinkTime = m_subsystemTime.GameTime;
                    m_componentPathfinding.Stop();
                },
                delegate {
                    if (m_random.Float(0f, 1f) < 2f * m_subsystemTime.GameTimeDelta
                        && m_componentCreature.ComponentBody.StandingOnValue == m_digInBlockIndex
                        && m_componentCreature.ComponentBody.Velocity.LengthSquared() < 1f) {
                        m_stateMachine.TransitionTo("DigIn");
                    }
                    if (!IsActive
                        || m_subsystemTime.GameTime > m_sinkTime + 6.0) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "DigIn",
                delegate {
                    m_digInTime = m_subsystemTime.GameTime;
                    m_digOutTime = m_digInTime + m_random.Float(30f, 60f);
                },
                delegate {
                    m_componentFishModel.DigInOrder = m_maxDigInDepth;
                    if (m_collidedWithBody != null) {
                        if (m_subsystemTime.GameTime - m_digInTime > 2.0
                            && m_collidedWithBody.Density < 0.95f) {
                            m_componentMiner.Hit(
                                m_collidedWithBody,
                                m_collidedWithBody.Position,
                                Vector3.Normalize(m_collidedWithBody.Position - m_componentCreature.ComponentBody.Position)
                            );
                        }
                        m_componentSwimAwayBehavior.SwimAwayFrom(m_collidedWithBody);
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    if (!IsActive
                        || m_subsystemTime.GameTime >= m_digOutTime
                        || m_componentCreature.ComponentBody.StandingOnValue != m_digInBlockIndex
                        || m_componentCreature.ComponentBody.Velocity.LengthSquared() > 1f) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public virtual Vector3? FindDestination() {
            for (int i = 0; i < 8; i++) {
                Vector2 vector = m_random.Vector2(1f, 1f);
                float y = 0.2f * m_random.Float(-0.8f, 1f);
                Vector3 v = Vector3.Normalize(new Vector3(vector.X, y, vector.Y));
                Vector3 vector2 = m_componentCreature.ComponentBody.Position + m_random.Float(8f, 16f) * v;
                TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(
                    m_componentCreature.ComponentBody.Position,
                    vector2,
                    false,
                    false,
                    delegate(int value, float _) {
                        int num = Terrain.ExtractContents(value);
                        return !(BlocksManager.Blocks[num] is WaterBlock);
                    }
                );
                if (!terrainRaycastResult.HasValue) {
                    return vector2;
                }
                if (terrainRaycastResult.Value.Distance > 4f) {
                    return m_componentCreature.ComponentBody.Position + v * terrainRaycastResult.Value.Distance;
                }
            }
            return null;
        }
    }
}