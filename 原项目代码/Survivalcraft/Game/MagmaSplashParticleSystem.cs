using Engine;
using Engine.Graphics;

namespace Game {
    public class MagmaSplashParticleSystem : ParticleSystem<MagmaSplashParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public Vector3 Velocity;

            public float TimeToLive;

            public float Duration;
        }

        public Random m_random = new();

        public SubsystemTerrain m_subsystemTerrain;

        public Vector3 m_position;

        public float m_time;

        public MagmaSplashParticleSystem(SubsystemTerrain terrain, Vector3 position, bool large) : base(40) {
            m_subsystemTerrain = terrain;
            m_position = position;
            Texture = ContentManager.Get<Texture2D>("Textures/MagmaSplashParticle");
            TextureSlotsCount = 2;
            int num = Terrain.ToCell(position.X);
            int num2 = Terrain.ToCell(position.Y);
            int num3 = Terrain.ToCell(position.Z);
            int x = 0;
            x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num + 1, num2, num3));
            x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num - 1, num2, num3));
            x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2 + 1, num3));
            x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2 - 1, num3));
            x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2, num3 + 1));
            x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2, num3 - 1));
            Color white = Color.White;
            float num4 = LightingManager.LightIntensityByLightValue[x];
            white *= num4;
            white.A = 255;
            float num5 = large ? 1.5f : 1f;
            for (int i = 0; i < Particles.Length; i++) {
                Particle obj = Particles[i];
                obj.IsActive = true;
                obj.Position = position;
                obj.Color = white;
                obj.Size = new Vector2(0.2f * num5);
                obj.TimeToLive = obj.Duration = m_random.Float(0.5f, 3.5f);
                Vector3 v = 4f * m_random.Float(0.1f, 1f) * Vector3.Normalize(new Vector3(m_random.Float(-1f, 1f), 0f, m_random.Float(-1f, 1f)));
                obj.Velocity = num5 * (v + new Vector3(0f, m_random.Float(0f, 4f), 0f));
            }
        }

        public override bool Simulate(float dt) {
            dt = Math.Clamp(dt, 0f, 0.1f);
            float num = MathF.Pow(0.015f, dt);
            m_time += dt;
            bool flag = false;
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (!particle.IsActive) {
                    continue;
                }
                flag = true;
                particle.Position += particle.Velocity * dt;
                particle.Velocity.Y += -10f * dt;
                particle.Velocity *= num;
                particle.Color *= MathUtils.Saturate(particle.TimeToLive);
                particle.TimeToLive -= dt;
                particle.TextureSlot = (int)(3.99f * particle.TimeToLive / particle.Duration);
                particle.FlipX = m_random.Sign() > 0;
                particle.FlipY = m_random.Sign() > 0;
                if (particle.TimeToLive <= 0f
                    || particle.Size.X <= 0f) {
                    particle.IsActive = false;
                    continue;
                }
                int cellValue = m_subsystemTerrain.Terrain.GetCellValue(
                    Terrain.ToCell(particle.Position.X),
                    Terrain.ToCell(particle.Position.Y),
                    Terrain.ToCell(particle.Position.Z)
                );
                int num2 = Terrain.ExtractContents(cellValue);
                if (num2 == 0) {
                    continue;
                }
                Block block = BlocksManager.Blocks[num2];
                if (block.IsCollidable_(cellValue)) {
                    particle.IsActive = true;
                }
                else if (block is MagmaBlock) {
                    int level = FluidBlock.GetLevel(Terrain.ExtractData(cellValue));
                    float levelHeight = ((MagmaBlock)block).GetLevelHeight(level);
                    if (particle.Position.Y <= MathF.Floor(particle.Position.Y) + levelHeight) {
                        particle.Velocity.Y = 0f;
                        float num3 = Vector2.Distance(new Vector2(particle.Position.X, particle.Position.Z), new Vector2(m_position.X, m_position.Z));
                        float num4 = 0.02f * MathF.Sin(2f * num3 + 10f * m_time);
                        particle.Position.Y = MathF.Floor(particle.Position.Y) + levelHeight + num4;
                        particle.TimeToLive -= 1f * dt;
                        particle.Size -= new Vector2(0.04f * dt);
                    }
                }
            }
            return !flag;
        }
    }
}