using Engine;
using Engine.Graphics;

namespace Game {
    public class SnowSplashParticleSystem : ParticleSystem<SnowSplashParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public float TimeToLive;

            public Color BaseColor;

            public float FadeFactor;
        }

        public Random m_random = new();

        public bool m_isActive;

        public SnowSplashParticleSystem() : base(100) {
            Texture = ContentManager.Get<Texture2D>("Textures/SnowParticle");
            TextureSlotsCount = 4;
        }

        public void AddSplash(int value, Vector3 position, Vector2 size, Color color, int textureSlot) {
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (!particle.IsActive) {
                    Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
                    particle.IsActive = true;
                    particle.Position = position;
                    particle.BaseColor = color;
                    particle.BillboardingMode = ParticleBillboardingMode.Horizontal;
                    particle.Size = size;
                    particle.TextureSlot = textureSlot;
                    if (block is WaterBlock) {
                        ((WaterBlock)block).GetLevelHeight(FluidBlock.GetLevel(Terrain.ExtractData(value)));
                        particle.TimeToLive = m_random.Float(0.2f, 0.3f);
                        particle.FadeFactor = 1f;
                    }
                    else if (block.IsCollidable_(value)
                        || block is SnowBlock) {
                        particle.TimeToLive = m_random.Float(0.8f, 1.2f);
                        particle.FadeFactor = 1f;
                    }
                    break;
                }
            }
            m_isActive = true;
        }

        public override bool Simulate(float dt) {
            if (m_isActive) {
                dt = Math.Clamp(dt, 0f, 0.1f);
                bool flag = false;
                for (int i = 0; i < Particles.Length; i++) {
                    Particle particle = Particles[i];
                    if (particle.IsActive) {
                        particle.Color = particle.BaseColor * MathUtils.Saturate(particle.FadeFactor * particle.TimeToLive);
                        particle.TimeToLive -= dt;
                        if (particle.TimeToLive <= 0f) {
                            particle.IsActive = false;
                        }
                        else {
                            flag = true;
                        }
                    }
                }
                if (!flag) {
                    m_isActive = false;
                }
            }
            return false;
        }
    }
}