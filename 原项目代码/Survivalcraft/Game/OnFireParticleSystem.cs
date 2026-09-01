using Engine;
using Engine.Graphics;

namespace Game {
    public class OnFireParticleSystem : ParticleSystem<OnFireParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public float Time;

            public float Duration;

            public Vector3 Velocity;
        }

        public Random m_random = new();

        public float m_toGenerate;

        public bool m_visible;

        public bool IsStopped { get; set; }

        public Vector3 Position { get; set; }

        public float Radius { get; set; }

        public OnFireParticleSystem() : base(25) {
            Texture = ContentManager.Get<Texture2D>("Textures/FireParticle");
            TextureSlotsCount = 3;
        }

        public override bool Simulate(float dt) {
            bool flag = false;
            if (m_visible) {
                m_toGenerate += 20f * dt;
                float num = MathF.Pow(0.02f, dt);
                for (int i = 0; i < Particles.Length; i++) {
                    Particle particle = Particles[i];
                    if (particle.IsActive) {
                        flag = true;
                        particle.Time += dt;
                        if (particle.Time <= particle.Duration) {
                            particle.Position += particle.Velocity * dt;
                            particle.Velocity *= num;
                            particle.Velocity.Y += 10f * dt;
                            particle.TextureSlot = (int)MathUtils.Min(9f * particle.Time / particle.Duration * 1.2f, 8f);
                        }
                        else {
                            particle.IsActive = false;
                        }
                    }
                    else if (!IsStopped) {
                        if (m_toGenerate >= 1f) {
                            particle.IsActive = true;
                            Vector3 v = new(m_random.Float(-1f, 1f), m_random.Float(0f, 1f), m_random.Float(-1f, 1f));
                            particle.Position = Position + 0.75f * Radius * v;
                            particle.Color = Color.White;
                            particle.Velocity = 1.5f * v;
                            particle.Size = new Vector2(0.5f);
                            particle.Time = 0f;
                            particle.Duration = m_random.Float(0.5f, 1.5f);
                            particle.FlipX = m_random.Bool();
                            particle.FlipY = m_random.Bool();
                            m_toGenerate -= 1f;
                        }
                    }
                    else {
                        m_toGenerate = 0f;
                    }
                }
                m_toGenerate = MathUtils.Remainder(m_toGenerate, 1f);
                m_visible = false;
            }
            if (IsStopped && !flag) {
                return true;
            }
            return false;
        }

        public override void Draw(Camera camera) {
            float num = Vector3.Dot(Position - camera.ViewPosition, camera.ViewDirection);
            if (num > -5f
                && num <= 48f) {
                m_visible = true;
                base.Draw(camera);
            }
        }
    }
}