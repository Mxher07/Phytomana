using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentSwimAwayBehavior : ComponentBehavior, IUpdateable, IComponentEscapeBehavior {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemTime m_subsystemTime;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public ComponentHerdBehavior m_componentHerdBehavior;

        public StateMachine m_stateMachine = new();

        public Random m_random = new();

        public float m_importanceLevel;

        public ComponentFrame m_attacker;

        public float m_timeToForgetAttacker;
        public float LowHealthToEscape { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void SwimAwayFrom(ComponentBody attacker) {
            m_attacker = attacker;
            m_timeToForgetAttacker = m_random.Float(10f, 20f);
        }

        public virtual void Update(float dt) {
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_componentHerdBehavior = Entity.FindComponent<ComponentHerdBehavior>();
            LowHealthToEscape = valuesDictionary.GetValue<float>("LowHealthToEscape");
            m_componentCreature.ComponentHealth.Injured += delegate(Injury injury) {
                ComponentCreature attacker = injury.Attacker;
                SwimAwayFrom(attacker?.ComponentBody);
            };
            m_stateMachine.AddState(
                "Inactive",
                delegate {
                    m_importanceLevel = 0f;
                    m_attacker = null;
                },
                delegate {
                    if (m_attacker != null) {
                        m_timeToForgetAttacker -= m_subsystemTime.GameTimeDelta;
                        if (m_timeToForgetAttacker <= 0f) {
                            m_attacker = null;
                        }
                    }
                    if (m_componentCreature.ComponentHealth.HealthChange < 0f) {
                        m_importanceLevel = m_componentCreature.ComponentHealth.Health < LowHealthToEscape ? 300 : 100;
                    }
                    else if (m_attacker != null
                        && Vector3.DistanceSquared(m_attacker.Position, m_componentCreature.ComponentBody.Position) < 25f) {
                        m_importanceLevel = 100f;
                    }
                    if (IsActive) {
                        m_stateMachine.TransitionTo("SwimmingAway");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "SwimmingAway",
                delegate {
                    m_componentPathfinding.SetDestination(
                        FindSafePlace(),
                        1f,
                        1f,
                        0,
                        false,
                        true,
                        false,
                        null
                    );
                },
                delegate {
                    if (!IsActive
                        || !m_componentPathfinding.Destination.HasValue
                        || m_componentPathfinding.IsStuck) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public virtual Vector3 FindSafePlace() {
            Vector3 vector = 0.5f * (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max);
            Vector3? herdPosition = m_componentHerdBehavior?.FindHerdCenter();
            float num = float.NegativeInfinity;
            Vector3 result = vector;
            for (int i = 0; i < 40; i++) {
                Vector2 vector2 = m_random.Vector2(1f, 1f);
                float y = 0.4f * m_random.Float(-1f, 1f);
                Vector3 v = Vector3.Normalize(new Vector3(vector2.X, y, vector2.Y));
                Vector3 vector3 = vector + m_random.Float(10f, 20f) * v;
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
                Vector3 vector4 = terrainRaycastResult.HasValue ? vector + v * terrainRaycastResult.Value.Distance : vector3;
                float num2 = ScoreSafePlace(vector, vector4, herdPosition);
                if (num2 > num) {
                    num = num2;
                    result = vector4;
                }
            }
            return result;
        }

        public virtual float ScoreSafePlace(Vector3 currentPosition, Vector3 safePosition, Vector3? herdPosition) {
            Vector2 vector = new(currentPosition.X, currentPosition.Z);
            Vector2 vector2 = new(safePosition.X, safePosition.Z);
            Vector2? vector3 = herdPosition.HasValue ? new Vector2?(new Vector2(herdPosition.Value.X, herdPosition.Value.Z)) : null;
            Segment2 s = new(vector, vector2);
            float num = vector3.HasValue ? Segment2.Distance(s, vector3.Value) : 0f;
            if (m_attacker != null) {
                Vector3 position = m_attacker.Position;
                Vector2 vector4 = new(position.X, position.Z);
                float num2 = Vector2.Distance(vector4, vector2);
                float num3 = Segment2.Distance(s, vector4);
                return num2 + 1.5f * num3 - num;
            }
            return 1.5f * Vector2.Distance(vector, vector2) - num;
        }
    }
}