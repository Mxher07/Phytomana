using Engine;
using Engine.Graphics;

namespace Game {
    public class KillParticleSystem : ParticleSystem<KillParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public Vector3 Velocity;

            public float TimeToLive;
        }

        public Random m_random = new();

        public KillParticleSystem(SubsystemTerrain terrain, Vector3 position, float size) : base(20) {
            Texture = ContentManager.Get<Texture2D>("Textures/KillParticle");
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
            TextureSlotsCount = 2;
            Color white = Color.White;
            float num4 = LightingManager.LightIntensityByLightValue[x];
            white *= num4;
            white.A = 255;
            for (int i = 0; i < Particles.Length; i++) {
                Particle obj = Particles[i];
                obj.IsActive = true;
                obj.Position = position + 0.4f * size * new Vector3(m_random.Float(-1f, 1f), m_random.Float(-1f, 1f), m_random.Float(-1f, 1f));
                obj.Color = white;
                obj.Size = new Vector2(0.3f * size);
                obj.TimeToLive = m_random.Float(0.5f, 3.5f);
                obj.Velocity = 1.2f * size * new Vector3(m_random.Float(-1f, 1f), m_random.Float(-1f, 1f), m_random.Float(-1f, 1f));
                obj.FlipX = m_random.Bool();
                obj.FlipY = m_random.Bool();
            }
        }

        public override bool Simulate(float dt) {
            dt = Math.Clamp(dt, 0f, 0.1f);
            float num = MathF.Pow(0.1f, dt);
            bool flag = false;
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (particle.IsActive) {
                    flag = true;
                    particle.TimeToLive -= dt;
                    if (particle.TimeToLive > 0f) {
                        particle.Position += particle.Velocity * dt;
                        particle.Velocity.Y += 1f * dt;
                        particle.Velocity *= num;
                        particle.TextureSlot = (int)(3.99f * MathUtils.Saturate(2f - particle.TimeToLive));
                    }
                    else {
                        particle.IsActive = false;
                    }
                }
            }
            return !flag;
        }
    }
}