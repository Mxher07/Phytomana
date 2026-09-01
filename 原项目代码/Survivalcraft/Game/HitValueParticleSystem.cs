using Engine;
using Engine.Graphics;

namespace Game {
    public class HitValueParticleSystem : ParticleSystem<HitValueParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public float TimeToLive;

            public Vector3 Velocity;

            public Color BaseColor;

            public string Text;
        }

        public FontBatch3D m_batch;

        public HitValueParticleSystem(Vector3 position, Vector3 velocity, Color color, string text) : base(1) {
            Random random = new();
            Particle obj = Particles[0];
            obj.IsActive = true;
            obj.Position = position;
            obj.TimeToLive = 0.9f;
            obj.Velocity = velocity + random.Vector3(0.75f) * new Vector3(1f, 0f, 1f) + 0.5f * Vector3.UnitY;
            obj.BaseColor = color;
            obj.Text = text;
        }

        public override bool Simulate(float dt) {
            dt = Math.Clamp(dt, 0f, 0.1f);
            float num = MathF.Pow(0.1f, dt);
            bool flag = false;
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (particle.IsActive) {
                    flag = true;
                    particle.TimeToLive -= dt;
                    if (particle.TimeToLive > 0f) {
                        particle.Velocity += new Vector3(0f, 0.5f, 0f) * dt;
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

        public override void Draw(Camera camera) {
            if (m_batch == null) {
                m_batch = SubsystemParticles.PrimitivesRenderer.FontBatch(LabelWidget.BitmapFont, 0, DepthStencilState.None);
            }
            Vector3 viewDirection = camera.ViewDirection;
            Vector3 vector = Vector3.Normalize(Vector3.Cross(viewDirection, Vector3.UnitY));
            Vector3 v = -Vector3.Normalize(Vector3.Cross(vector, viewDirection));
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (particle.IsActive) {
                    float num = Vector3.Distance(camera.ViewPosition, particle.Position);
                    float num2 = MathUtils.Saturate(3f * (num - 0.2f));
                    float num3 = MathUtils.Saturate(0.2f * (20f - num));
                    float num4 = num2 * num3;
                    if (num4 > 0f) {
                        float s = 0.006f * MathF.Sqrt(num);
                        Color color = particle.Color * num4;
                        m_batch.QueueText(
                            particle.Text,
                            particle.Position,
                            vector * s,
                            v * s,
                            color,
                            TextAnchor.HorizontalCenter | TextAnchor.VerticalCenter,
                            Vector2.Zero
                        );
                    }
                }
            }
        }
    }
}