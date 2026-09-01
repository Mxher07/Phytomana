using Engine;
using Engine.Graphics;

namespace Game {
    public class RainSplashParticleSystem : ParticleSystem<RainSplashParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public Vector3 Velocity;

            public float Duration;

            public float TimeToLive;

            public Color BaseColor;

            public float BaseSize1;

            public float BaseSize2;

            public float Gravity;

            public float FadeFactor;
        }

        public Random m_random = new();

        public bool m_isActive;

        public RainSplashParticleSystem() : base(150) {
            Texture = ContentManager.Get<Texture2D>("Textures/RainSplashParticle");
            TextureSlotsCount = 1;
        }

        public void AddSplash(int value, Vector3 position, Color color) {
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (!particle.IsActive) {
                    Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
                    particle.IsActive = true;
                    particle.Position = position;
                    particle.BaseColor = color;
                    if (block is WaterBlock) {
                        particle.Position.Y += 0.05f;
                        particle.BaseSize1 = 0.02f;
                        particle.BaseSize2 = 0.09f;
                        particle.Duration = particle.TimeToLive = m_random.Float(0.3f, 0.5f);
                        particle.Velocity = Vector3.Zero;
                        particle.Gravity = 0f;
                        particle.BillboardingMode = ParticleBillboardingMode.Horizontal;
                        particle.FadeFactor = 1.6f;
                    }
                    else if (block.IsCollidable_(value)) {
                        particle.BaseSize1 = 0.03f;
                        particle.BaseSize2 = 0.08f;
                        particle.Duration = particle.TimeToLive = m_random.Float(0.25f, 0.3f);
                        particle.Velocity = m_random.Float(0.7f, 0.9f) * Vector3.UnitY;
                        particle.Gravity = -10f;
                        particle.BillboardingMode = ParticleBillboardingMode.Camera;
                        particle.FadeFactor = 2.8f;
                    }
                    else if (m_random.Bool(0.33f)) {
                        particle.BaseSize1 = m_random.Float(0.015f, 0.025f);
                        particle.BaseSize2 = particle.BaseSize1;
                        particle.Duration = particle.TimeToLive = m_random.Float(0.25f, 0.3f);
                        particle.Velocity = m_random.Vector3(0f, 1.5f) * new Vector3(1f, 0f, 1f);
                        particle.Gravity = -10f;
                        particle.BillboardingMode = ParticleBillboardingMode.Camera;
                        particle.FadeFactor = 2.8f;
                    }
                    break;
                }
            }
            m_isActive = true;
        }

        public override bool Simulate(float dt) {
            if (m_isActive) {
                dt = Math.Clamp(dt, 0f, 0.1f);
                float num = MathF.Pow(0.0005f, dt);
                bool flag = false;
                for (int i = 0; i < Particles.Length; i++) {
                    Particle particle = Particles[i];
                    if (particle.IsActive) {
                        particle.Position += particle.Velocity * dt;
                        particle.Velocity.Y += particle.Gravity * dt;
                        particle.Velocity *= num;
                        particle.Size = new Vector2(
                            MathUtils.Lerp(particle.BaseSize1, particle.BaseSize2, (particle.Duration - particle.TimeToLive) / particle.Duration)
                        );
                        particle.Color = particle.BaseColor * MathUtils.Saturate(particle.FadeFactor * particle.TimeToLive);
                        particle.TimeToLive -= dt;
                        particle.FlipX = m_random.Bool();
                        particle.FlipY = m_random.Bool();
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