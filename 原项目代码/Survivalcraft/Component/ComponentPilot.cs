using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentPilot : Component, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemBodies m_subsystemBodies;

        public SubsystemTerrain m_subsystemTerrain;

        public ComponentCreature m_componentCreature;

        public Random m_random = new();

        public Vector2? m_walkOrder;

        public Vector3? m_flyOrder;

        public Vector3? m_swimOrder;

        public Vector2 m_turnOrder;

        public float m_jumpOrder;

        public double m_nextUpdateTime;

        public double m_lastStuckCheckTime;

        public int m_stuckCount;

        public double? m_aboveBelowTime;

        public Vector3? m_lastStuckCheckPosition;

        public DynamicArray<ComponentBody> m_nearbyBodies = [];

        public double m_nextBodiesUpdateTime;

        public static bool DrawPilotDestination;

        public int m_maxFallHeight = 5;

        public int m_maxFallHeightRisk = 7;

        public Vector3? Destination { get; set; }

        public float Speed { get; set; }

        public float Range { get; set; }

        public bool IgnoreHeightDifference { get; set; }

        public bool RaycastDestination { get; set; }

        public bool TakeRisks { get; set; }

        public ComponentBody DoNotAvoidBody { get; set; }

        public bool IsStuck { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public virtual void SetDestination(Vector3? destination,
            float speed,
            float range,
            bool ignoreHeightDifference,
            bool raycastDestination,
            bool takeRisks,
            ComponentBody doNotAvoidBody) {
            bool flag = true;
            if (Destination.HasValue
                && destination.HasValue) {
                Vector3 v = Vector3.Normalize(Destination.Value - m_componentCreature.ComponentBody.Position);
                if (Vector3.Dot(Vector3.Normalize(destination.Value - m_componentCreature.ComponentBody.Position), v) > 0.5f) {
                    flag = false;
                }
            }
            if (flag) {
                IsStuck = false;
                m_lastStuckCheckPosition = null;
                m_aboveBelowTime = null;
            }
            Destination = destination;
            Speed = speed;
            Range = range;
            IgnoreHeightDifference = ignoreHeightDifference;
            RaycastDestination = raycastDestination;
            TakeRisks = takeRisks;
            DoNotAvoidBody = doNotAvoidBody;
        }

        public virtual void Stop() {
            SetDestination(
                null,
                0f,
                0f,
                false,
                false,
                false,
                null
            );
        }

        public virtual void Update(float dt) {
            if (m_subsystemTime.GameTime >= m_nextUpdateTime) {
                m_nextUpdateTime = m_subsystemTime.GameTime + m_random.Float(0.09f, 0.11f);
                m_walkOrder = null;
                m_flyOrder = null;
                m_swimOrder = null;
                m_turnOrder = Vector2.Zero;
                m_jumpOrder = 0f;
                if (Destination.HasValue) {
                    Vector3 position = m_componentCreature.ComponentBody.Position;
                    Vector3 forward = m_componentCreature.ComponentBody.Matrix.Forward;
                    Vector3 v = AvoidNearestBody(position, Destination.Value);
                    Vector3 vector = v - position;
                    float num = vector.LengthSquared();
                    Vector2 vector2 = new Vector2(v.X, v.Z) - new Vector2(position.X, position.Z);
                    float num2 = vector2.LengthSquared();
                    float x = Vector2.Angle(forward.XZ, vector.XZ);
                    float num3 = (m_componentCreature.ComponentBody.CollisionVelocityChange * new Vector3(1f, 0f, 1f)).LengthSquared() > 0f
                        && m_componentCreature.ComponentBody.StandingOnValue.HasValue
                            ? 0.15f
                            : 0.4f;
                    if (m_subsystemTime.GameTime >= m_lastStuckCheckTime + num3
                        || !m_lastStuckCheckPosition.HasValue) {
                        m_lastStuckCheckTime = m_subsystemTime.GameTime;
                        if (MathF.Abs(x) > MathUtils.DegToRad(20f)
                            || !m_lastStuckCheckPosition.HasValue
                            || Vector3.Dot(position - m_lastStuckCheckPosition.Value, Vector3.Normalize(vector)) > 0.2f) {
                            m_lastStuckCheckPosition = position;
                            m_stuckCount = 0;
                        }
                        else {
                            m_stuckCount++;
                        }
                        IsStuck = m_stuckCount >= 4;
                    }
                    if (m_componentCreature.ComponentLocomotion.FlySpeed > 0f
                        && (num > 9f
                            || vector.Y > 0.5f
                            || vector.Y < -1.5f
                            || (!m_componentCreature.ComponentBody.StandingOnValue.HasValue
                                && m_componentCreature.ComponentBody.ImmersionFactor == 0f))
                        && m_componentCreature.ComponentBody.ImmersionFactor < 1f) {
                        float y = MathUtils.Min(0.08f * vector2.LengthSquared(), 12f);
                        Vector3 v2 = v + new Vector3(0f, y, 0f);
                        Vector3 value2 = Speed * Vector3.Normalize(v2 - position);
                        value2.Y = MathUtils.Max(value2.Y, -0.5f);
                        m_flyOrder = value2;
                        m_turnOrder = new Vector2(Math.Clamp(x, -1f, 1f), 0f);
                    }
                    else if (m_componentCreature.ComponentLocomotion.SwimSpeed > 0f
                        && m_componentCreature.ComponentBody.ImmersionFactor > 0.5f) {
                        Vector3 value3 = Speed * Vector3.Normalize(v - position);
                        value3.Y = Math.Clamp(value3.Y, -0.5f, 0.5f);
                        m_swimOrder = value3;
                        m_turnOrder = new Vector2(Math.Clamp(x, -1f, 1f), 0f);
                    }
                    else if (m_componentCreature.ComponentLocomotion.WalkSpeed > 0f) {
                        if (IsTerrainSafeToGo(position, vector)) {
                            m_turnOrder = new Vector2(Math.Clamp(x, -1f, 1f), 0f);
                            if (num2 > 1f) {
                                m_walkOrder = new Vector2(0f, MathUtils.Lerp(Speed, 0f, MathUtils.Saturate((MathF.Abs(x) - 0.33f) / 0.66f)));
                                if (Speed >= 1f
                                    && m_componentCreature.ComponentLocomotion.InAirWalkFactor >= 1f
                                    && num > 1f
                                    && m_random.Float(0f, 1f) < 0.05f) {
                                    m_jumpOrder = 1f;
                                }
                            }
                            else {
                                float x2 = Speed * MathUtils.Min(1f * MathF.Sqrt(num2), 1f);
                                m_walkOrder = new Vector2(0f, MathUtils.Lerp(x2, 0f, MathUtils.Saturate(2f * MathF.Abs(x))));
                            }
                        }
                        else {
                            IsStuck = true;
                        }
                        m_componentCreature.ComponentBody.IsSmoothRiseEnabled = num2 >= 1f || vector.Y >= -0.1f;
                        if (num2 < 1f
                            && (vector.Y < -0.5f || vector.Y > 1f)) {
                            if (vector.Y > 0f
                                && m_random.Float(0f, 1f) < 0.05f) {
                                m_jumpOrder = 1f;
                            }
                            if (!m_aboveBelowTime.HasValue) {
                                m_aboveBelowTime = m_subsystemTime.GameTime;
                            }
                            else if (m_subsystemTime.GameTime - m_aboveBelowTime.Value > 2.0
                                && m_componentCreature.ComponentBody.StandingOnValue.HasValue) {
                                IsStuck = true;
                            }
                        }
                        else {
                            m_aboveBelowTime = null;
                        }
                    }
                    if (!IgnoreHeightDifference ? num <= Range * Range : num2 <= Range * Range) {
                        if (RaycastDestination) {
                            if (!m_subsystemTerrain.Raycast(
                                    position + new Vector3(0f, 0.5f, 0f),
                                    v + new Vector3(0f, 0.5f, 0f),
                                    false,
                                    true,
                                    (value, _) => BlocksManager.Blocks[Terrain.ExtractContents(value)].GetIsCollidable(m_componentCreature.ComponentBody, value)
                                )
                                .HasValue) {
                                Destination = null;
                            }
                        }
                        else {
                            Destination = null;
                        }
                    }
                }
                if (!Destination.HasValue
                    && m_componentCreature.ComponentLocomotion.FlySpeed > 0f
                    && !m_componentCreature.ComponentBody.StandingOnValue.HasValue
                    && m_componentCreature.ComponentBody.ImmersionFactor == 0f) {
                    m_turnOrder = Vector2.Zero;
                    m_walkOrder = null;
                    m_swimOrder = null;
                    m_flyOrder = new Vector3(0f, -0.5f, 0f);
                }
            }
            m_componentCreature.ComponentLocomotion.WalkOrder = CombineNullables(m_componentCreature.ComponentLocomotion.WalkOrder, m_walkOrder);
            m_componentCreature.ComponentLocomotion.SwimOrder = CombineNullables(m_componentCreature.ComponentLocomotion.SwimOrder, m_swimOrder);
            m_componentCreature.ComponentLocomotion.TurnOrder += m_turnOrder;
            m_componentCreature.ComponentLocomotion.FlyOrder = CombineNullables(m_componentCreature.ComponentLocomotion.FlyOrder, m_flyOrder);
            m_componentCreature.ComponentLocomotion.JumpOrder = MathUtils.Max(m_jumpOrder, m_componentCreature.ComponentLocomotion.JumpOrder);
            m_jumpOrder = 0f;
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
        }

        public virtual bool ShouldAvoidBlock(Block block, int cellValue) => block.ShouldAvoid(cellValue, this);

        /// <summary>
        ///     地形是否安全可通行
        /// </summary>
        public virtual bool IsTerrainSafeToGo(Vector3 position, Vector3 direction) {
            bool isTerrainSafeToGo = false;
            bool skipVanilla = false;
            ModsManager.HookAction(
                "IsTerrainSafeToGo",
                modLoader => {
                    modLoader.IsTerrainSafeToGo(this, position, direction, out isTerrainSafeToGo, out skipVanilla);
                    return false;
                }
            );
            if (skipVanilla) {
                return isTerrainSafeToGo;
            }
            //vector是自己判断移动后的位置
            Vector3 vector = position
                + new Vector3(0f, 0.1f, 0f)
                + (direction.LengthSquared() < 1.2f
                    ? new Vector3(direction.X, 0f, direction.Z)
                    : 1.2f * Vector3.Normalize(new Vector3(direction.X, 0f, direction.Z)));
            Vector3 vector2 = position
                + new Vector3(0f, 0.1f, 0f)
                + (direction.LengthSquared() < 1f
                    ? new Vector3(direction.X, 0f, direction.Z)
                    : 1f * Vector3.Normalize(new Vector3(direction.X, 0f, direction.Z)));
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    //只有向前的向量才被计入
                    if (!(Vector3.Dot(direction, new Vector3(i, 0f, j)) > 0f)) {
                        continue;
                    }
                    //检查器位置的方块、下面一格的方块、下面两格的方块。
                    //碰到危险方块则返回不是安全方向；碰到非危险的可碰撞方块则是安全方向
                    for (int num = 0; num >= -2; num--) {
                        int cellValue = m_subsystemTerrain.Terrain.GetCellValue(
                            Terrain.ToCell(vector.X) + i,
                            Terrain.ToCell(vector.Y) + num,
                            Terrain.ToCell(vector.Z) + j
                        );
                        Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
                        if (ShouldAvoidBlock(block, cellValue)) {
                            return false;
                        }
                        if (block.GetIsCollidable(m_componentCreature.ComponentBody, cellValue)) {
                            break;
                        }
                    }
                }
            }
            bool isBlockBeneathDangerous = true;
            //num2是计算的摔落高度
            int num2 = TakeRisks ? m_maxFallHeightRisk : m_maxFallHeight;
            for (int num3 = 0; num3 >= -num2 && vector2.Y + num3 >= 0; num3--) {
                int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(
                    Terrain.ToCell(vector2.X),
                    Terrain.ToCell(vector2.Y) + num3,
                    Terrain.ToCell(vector2.Z)
                );
                Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(cellValue2)];
                if ((block2.GetIsCollidable(m_componentCreature.ComponentBody, cellValue2) || block2 is FluidBlock)
                    && !ShouldAvoidBlock(block2, cellValue2)) {
                    isBlockBeneathDangerous = false;
                    break;
                }
            }
            if (isBlockBeneathDangerous) {
                return false;
            }
            return true;
        }

        public virtual ComponentBody FindNearestBodyInFront(Vector3 position, Vector2 direction) {
            if (m_subsystemTime.GameTime >= m_nextBodiesUpdateTime) {
                m_nextBodiesUpdateTime = m_subsystemTime.GameTime + 0.5;
                m_nearbyBodies.Clear();
                m_subsystemBodies.FindBodiesAroundPoint(m_componentCreature.ComponentBody.Position.XZ, 4f, m_nearbyBodies);
            }
            ComponentBody result = null;
            float num = float.MaxValue;
            foreach (ComponentBody nearbyBody in m_nearbyBodies) {
                if (nearbyBody != m_componentCreature.ComponentBody
                    && !(MathF.Abs(nearbyBody.Position.Y - m_componentCreature.ComponentBody.Position.Y) > 1.1f)
                    && Vector2.Dot(nearbyBody.Position.XZ - position.XZ, direction) > 0f) {
                    float num2 = Vector2.DistanceSquared(nearbyBody.Position.XZ, position.XZ);
                    if (num2 < num) {
                        num = num2;
                        result = nearbyBody;
                    }
                }
            }
            return result;
        }

        public virtual Vector3 AvoidNearestBody(Vector3 position, Vector3 destination) {
            Vector2 v = destination.XZ - position.XZ;
            ComponentBody componentBody = FindNearestBodyInFront(position, Vector2.Normalize(v));
            if (componentBody != null
                && componentBody != DoNotAvoidBody) {
                float num = 0.72f * (componentBody.BoxSize.X + m_componentCreature.ComponentBody.BoxSize.X) + 0.5f;
                Vector2 xZ = componentBody.Position.XZ;
                Vector2 v2 = Segment2.NearestPoint(new Segment2(position.XZ, destination.XZ), xZ) - xZ;
                if (v2.LengthSquared() < num * num) {
                    float num2 = v.Length();
                    Vector2 v3 = Vector2.Normalize(xZ + Vector2.Normalize(v2) * num - position.XZ);
                    if (Vector2.Dot(v / num2, v3) > 0.5f) {
                        return new Vector3(position.X + v3.X * num2, destination.Y, position.Z + v3.Y * num2);
                    }
                }
            }
            return destination;
        }

        public static Vector2? CombineNullables(Vector2? v1, Vector2? v2) {
            if (!v1.HasValue) {
                return v2;
            }
            if (!v2.HasValue) {
                return v1;
            }
            return v1.Value + v2.Value;
        }

        public static Vector3? CombineNullables(Vector3? v1, Vector3? v2) {
            if (!v1.HasValue) {
                return v2;
            }
            if (!v2.HasValue) {
                return v1;
            }
            return v1.Value + v2.Value;
        }
    }
}