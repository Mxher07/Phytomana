using Engine;
using Engine.Graphics;

namespace Game {
    public class FireParticleSystem : ParticleSystem<FireParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public float Time;

            public float TimeToLive;

            public float Speed;
        }

        public Random m_random = new();

        public Vector3 m_position;

        public float m_size;

        public float m_toGenerate;

        public bool m_visible;

        public float m_maxVisibilityDistance;

        public float m_age;

        public bool IsStopped { get; set; }

        public FireParticleSystem(Vector3 position, float size, float maxVisibilityDistance) : base(10) {
            m_position = position;
            m_size = size;
            m_maxVisibilityDistance = maxVisibilityDistance;
            Texture = ContentManager.Get<Texture2D>("Textures/FireParticle");
            TextureSlotsCount = 3;
        }

        public override bool Simulate(float dt) {
            m_age += dt;
            bool flag = false;
            if (m_visible || m_age < 2f) {
                m_toGenerate += IsStopped ? 0f : 5f * dt;
                for (int i = 0; i < Particles.Length; i++) {
                    Particle particle = Particles[i];
                    if (particle.IsActive) {
                        flag = true;
                        particle.Time += dt;
                        particle.TimeToLive -= dt;
                        if (particle.TimeToLive > 0f) {
                            particle.Position.Y += particle.Speed * dt;
                            particle.TextureSlot = (int)MathUtils.Min(9f * particle.Time / 1.25f, 8f);
                        }
                        else {
                            particle.IsActive = false;
                        }
                    }
                    else if (m_toGenerate >= 1f) {
                        particle.IsActive = true;
                        particle.Position = m_position + 0.25f * m_size * new Vector3(m_random.Float(-1f, 1f), 0f, m_random.Float(-1f, 1f));
                        particle.Color = Color.White;
                        particle.Size = new Vector2(m_size);
                        particle.Speed = m_random.Float(0.45f, 0.55f) * m_size / 0.15f;
                        particle.Time = 0f;
                        particle.TimeToLive = m_random.Float(0.5f, 2f);
                        particle.FlipX = m_random.Int(0, 1) == 0;
                        particle.FlipY = m_random.Int(0, 1) == 0;
                        m_toGenerate -= 1f;
                    }
                }
                m_toGenerate = MathUtils.Remainder(m_toGenerate, 1f);
            }
            m_visible = false;
            if (IsStopped) {
                return !flag;
            }
            return false;
        }

        public override void Draw(Camera camera) {
            float num = Vector3.Dot(m_position - camera.ViewPosition, camera.ViewDirection);
            if (num > -0.5f
                && num <= m_maxVisibilityDistance
                && Vector3.DistanceSquared(m_position, camera.ViewPosition) <= m_maxVisibilityDistance * m_maxVisibilityDistance) {
                m_visible = true;
                base.Draw(camera);
            }
        }
    }
}