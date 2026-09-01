using Engine;
using Engine.Graphics;

namespace Game {
    public class LeavesParticleSystem : ParticleSystem<LeavesParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public float Time;

            public float EndTime;

            public float Speed;

            public float Phase;

            public float PhaseSpeed;

            public Color BaseColor;

            public int Light;

            public float Angle;

            public float AngleSpeed;
        }

        public SubsystemTerrain m_subsystemTerrain;

        Random m_random = new();

        Point3 m_point;

        bool m_fadeIn;

        bool m_createFallenLeaves;

        public LeavesParticleSystem(SubsystemTerrain subsystemTerrain, Point3 point, int leavesCount, bool fadeIn, bool createFallenLeaves, int value)
            : base(leavesCount) {
            m_subsystemTerrain = subsystemTerrain;
            m_point = point;
            m_fadeIn = fadeIn;
            m_createFallenLeaves = createFallenLeaves;
            Texture = ContentManager.Get<Texture2D>("Textures/LeafParticle");
            TextureSlotsCount = 1;
            Color color = BlocksManager.Blocks[Terrain.ExtractContents(value)] is LeavesBlock leavesBlock
                ? leavesBlock.GetLeavesBlockColor(value, m_subsystemTerrain.Terrain, point.X, point.Y, point.Z)
                : Color.Transparent;
            for (int i = 0; i < Particles.Length; i++) {
                float f = m_random.Float();
                Color color2 = Color.Lerp(new Color(180, 120, 120), new Color(200, 255, 255), m_random.Float(0f, 1f));
                Particle particle = Particles[i];
                particle.IsActive = true;
                particle.EndTime = 12f;
                particle.Position = new Vector3(point)
                    + new Vector3(0.5f)
                    + 0.45f * new Vector3(m_random.Float(-1f, 1f), MathUtils.Lerp(1f, -1f, f), m_random.Float(-1f, 1f));
                particle.Light = 7;
                particle.BaseColor = color * color2;
                particle.Color = Color.Transparent;
                particle.BillboardingMode = ParticleBillboardingMode.None;
                particle.Size = new Vector2(0.18f) * m_random.Float(0.75f, 1f);
                particle.Speed = MathUtils.Lerp(1.5f, 3.5f, f);
                particle.Phase = m_random.Float(0f, (float)Math.PI * 2f);
                particle.PhaseSpeed = 2f * particle.Speed * m_random.Float(0.75f, 1.25f);
                particle.Angle = m_random.Float(0f, (float)Math.PI * 2f);
                particle.AngleSpeed = m_random.Sign() * m_random.Float(1f, 3f);
                particle.FlipX = m_random.Bool();
                particle.FlipY = m_random.Bool();
            }
        }

        public override bool Simulate(float dt) {
            Terrain terrain = m_subsystemTerrain.Terrain;
            bool flag = false;
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (!particle.IsActive) {
                    continue;
                }
                if (particle.BillboardingMode == ParticleBillboardingMode.None) {
                    particle.Phase += particle.PhaseSpeed * dt;
                    particle.Rotation = -0.5f * MathF.Sin(particle.Phase) + (float)Math.PI / 2f;
                    particle.Angle += particle.AngleSpeed * dt;
                    Vector2 v = Vector2.Rotate(Vector2.UnitX, particle.Rotation);
                    Vector2 vector = Vector2.Perpendicular(v);
                    Vector2 vector2 = Vector2.Rotate(Vector2.UnitY, particle.Angle);
                    particle.Right = particle.Size.X * new Vector3(v.X * vector2.X, v.Y, v.X * vector2.Y);
                    particle.Up = particle.Size.Y * new Vector3(vector.X * vector2.X, vector.Y, vector.X * vector2.Y);
                    float num = MathUtils.Saturate(4f * particle.Time);
                    particle.Position += 0.8f * particle.Speed * num * dt * new Vector3(0f, -1f, 0f);
                    particle.Position += 0.4f * particle.Speed * num * dt * MathF.Cos(particle.Phase) * new Vector3(vector2.X, 0f, vector2.Y);
                    particle.Position += 0.3f * particle.Speed * num * dt * MathF.Cos(2f * particle.Phase) * new Vector3(0f, -1f, 0f);
                }
                int num2 = Terrain.ToCell(particle.Position.X);
                int num3 = Terrain.ToCell(particle.Position.Y);
                int num4 = Terrain.ToCell(particle.Position.Z);
                TerrainChunk chunkAtCell = terrain.GetChunkAtCell(num2, num4);
                if (chunkAtCell != null
                    && chunkAtCell.State >= TerrainChunkState.InvalidVertices1) {
                    particle.Light = terrain.GetCellLight(num2, num3, num4);
                }
                particle.Color = Color.MultiplyColorOnlyNotSaturated(particle.BaseColor, LightingManager.LightIntensityByLightValue[particle.Light]);
                float num5 = MathUtils.Saturate(0.5f * (particle.EndTime - particle.Time));
                if (m_fadeIn) {
                    num5 *= MathUtils.Saturate(1f * particle.Time);
                }
                particle.Color *= num5;
                if (particle.BillboardingMode == ParticleBillboardingMode.None) {
                    int cellValue = terrain.GetCellValue(num2, num3, num4);
                    int num6 = Terrain.ExtractContents(cellValue);
                    Block block = BlocksManager.Blocks[num6];
                    if (block is WaterBlock) {
                        particle.EndTime = particle.Time;
                    }
                    else if (block.IsCollidable
                        && !(block is LeavesBlock)) {
                        float num7 = 0.5f;
                        Ray3 ray = new(particle.Position - new Vector3(num2, num3, num4) + new Vector3(0f, num7, 0f), -Vector3.UnitY);
                        float? num8 = block.Raycast(ray, m_subsystemTerrain, cellValue, false, out int _, out BoundingBox _);
                        if (num8.HasValue
                            && num8 < num7 - 0f) {
                            particle.BillboardingMode = ParticleBillboardingMode.Horizontal;
                            particle.Position = ray.Sample(num8.Value) + new Vector3(num2, num3 + 0.03f, num4);
                            particle.EndTime = particle.Time + 2f;
                            if (m_createFallenLeaves) {
                                m_createFallenLeaves = false;
                                m_subsystemTerrain.Project.FindSubsystem<SubsystemDeciduousLeavesBlockBehavior>(true)
                                    .CreateFallenLeaves(m_point, true);
                            }
                        }
                    }
                }
                particle.Time += dt;
                if (particle.Time >= particle.EndTime) {
                    particle.IsActive = false;
                }
                else {
                    flag = true;
                }
            }
            return !flag;
        }
    }
}