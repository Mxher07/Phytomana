using Engine;
using Engine.Graphics;

namespace Game {
    public class WhalePlumeParticleSystem : ParticleSystem<WhalePlumeParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public Vector3 Velocity;

            public float Time;

            public float Duration;
        }

        public Random m_random = new();

        public float m_time;

        public float m_duration;

        public float m_size;

        public float m_toGenerate;

        public bool IsStopped { get; set; }

        public Vector3 Position { get; set; }

        // ReSharper disable UnusedParameter.Local
        public WhalePlumeParticleSystem(SubsystemTerrain terrain, float size, float duration)
            // ReSharper restore UnusedParameter.Local
            : base(100) {
            Texture = ContentManager.Get<Texture2D>("Textures/WaterSplashParticle");
            TextureSlotsCount = 2;
            m_size = size;
            m_duration = duration;
        }

        public override bool Simulate(float dt) {
            m_time += dt;
            if (m_time < m_duration
                && !IsStopped) {
                m_toGenerate += 60f * dt;
            }
            else {
                m_toGenerate = 0f;
            }
            float num = MathF.Pow(0.001f, dt);
            float num2 = MathUtils.Lerp(4f, 10f, MathUtils.Saturate(2f * m_time / m_duration));
            Vector3 v = new(0f, 1f, 2f);
            bool flag = false;
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (particle.IsActive) {
                    flag = true;
                    particle.Time += dt;
                    if (particle.Time <= particle.Duration) {
                        particle.Position += particle.Velocity * dt;
                        particle.Velocity *= num;
                        particle.Velocity += v * dt;
                        particle.TextureSlot = (int)MathUtils.Min(4f * particle.Time / particle.Duration * 1.2f, 3f);
                        particle.Size = new Vector2(m_size) * MathUtils.Lerp(0.1f, 0.2f, particle.Time / particle.Duration);
                    }
                    else {
                        particle.IsActive = false;
                    }
                }
                else if (m_toGenerate >= 1f) {
                    particle.IsActive = true;
                    Vector3 v2 = 0.1f * m_size * new Vector3(m_random.Float(-1f, 1f), m_random.Float(0f, 2f), m_random.Float(-1f, 1f));
                    particle.Position = Position + v2;
                    particle.Color = new Color(200, 220, 210);
                    particle.Velocity = 1f * m_size * new Vector3(m_random.Float(-1f, 1f), num2 * m_random.Float(0.3f, 1f), m_random.Float(-1f, 1f));
                    particle.Size = Vector2.Zero;
                    particle.Time = 0f;
                    particle.Duration = m_random.Float(1f, 3f);
                    particle.FlipX = m_random.Bool();
                    particle.FlipY = m_random.Bool();
                    m_toGenerate -= 1f;
                }
            }
            m_toGenerate = MathUtils.Remainder(m_toGenerate, 1f);
            if (!flag
                && (m_time >= m_duration || IsStopped)) {
                return true;
            }
            return false;
        }
    }
}