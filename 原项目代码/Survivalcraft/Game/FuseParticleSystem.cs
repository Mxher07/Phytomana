using Engine;
using Engine.Graphics;

namespace Game {
    public class FuseParticleSystem : ParticleSystem<FuseParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public float Time;

            public float TimeToLive;

            public float Speed;

            public float TargetSpeed;
        }

        public Random m_random = new();

        public Vector3 m_position;

        public float m_toGenerate;

        public bool m_visible;

        public FuseParticleSystem(Vector3 position) : base(15) {
            m_position = position;
            Texture = ContentManager.Get<Texture2D>("Textures/FireParticle");
            TextureSlotsCount = 3;
        }

        public override bool Simulate(float dt) {
            if (m_visible) {
                m_toGenerate += 15f * dt;
                for (int i = 0; i < Particles.Length; i++) {
                    Particle particle = Particles[i];
                    if (particle.IsActive) {
                        particle.Time += dt;
                        particle.TimeToLive -= dt;
                        if (particle.TimeToLive > 0f) {
                            particle.Position.Y += particle.Speed * dt;
                            particle.Speed = MathUtils.Max(particle.Speed - 1.5f * dt, particle.TargetSpeed);
                            particle.TextureSlot = (int)MathUtils.Min(9f * particle.Time / 0.75f, 8f);
                            particle.Size = new Vector2(0.07f * (1f + 2f * particle.Time));
                        }
                        else {
                            particle.IsActive = false;
                        }
                    }
                    else if (m_toGenerate >= 1f) {
                        particle.IsActive = true;
                        particle.Position = m_position + 0.02f * new Vector3(0f, m_random.Float(-1f, 1f), 0f);
                        particle.Color = Color.White;
                        particle.TargetSpeed = m_random.Float(0.45f, 0.55f) * 0.4f;
                        particle.Speed = m_random.Float(0.45f, 0.55f) * 2.5f;
                        particle.Time = 0f;
                        particle.Size = Vector2.Zero;
                        particle.TimeToLive = m_random.Float(0.3f, 1f);
                        particle.FlipX = m_random.Int(0, 1) == 0;
                        particle.FlipY = m_random.Int(0, 1) == 0;
                        m_toGenerate -= 1f;
                    }
                }
                m_toGenerate = MathUtils.Remainder(m_toGenerate, 1f);
            }
            m_visible = false;
            return false;
        }

        public override void Draw(Camera camera) {
            float num = Vector3.Dot(m_position - camera.ViewPosition, camera.ViewDirection);
            if (num > -0.5f
                && num <= 48f
                && Vector3.DistanceSquared(m_position, camera.ViewPosition) <= 1024f) {
                m_visible = true;
                base.Draw(camera);
            }
        }
    }
}