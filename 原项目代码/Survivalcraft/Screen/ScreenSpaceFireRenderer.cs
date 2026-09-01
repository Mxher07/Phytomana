using Engine;
using Engine.Graphics;

namespace Game {
    public class ScreenSpaceFireRenderer {
        public class Particle {
            public bool Active;

            public Vector2 Position;

            public Vector2 Size;

            public float Speed;

            public int TextureSlot;

            public bool FlipX;

            public bool FlipY;

            public float AnimationTime;

            public float TimeToLive;
        }

        public List<Particle> m_particles = [];

        public Random m_random = new();

        public float m_toGenerate;

        public Texture2D m_texture;

        public float ParticlesPerSecond { get; set; }

        public float ParticleSpeed { get; set; }

        public float MinTimeToLive { get; set; }

        public float MaxTimeToLive { get; set; }

        public float ParticleSize { get; set; }

        public float ParticleAnimationPeriod { get; set; }

        public float ParticleAnimationOffset { get; set; }

        public Vector2 Origin { get; set; }

        public float Width { get; set; }

        public float CutoffPosition { get; set; }

        public ScreenSpaceFireRenderer(int particlesCount) {
            m_texture = ContentManager.Get<Texture2D>("Textures/FireParticle");
            for (int i = 0; i < particlesCount; i++) {
                m_particles.Add(new Particle());
            }
        }

        public virtual void Update(float dt) {
            m_toGenerate += ParticlesPerSecond * dt;
            foreach (Particle particle in m_particles) {
                if (particle.Active) {
                    particle.Position.Y += particle.Speed * dt;
                    particle.AnimationTime += dt;
                    particle.TimeToLive -= dt;
                    particle.TextureSlot = (int)MathUtils.Max(9f * particle.AnimationTime / ParticleAnimationPeriod, 0f);
                    if (particle.TimeToLive <= 0f
                        || particle.TextureSlot > 8
                        || particle.Position.Y < CutoffPosition) {
                        particle.Active = false;
                    }
                }
                else if (m_toGenerate >= 1f) {
                    particle.Active = true;
                    particle.Position = new Vector2(m_random.Float(Origin.X, Origin.X + Width), Origin.Y);
                    particle.Size = new Vector2(ParticleSize);
                    particle.Speed = (0f - m_random.Float(0.75f, 1.25f)) * ParticleSpeed;
                    particle.AnimationTime = m_random.Float(0f, ParticleAnimationOffset);
                    particle.TimeToLive = MathUtils.Lerp(MinTimeToLive, MaxTimeToLive, m_random.Float(0f, 1f));
                    particle.FlipX = m_random.Int(0, 1) == 0;
                    particle.FlipY = m_random.Int(0, 1) == 0;
                    m_toGenerate -= 1f;
                }
            }
            m_toGenerate = MathUtils.Remainder(m_toGenerate, 1f);
        }

        public virtual void Draw(PrimitivesRenderer2D primitivesRenderer, float depth, Matrix matrix, Color color) {
            TexturedBatch2D texturedBatch2D = primitivesRenderer.TexturedBatch(
                m_texture,
                false,
                0,
                DepthStencilState.None,
                null,
                null,
                SamplerState.PointClamp
            );
            int count = texturedBatch2D.TriangleVertices.Count;
            foreach (Particle particle in m_particles) {
                if (particle.Active) {
                    DrawParticle(texturedBatch2D, particle, depth, color);
                }
            }
            texturedBatch2D.TransformTriangles(matrix, count);
        }

        public virtual void DrawParticle(TexturedBatch2D batch, Particle particle, float depth, Color color) {
            Vector2 corner = particle.Position - particle.Size / 2f;
            Vector2 corner2 = particle.Position + particle.Size / 2f;
            int textureSlot = particle.TextureSlot;
            Vector2 v = new(textureSlot % 3, textureSlot / 3);
            float num = 0f;
            float num2 = 1f;
            float num3 = 0f;
            float num4 = 1f;
            if (particle.FlipX) {
                num = 1f - num;
                num2 = 1f - num2;
            }
            if (particle.FlipY) {
                num3 = 1f - num3;
                num4 = 1f - num4;
            }
            Vector2 texCoord = (v + new Vector2(num, num3)) * 0.333333343f;
            Vector2 texCoord2 = (v + new Vector2(num2, num4)) * 0.333333343f;
            batch.QueueQuad(corner, corner2, depth, texCoord, texCoord2, color);
        }
    }
}