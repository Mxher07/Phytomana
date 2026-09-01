using Engine;
using Engine.Graphics;

namespace Game {
    public class SmokeTrailParticleSystem : ParticleSystem<SmokeTrailParticleSystem.Particle>, ITrailParticleSystem {
        public class Particle : Game.Particle {
            public float Time;

            public float Duration;

            public Vector3 Velocity;
        }

        public Random m_random = new();

        public float m_toGenerate;

        public float m_textureSlotMultiplier;

        public float m_textureSlotOffset;

        public float m_duration;

        public float m_size;

        public float m_maxDuration;

        public Color m_color;

        public Vector3 Position { get; set; }

        public bool IsStopped { get; set; }

        public SmokeTrailParticleSystem(int particlesCount, float size, float maxDuration, Color color) : base(particlesCount) {
            m_size = size;
            m_maxDuration = maxDuration;
            Texture = ContentManager.Get<Texture2D>("Textures/FireParticle");
            TextureSlotsCount = 3;
            m_textureSlotMultiplier = m_random.Float(1.1f, 1.9f);
            m_textureSlotOffset = m_random.Float(0f, 1f) < 0.33f ? 3 : 0;
            m_color = color;
        }

        public override bool Simulate(float dt) {
            m_duration += dt;
            if (m_duration > m_maxDuration) {
                IsStopped = true;
            }
            float num = Math.Clamp(50f / m_size, 10f, 40f);
            m_toGenerate += num * dt;
            float num2 = MathF.Pow(0.1f, dt);
            bool flag = false;
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (particle.IsActive) {
                    flag = true;
                    particle.Time += dt;
                    if (particle.Time <= particle.Duration) {
                        particle.Position += particle.Velocity * dt;
                        particle.Velocity *= num2;
                        particle.Velocity.Y += 10f * dt;
                        particle.TextureSlot = (int)MathUtils.Min(
                            9f * particle.Time / particle.Duration * m_textureSlotMultiplier + m_textureSlotOffset,
                            8f
                        );
                        particle.Size = new Vector2(m_size * (0.15f + 0.8f * particle.Time / particle.Duration));
                    }
                    else {
                        particle.IsActive = false;
                    }
                }
                else if (!IsStopped
                    && m_toGenerate >= 1f) {
                    particle.IsActive = true;
                    Vector3 v = new(m_random.Float(-1f, 1f), m_random.Float(-1f, 1f), m_random.Float(-1f, 1f));
                    particle.Position = Position + 0.025f * v;
                    particle.Color = m_color;
                    particle.Velocity = 0.2f * v;
                    particle.Time = 0f;
                    particle.Size = new Vector2(0.15f * m_size);
                    particle.Duration = Particles.Length / num * m_random.Float(0.8f, 1.05f);
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