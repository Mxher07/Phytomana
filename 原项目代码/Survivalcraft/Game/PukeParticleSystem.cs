using Engine;
using Engine.Graphics;

namespace Game {
    public class PukeParticleSystem : ParticleSystem<PukeParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public Vector3 Velocity;

            public float TimeToLive;
        }

        public Random m_random = new();

        public SubsystemTerrain m_subsystemTerrain;

        public float m_duration;

        public float m_toGenerate;

        public Vector3 Position { get; set; }

        public Vector3 Direction { get; set; }

        public bool IsStopped { get; set; }

        public PukeParticleSystem(SubsystemTerrain terrain) : base(80) {
            m_subsystemTerrain = terrain;
            Texture = ContentManager.Get<Texture2D>("Textures/PukeParticle");
            TextureSlotsCount = 3;
        }

        public override bool Simulate(float dt) {
            int num = Terrain.ToCell(Position.X);
            int num2 = Terrain.ToCell(Position.Y);
            int num3 = Terrain.ToCell(Position.Z);
            int x = 0;
            x = MathUtils.Max(x, m_subsystemTerrain.Terrain.GetCellLight(num + 1, num2, num3));
            x = MathUtils.Max(x, m_subsystemTerrain.Terrain.GetCellLight(num - 1, num2, num3));
            x = MathUtils.Max(x, m_subsystemTerrain.Terrain.GetCellLight(num, num2 + 1, num3));
            x = MathUtils.Max(x, m_subsystemTerrain.Terrain.GetCellLight(num, num2 - 1, num3));
            x = MathUtils.Max(x, m_subsystemTerrain.Terrain.GetCellLight(num, num2, num3 + 1));
            x = MathUtils.Max(x, m_subsystemTerrain.Terrain.GetCellLight(num, num2, num3 - 1));
            Color white = Color.White;
            float num4 = LightingManager.LightIntensityByLightValue[x];
            white *= num4;
            white.A = 255;
            dt = Math.Clamp(dt, 0f, 0.1f);
            float num5 = MathF.Pow(0.03f, dt);
            m_duration += dt;
            if (m_duration > 3.5f) {
                IsStopped = true;
            }
            float num6 = MathUtils.Saturate(1.3f * SimplexNoise.Noise(3f * m_duration + GetHashCode() % 100) - 0.3f);
            float num7 = 30f * num6;
            m_toGenerate += num7 * dt;
            bool flag = false;
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (particle.IsActive) {
                    flag = true;
                    particle.TimeToLive -= dt;
                    if (particle.TimeToLive > 0f) {
                        Vector3 position = particle.Position;
                        Vector3 vector = position + particle.Velocity * dt;
                        TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(
                            position,
                            vector,
                            false,
                            true,
                            (value, _) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable_(value)
                        );
                        if (terrainRaycastResult.HasValue) {
                            Plane plane = terrainRaycastResult.Value.CellFace.CalculatePlane();
                            vector = position;
                            if (plane.Normal.X != 0f) {
                                particle.Velocity *= new Vector3(-0.05f, 0.05f, 0.05f);
                            }
                            if (plane.Normal.Y != 0f) {
                                particle.Velocity *= new Vector3(0.05f, -0.05f, 0.05f);
                            }
                            if (plane.Normal.Z != 0f) {
                                particle.Velocity *= new Vector3(0.05f, 0.05f, -0.05f);
                            }
                        }
                        particle.Position = vector;
                        particle.Velocity.Y += -9.81f * dt;
                        particle.Velocity *= num5;
                        particle.Color *= MathUtils.Saturate(particle.TimeToLive);
                        particle.TextureSlot = (int)(8.99f * MathUtils.Saturate(3f - particle.TimeToLive));
                    }
                    else {
                        particle.IsActive = false;
                    }
                }
                else if (!IsStopped
                    && m_toGenerate >= 1f) {
                    Vector3 v = m_random.Vector3(0f, 1f);
                    particle.IsActive = true;
                    particle.Position = Position + 0.05f * v;
                    particle.Color = Color.MultiplyColorOnly(white, m_random.Float(0.7f, 1f));
                    particle.Velocity = MathUtils.Lerp(1f, 2.5f, num6) * Vector3.Normalize(Direction + 0.25f * v);
                    particle.TimeToLive = 3f;
                    particle.Size = new Vector2(0.1f);
                    particle.FlipX = m_random.Bool();
                    particle.FlipY = m_random.Bool();
                    m_toGenerate -= 1f;
                }
            }
            if (IsStopped) {
                return !flag;
            }
            return false;
        }
    }
}