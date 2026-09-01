using Engine;
using Engine.Graphics;

namespace Game {
    public class FireworksTrailParticleSystem : ParticleSystem<FireworksTrailParticleSystem.Particle>, ITrailParticleSystem {
        public class Particle : Game.Particle {
            public float Time;

            public float Duration;
        }

        public Random m_random = new();

        public float m_toGenerate;

        public Vector3? m_lastPosition;

        public Vector3 Position { get; set; }

        public bool IsStopped { get; set; }

        public FireworksTrailParticleSystem() : base(60) {
            Texture = ContentManager.Get<Texture2D>("Textures/FireParticle");
            TextureSlotsCount = 3;
        }

        public override bool Simulate(float dt) {
            float num = 120f;
            m_toGenerate += num * dt;
            if (!m_lastPosition.HasValue) {
                m_lastPosition = Position;
            }
            bool flag = false;
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (particle.IsActive) {
                    flag = true;
                    particle.Time += dt;
                    if (particle.Time <= particle.Duration) {
                        particle.TextureSlot = (int)MathUtils.Min(9f * particle.Time / particle.Duration, 8f);
                    }
                    else {
                        particle.IsActive = false;
                    }
                }
                else if (!IsStopped
                    && m_toGenerate >= 1f) {
                    particle.IsActive = true;
                    particle.Position = Vector3.Lerp(m_lastPosition.Value, Position, m_random.Float(0f, 1f));
                    particle.Color = Color.White;
                    particle.Time = m_random.Float(0f, 0.75f);
                    particle.Size = new Vector2(m_random.Float(0.12f, 0.16f));
                    particle.Duration = 1f;
                    particle.FlipX = m_random.Bool();
                    particle.FlipY = m_random.Bool();
                    m_toGenerate -= 1f;
                }
            }
            m_toGenerate = MathUtils.Remainder(m_toGenerate, 1f);
            m_lastPosition = Position;
            if (IsStopped) {
                return !flag;
            }
            return false;
        }
    }
}