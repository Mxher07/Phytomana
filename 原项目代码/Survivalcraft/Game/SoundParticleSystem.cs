using Engine;
using Engine.Graphics;

namespace Game {
    public class SoundParticleSystem : ParticleSystem<SoundParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public float TimeToLive;

            public Vector3 Velocity;

            public Color BaseColor;
        }

        public Random m_random = new();

        public Vector3 m_position;

        public Vector3 m_direction;

        // ReSharper disable UnusedParameter.Local
        public SoundParticleSystem(SubsystemTerrain terrain, Vector3 position, Vector3 direction)
            // ReSharper restore UnusedParameter.Local
            : base(15) {
            m_position = position;
            m_direction = direction;
            Texture = ContentManager.Get<Texture2D>("Textures/SoundParticle");
            TextureSlotsCount = 2;
        }

        public void AddNote(Color color) {
            int num = 0;
            Particle particle;
            while (true) {
                if (num < Particles.Length) {
                    particle = Particles[num];
                    if (!Particles[num].IsActive) {
                        break;
                    }
                    num++;
                    continue;
                }
                return;
            }
            particle.IsActive = true;
            particle.Position = m_position;
            particle.Color = Color.White;
            particle.Size = new Vector2(0.1f);
            particle.TimeToLive = m_random.Float(1f, 1.5f);
            particle.Velocity = 3f * (m_direction + m_random.Vector3(0.5f));
            particle.BaseColor = color;
            particle.TextureSlot = m_random.Int(0, TextureSlotsCount * TextureSlotsCount - 1);
            particle.BillboardingMode = ParticleBillboardingMode.Vertical;
        }

        public override bool Simulate(float dt) {
            dt = Math.Clamp(dt, 0f, 0.1f);
            float num = MathF.Pow(0.02f, dt);
            bool flag = false;
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (particle.IsActive) {
                    flag = true;
                    particle.TimeToLive -= dt;
                    if (particle.TimeToLive > 0f) {
                        particle.Velocity += new Vector3(0f, 5f, 0f) * dt;
                        particle.Velocity *= num;
                        particle.Position += particle.Velocity * dt;
                        particle.Color = particle.BaseColor * MathUtils.Saturate(2f * particle.TimeToLive);
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