using Engine;
using Engine.Graphics;

namespace Game {
    public class ExplosionParticleSystem : ParticleSystem<ExplosionParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public float Strength;
        }

        public Dictionary<Point3, Particle> m_particlesByPoint = [];

        public List<Particle> m_inactiveParticles = [];

        public Random m_random = new();

        public const float m_duration = 2.5f;

        public bool m_isEmpty;

        public ExplosionParticleSystem() : base(2000) {
            Texture = ContentManager.Get<Texture2D>("Textures/FireParticle");
            TextureSlotsCount = 3;
            m_inactiveParticles.AddRange(Particles);
        }

        public void SetExplosionCell(Point3 point, float strength) {
            if (!m_particlesByPoint.TryGetValue(point, out Particle value)) {
                if (m_inactiveParticles.Count > 0) {
                    value = m_inactiveParticles[^1];
                    m_inactiveParticles.RemoveAt(m_inactiveParticles.Count - 1);
                }
                else {
                    for (int i = 0; i < 5; i++) {
                        int num = m_random.Int(0, Particles.Length - 1);
                        if (strength > Particles[num].Strength) {
                            value = Particles[num];
                        }
                    }
                }
                if (value != null) {
                    m_particlesByPoint.Add(point, value);
                }
            }
            if (value != null) {
                value.IsActive = true;
                value.Position = new Vector3(point.X, point.Y, point.Z)
                    + new Vector3(0.5f)
                    + 0.2f * new Vector3(m_random.Float(-1f, 1f), m_random.Float(-1f, 1f), m_random.Float(-1f, 1f));
                value.Size = new Vector2(m_random.Float(0.6f, 0.9f));
                value.Strength = strength;
                value.Color = Color.White;
                m_isEmpty = false;
            }
        }

        public override bool Simulate(float dt) {
            if (!m_isEmpty) {
                m_isEmpty = true;
                for (int i = 0; i < Particles.Length; i++) {
                    Particle particle = Particles[i];
                    if (particle.IsActive) {
                        m_isEmpty = false;
                        particle.Strength -= dt / m_duration;
                        if (particle.Strength > 0f) {
                            particle.TextureSlot = (int)MathUtils.Min(9f * (1f - particle.Strength) * 0.6f, 8f);
                            particle.Position.Y += 2f * MathUtils.Max(1f - particle.Strength - 0.25f, 0f) * dt;
                        }
                        else {
                            particle.IsActive = false;
                            m_inactiveParticles.Add(particle);
                        }
                    }
                }
            }
            return false;
        }

        public override void Draw(Camera camera) {
            if (!m_isEmpty) {
                base.Draw(camera);
            }
        }
    }
}