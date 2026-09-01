using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentFishOutOfWaterBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemTime m_subsystemTime;

        public ComponentCreature m_componentCreature;

        public ComponentFishModel m_componentFishModel;

        public StateMachine m_stateMachine = new();

        public float m_importanceLevel;

        public float m_outOfWaterTime;

        public Vector2 m_direction;

        public Random m_random = new();

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentFishModel = Entity.FindComponent<ComponentFishModel>(true);
            m_stateMachine.AddState(
                "Inactive",
                null,
                delegate {
                    if (IsOutOfWater()) {
                        m_outOfWaterTime += m_subsystemTime.GameTimeDelta;
                    }
                    else {
                        m_outOfWaterTime = 0f;
                    }
                    if (m_outOfWaterTime > 3f) {
                        m_importanceLevel = 1000f;
                    }
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Jump");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Jump",
                null,
                delegate {
                    m_componentFishModel.BendOrder = 2f
                        * (2f
                            * MathUtils.Saturate(
                                SimplexNoise.OctavedNoise(
                                    (float)MathUtils.Remainder(m_subsystemTime.GameTime, 1000.0),
                                    1.2f * m_componentCreature.ComponentLocomotion.TurnSpeed,
                                    1,
                                    1f,
                                    1f
                                )
                            )
                            - 1f);
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    if (!IsOutOfWater()) {
                        m_importanceLevel = 0f;
                    }
                    if (m_random.Float(0f, 1f) < 2.5f * m_subsystemTime.GameTimeDelta) {
                        m_componentCreature.ComponentLocomotion.JumpOrder = m_random.Float(0.33f, 1f);
                        m_direction = new Vector2(MathF.Sign(m_componentFishModel.BendOrder.Value), 0f);
                    }
                    if (!m_componentCreature.ComponentBody.StandingOnValue.HasValue) {
                        m_componentCreature.ComponentLocomotion.TurnOrder = new Vector2(0f - m_componentFishModel.BendOrder.Value, 0f);
                        m_componentCreature.ComponentLocomotion.WalkOrder = m_direction;
                    }
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public virtual bool IsOutOfWater() => m_componentCreature.ComponentBody.ImmersionFactor < 0.33f;

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