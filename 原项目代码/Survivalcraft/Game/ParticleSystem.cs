using Engine;
using Engine.Graphics;

namespace Game {
    public class ParticleSystem<T> : ParticleSystemBase where T : Particle, new() {
        public T[] m_particles;

        public Texture2D m_texture;

        public Vector3[] m_front = new Vector3[3];

        public Vector3[] m_right = new Vector3[3];

        public Vector3[] m_up = new Vector3[3];

        public TexturedBatch3D AdditiveBatch;

        public TexturedBatch3D AlphaBlendedBatch;

        public T[] Particles => m_particles;

        public Texture2D Texture {
            get => m_texture;
            set {
                if (value != m_texture) {
                    m_texture = value;
                    AdditiveBatch = null;
                    AlphaBlendedBatch = null;
                }
            }
        }

        public int TextureSlotsCount { get; set; }

        public ParticleSystem(int particlesCount) {
            m_particles = new T[particlesCount];
            for (int i = 0; i < m_particles.Length; i++) {
                m_particles[i] = new T();
            }
        }

        public override void Draw(Camera camera) {
            if (AdditiveBatch == null
                || AlphaBlendedBatch == null) {
                AdditiveBatch = SubsystemParticles.PrimitivesRenderer.TexturedBatch(
                    m_texture,
                    true,
                    0,
                    DepthStencilState.DepthRead,
                    null,
                    BlendState.Additive,
                    SamplerState.PointClamp
                );
                AlphaBlendedBatch = SubsystemParticles.PrimitivesRenderer.TexturedBatch(
                    m_texture,
                    true,
                    0,
                    DepthStencilState.Default,
                    null,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp
                );
            }
            m_front[0] = camera.ViewDirection;
            m_right[0] = Vector3.Normalize(Vector3.Cross(m_front[0], Vector3.UnitY));
            m_up[0] = Vector3.Normalize(Vector3.Cross(m_right[0], m_front[0]));
            m_front[1] = camera.ViewDirection;
            m_right[1] = Vector3.Normalize(Vector3.Cross(m_front[1], Vector3.UnitY));
            m_up[1] = Vector3.UnitY;
            m_front[2] = Vector3.UnitY;
            m_right[2] = Vector3.UnitX;
            m_up[2] = Vector3.UnitZ;
            float s = 1f / TextureSlotsCount;
            for (int i = 0; i < m_particles.Length; i++) {
                Particle particle = m_particles[i];
                if (particle.IsActive) {
                    Vector3 position = particle.Position;
                    Vector2 size = particle.Size;
                    float rotation = particle.Rotation;
                    int textureSlot = particle.TextureSlot;
                    Vector3 p;
                    Vector3 p2;
                    Vector3 p3;
                    Vector3 p4;
                    if (particle.BillboardingMode == ParticleBillboardingMode.None) {
                        p = position + (-particle.Right - particle.Up);
                        p2 = position + (particle.Right - particle.Up);
                        p3 = position + (particle.Right + particle.Up);
                        p4 = position + (-particle.Right + particle.Up);
                    }
                    else if (particle.BillboardingMode == ParticleBillboardingMode.Horizontal
                        && rotation != 0f) {
                        Vector3 vector = new(MathF.Cos(rotation), 0f, MathF.Sin(rotation));
                        Vector3 vector2 = new(vector.Z, 0f, 0f - vector.X);
                        vector2 *= size.Y;
                        vector *= size.X;
                        p = position + (-vector2 - vector);
                        p2 = position + (vector2 - vector);
                        p3 = position + (vector2 + vector);
                        p4 = position + (-vector2 + vector);
                    }
                    else if (rotation != 0f) {
                        Vector3 v = m_front[(uint)particle.BillboardingMode];
                        Vector3 v2 = v.X * v.X > v.Z * v.Z
                            ? new Vector3(0f, MathF.Cos(rotation), MathF.Sin(rotation))
                            : new Vector3(MathF.Sin(rotation), MathF.Cos(rotation), 0f);
                        Vector3 vector3 = Vector3.Normalize(Vector3.Cross(v, v2));
                        v2 = Vector3.Normalize(Vector3.Cross(v, vector3));
                        vector3 *= size.Y;
                        v2 *= size.X;
                        p = position + (-vector3 - v2);
                        p2 = position + (vector3 - v2);
                        p3 = position + (vector3 + v2);
                        p4 = position + (-vector3 + v2);
                    }
                    else {
                        Vector3 vector4 = m_right[(uint)particle.BillboardingMode];
                        Vector3 vector5 = m_up[(uint)particle.BillboardingMode];
                        Vector3 vector6 = vector4 * size.X;
                        Vector3 vector7 = vector5 * size.Y;
                        p = position + (-vector6 - vector7);
                        p2 = position + (vector6 - vector7);
                        p3 = position + (vector6 + vector7);
                        p4 = position + (-vector6 + vector7);
                    }
                    TexturedBatch3D obj = particle.UseAdditiveBlending ? AdditiveBatch : AlphaBlendedBatch;
                    Vector2 v3 = new(textureSlot % TextureSlotsCount, textureSlot / TextureSlotsCount);
                    float num = 0f;
                    float num2 = 1f;
                    float num3 = 1f;
                    float num4 = 0f;
                    if (particle.FlipX) {
                        num = 1f - num;
                        num2 = 1f - num2;
                    }
                    if (particle.FlipY) {
                        num3 = 1f - num3;
                        num4 = 1f - num4;
                    }
                    Vector2 texCoord = (v3 + new Vector2(num, num3)) * s;
                    Vector2 texCoord2 = (v3 + new Vector2(num2, num3)) * s;
                    Vector2 texCoord3 = (v3 + new Vector2(num2, num4)) * s;
                    Vector2 texCoord4 = (v3 + new Vector2(num, num4)) * s;
                    obj.QueueQuad(
                        p,
                        p2,
                        p3,
                        p4,
                        texCoord,
                        texCoord2,
                        texCoord3,
                        texCoord4,
                        particle.Color
                    );
                }
            }
        }

        public override bool Simulate(float dt) => false;
    }
}