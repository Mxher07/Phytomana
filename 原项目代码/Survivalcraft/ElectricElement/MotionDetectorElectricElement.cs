using Engine;

namespace Game {
    public class MotionDetectorElectricElement : MountedElectricElement {
        public const float m_range = 8f;

        public const float m_speedThreshold = 0.25f;

        public const float m_pollingPeriod = 0.25f;

        public SubsystemBodies m_subsystemBodies;

        public SubsystemMovingBlocks m_subsystemMovingBlocks;

        public SubsystemProjectiles m_subsystemProjectiles;

        public SubsystemPickables m_subsystemPickables;

        public float m_voltage;

        public Vector3 m_center;

        public Vector3 m_direction;

        public Vector2 m_corner1;

        public Vector2 m_corner2;

        public DynamicArray<ComponentBody> m_bodies = [];

        public MotionDetectorElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) {
            m_subsystemBodies = subsystemElectricity.Project.FindSubsystem<SubsystemBodies>(true);
            m_subsystemMovingBlocks = subsystemElectricity.Project.FindSubsystem<SubsystemMovingBlocks>(true);
            m_subsystemProjectiles = subsystemElectricity.Project.FindSubsystem<SubsystemProjectiles>(true);
            m_subsystemPickables = subsystemElectricity.Project.FindSubsystem<SubsystemPickables>(true);
            m_center = new Vector3(cellFace.X, cellFace.Y, cellFace.Z) + new Vector3(0.5f) - 0.25f * m_direction;
            m_direction = CellFace.FaceToVector3(cellFace.Face);
            Vector3 vector = Vector3.One - new Vector3(MathF.Abs(m_direction.X), MathF.Abs(m_direction.Y), MathF.Abs(m_direction.Z));
            Vector3 vector2 = m_center - 8f * vector;
            Vector3 vector3 = m_center + 8f * (vector + m_direction);
            m_corner1 = new Vector2(vector2.X, vector2.Z);
            m_corner2 = new Vector2(vector3.X, vector3.Z);
        }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            m_voltage = CalculateMotionVoltage();
            if (m_voltage > 0f
                && voltage == 0f) {
                SubsystemElectricity.SubsystemAudio.PlaySound("Audio/MotionDetectorClick", 1f, 0f, m_center, 1f, true);
            }
            float num = 0.25f * (0.9f + 0.000200000009f * (GetHashCode() % 1000));
            SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + MathUtils.Max((int)(num / 0.01f), 1));
            return m_voltage != voltage;
        }

        public float CalculateMotionVoltage() {
            float num = 0f;
            m_bodies.Clear();
            m_subsystemBodies.FindBodiesInArea(m_corner1, m_corner2, m_bodies);
            for (int i = 0; i < m_bodies.Count; i++) {
                ComponentBody componentBody = m_bodies.Array[i];
                if (!(componentBody.Velocity.LengthSquared() < 0.0625f)) {
                    num = MathUtils.Max(num, TestPoint(componentBody.Position + new Vector3(0f, 0.5f * componentBody.BoxSize.Y, 0f)));
                }
            }
            foreach (IMovingBlockSet movingBlockSet in m_subsystemMovingBlocks.MovingBlockSetEnumerable) {
                if (movingBlockSet.CurrentVelocity.LengthSquared() < 0.0625f
                    || BoundingBox.Distance(movingBlockSet.BoundingBox(false), m_center) > 8f) {
                    continue;
                }
                foreach (MovingBlock block in movingBlockSet.Blocks) {
                    num = MathUtils.Max(num, TestPoint(movingBlockSet.Position + new Vector3(block.Offset) + new Vector3(0.5f)));
                }
            }
            foreach (Projectile projectile in m_subsystemProjectiles.Projectiles) {
                if (!(projectile.Velocity.LengthSquared() < 0.0625f)) {
                    num = MathUtils.Max(num, TestPoint(projectile.Position));
                }
            }
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (!(pickable.Velocity.LengthSquared() < 0.0625f)) {
                    num = MathUtils.Max(num, TestPoint(pickable.Position));
                }
            }
            if (!(num > 0f)) {
                return 0f;
            }
            return MathUtils.Lerp(0.51f, 1f, MathUtils.Saturate(num * 1.1f));
        }

        public float TestPoint(Vector3 p) {
            float num = Vector3.DistanceSquared(p, m_center);
            if (num < 64f
                && Vector3.Dot(Vector3.Normalize(p - (m_center - 0.75f * m_direction)), m_direction) > 0.5f
                && !SubsystemElectricity.SubsystemTerrain.Raycast(
                        m_center,
                        p,
                        false,
                        true,
                        delegate(int value, float _) {
                            Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
                            return block.IsCollidable_(value)
                                && block.BlockIndex != 15
                                && block.BlockIndex != 60
                                && block.BlockIndex != 44
                                && block.BlockIndex != 18;
                        }
                    )
                    .HasValue) {
                return MathUtils.Saturate(1f - MathF.Sqrt(num) / 8f);
            }
            return 0f;
        }
    }
}