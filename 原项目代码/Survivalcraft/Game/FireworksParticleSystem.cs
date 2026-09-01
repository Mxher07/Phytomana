using Engine;
using Engine.Graphics;

namespace Game {
    public class FireworksParticleSystem : ParticleSystem<FireworksParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public Vector3 Velocity;

            public float TimeToLive;

            public float FadeRate;

            public Color BaseColor;

            public float RotationSpeed;

            public float GenerationFrequency;

            public float GenerationAccumulator;

            public bool HighDamping;
        }

        public Random m_random = new();

        public Color m_color;

        public float m_flickering;

        public int m_nextParticle;

        public FireworksParticleSystem(Vector3 position, Color color, FireworksBlock.Shape shape, float flickering, float particleSize) : base(300) {
            Texture = ContentManager.Get<Texture2D>("Textures/FireworksParticle");
            m_color = color;
            m_flickering = flickering;
            TextureSlotsCount = 2;
            if (shape == FireworksBlock.Shape.SmallBurst
                || shape == FireworksBlock.Shape.LargeBurst) {
                int num = shape == FireworksBlock.Shape.SmallBurst ? 100 : 200;
                while (m_nextParticle < num) {
                    Particle particle = Particles[m_nextParticle++];
                    particle.IsActive = true;
                    particle.Position = position;
                    particle.Size = new Vector2(0.2f * particleSize);
                    particle.TimeToLive = shape == FireworksBlock.Shape.SmallBurst ? m_random.Float(0.5f, 2f) : m_random.Float(1f, 3f);
                    particle.Velocity = m_random.Vector3(0.5f, 1f);
                    particle.Velocity *= (shape == FireworksBlock.Shape.SmallBurst ? 16 : 26) * particle.Velocity.LengthSquared();
                    particle.TextureSlot = m_random.Int(0, 3);
                    particle.FadeRate = m_random.Float(1f, 3f);
                    particle.BaseColor = m_color * m_random.Float(0.5f, 1f);
                    particle.RotationSpeed = 0f;
                }
            }
            switch (shape) {
                case FireworksBlock.Shape.Circle: {
                    float num4 = m_random.Float(0f, (float)Math.PI * 2f);
                    int num5 = 150;
                    for (int j = 0; j < num5; j++) {
                        float x2 = (float)Math.PI * 2f * j / num5 + num4;
                        Vector3 v2 = new(MathF.Sin(x2) + 0.1f * m_random.Float(-1f, 1f), 0f, MathF.Cos(x2) + 0.1f * m_random.Float(-1f, 1f));
                        Particle obj2 = Particles[m_nextParticle++];
                        obj2.IsActive = true;
                        obj2.Position = position;
                        obj2.Size = new Vector2(0.2f * particleSize);
                        obj2.TimeToLive = m_random.Float(1f, 3f);
                        obj2.Velocity = 20f * v2;
                        obj2.TextureSlot = m_random.Int(0, 3);
                        obj2.FadeRate = m_random.Float(1f, 3f);
                        obj2.BaseColor = m_color * m_random.Float(0.5f, 1f);
                        obj2.RotationSpeed = 0f;
                    }
                    break;
                }
                case FireworksBlock.Shape.Disc: {
                    float num10 = m_random.Float(0f, (float)Math.PI * 2f);
                    int num11 = 13;
                    for (int m = 0; m <= num11; m++) {
                        float num12 = m / (float)num11;
                        int num13 = (int)MathF.Round(num12 * 2f * num11);
                        for (int n = 0; n < num13; n++) {
                            float x5 = (float)Math.PI * 2f * n / num13 + num10;
                            Vector3 v4 = new(
                                num12 * MathF.Sin(x5) + 0.1f * m_random.Float(-1f, 1f),
                                0f,
                                num12 * MathF.Cos(x5) + 0.1f * m_random.Float(-1f, 1f)
                            );
                            Particle obj4 = Particles[m_nextParticle++];
                            obj4.IsActive = true;
                            obj4.Position = position;
                            obj4.Size = new Vector2(0.2f * particleSize);
                            obj4.TimeToLive = m_random.Float(1f, 3f);
                            obj4.Velocity = 22f * v4;
                            obj4.TextureSlot = m_random.Int(0, 3);
                            obj4.FadeRate = m_random.Float(1f, 3f);
                            obj4.BaseColor = m_color * m_random.Float(0.5f, 1f);
                            obj4.RotationSpeed = 0f;
                        }
                    }
                    break;
                }
                case FireworksBlock.Shape.Ball: {
                    float num14 = m_random.Float(0f, (float)Math.PI * 2f);
                    int num15 = 12;
                    Vector3 v5 = default;
                    for (int num16 = 0; num16 <= num15; num16++) {
                        float x6 = (float)Math.PI * num16 / num15;
                        v5.Y = MathF.Cos(x6);
                        float num17 = MathF.Sin(x6);
                        int num18 = (int)MathF.Round(num17 * 2f * num15);
                        for (int num19 = 0; num19 < num18; num19++) {
                            float x7 = (float)Math.PI * 2f * num19 / num18 + num14;
                            v5.X = num17 * MathF.Sin(x7);
                            v5.Z = num17 * MathF.Cos(x7);
                            Particle obj5 = Particles[m_nextParticle++];
                            obj5.IsActive = true;
                            obj5.Position = position;
                            obj5.Size = new Vector2(0.2f * particleSize);
                            obj5.TimeToLive = m_random.Float(1f, 3f);
                            obj5.Velocity = 20f * v5;
                            obj5.TextureSlot = m_random.Int(0, 3);
                            obj5.FadeRate = m_random.Float(1f, 3f);
                            obj5.BaseColor = m_color * m_random.Float(0.5f, 1f);
                            obj5.RotationSpeed = 0f;
                        }
                    }
                    break;
                }
                case FireworksBlock.Shape.ShortTrails:
                case FireworksBlock.Shape.LongTrails: {
                    float num6 = m_random.Float(0f, (float)Math.PI * 2f);
                    int num7 = 3;
                    Vector3 v3 = default;
                    for (int k = 0; k <= num7; k++) {
                        float x3 = (float)Math.PI * k / num7;
                        float num8 = MathF.Sin(x3);
                        int num9 = (int)MathF.Round(num8 * (shape == FireworksBlock.Shape.ShortTrails ? 3 : 2) * num7);
                        for (int l = 0; l < num9; l++) {
                            float x4 = (float)Math.PI * 2f * l / num9 + num6;
                            v3.X = num8 * MathF.Sin(x4) + 0.3f * m_random.Float(-1f, 1f);
                            v3.Y = MathF.Cos(x3) + 0.3f * m_random.Float(-1f, 1f);
                            v3.Z = num8 * MathF.Cos(x4) + 0.3f * m_random.Float(-1f, 1f);
                            Particle obj3 = Particles[m_nextParticle++];
                            obj3.IsActive = true;
                            obj3.Position = position;
                            obj3.Size = new Vector2(0.25f);
                            obj3.TimeToLive = m_random.Float(0.5f, 2.5f);
                            obj3.Velocity = shape == FireworksBlock.Shape.ShortTrails ? 25f * v3 : 35f * v3;
                            obj3.TextureSlot = m_random.Int(0, 3);
                            obj3.FadeRate = m_random.Float(1f, 3f);
                            obj3.BaseColor = m_color * m_random.Float(0.5f, 1f);
                            obj3.GenerationFrequency = shape == FireworksBlock.Shape.ShortTrails ? 1.9f : 2.1f;
                            obj3.RotationSpeed = m_random.Float(-40f, 40f);
                        }
                    }
                    break;
                }
                case FireworksBlock.Shape.FlatTrails: {
                    float num2 = m_random.Float(0f, (float)Math.PI * 2f);
                    int num3 = 13;
                    for (int i = 0; i < num3; i++) {
                        float x = (float)Math.PI * 2f * i / num3 + num2;
                        Vector3 v = new(MathF.Sin(x) + 0.1f * m_random.Float(-1f, 1f), 0f, MathF.Cos(x) + 0.1f * m_random.Float(-1f, 1f));
                        Particle obj = Particles[m_nextParticle++];
                        obj.IsActive = true;
                        obj.Position = position;
                        obj.Size = new Vector2(0.25f);
                        obj.TimeToLive = m_random.Float(0.5f, 2.5f);
                        obj.Velocity = 25f * v;
                        obj.TextureSlot = m_random.Int(0, 3);
                        obj.FadeRate = m_random.Float(1f, 3f);
                        obj.BaseColor = m_color * m_random.Float(0.5f, 1f);
                        obj.GenerationFrequency = 2.5f;
                        obj.RotationSpeed = m_random.Float(-40f, 40f);
                    }
                    break;
                }
            }
        }

        public override bool Simulate(float dt) {
            dt = Math.Clamp(dt, 0f, 0.1f);
            float num = MathF.Pow(0.01f, dt);
            float num2 = MathF.Pow(0.1f, dt);
            bool flag = false;
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (!particle.IsActive) {
                    continue;
                }
                flag = true;
                particle.TimeToLive -= dt;
                if (particle.TimeToLive > 0f) {
                    Vector3 position = particle.Position += particle.Velocity * dt;
                    particle.Velocity.Y += -9.81f * dt;
                    particle.Velocity *= particle.HighDamping ? num : num2;
                    particle.Color = particle.BaseColor * MathUtils.Min(particle.FadeRate * particle.TimeToLive, 1f);
                    particle.Rotation += particle.RotationSpeed * dt;
                    if (!particle.HighDamping
                        && m_random.Float(0f, 1f) < m_flickering) {
                        particle.Color = Color.Transparent;
                    }
                    if (m_random.Float(0f, 1f) < 20f * dt) {
                        particle.TextureSlot = m_random.Int(0, 3);
                    }
                    if (particle.GenerationFrequency > 0f) {
                        float num3 = particle.Velocity.Length();
                        particle.GenerationAccumulator += particle.GenerationFrequency * num3 * dt;
                        if (particle.GenerationAccumulator > 1f
                            && m_nextParticle < Particles.Length) {
                            particle.GenerationAccumulator -= 1f;
                            Particle obj = Particles[m_nextParticle++];
                            obj.IsActive = true;
                            obj.Position = position;
                            obj.Size = new Vector2(0.2f);
                            obj.TimeToLive = 1f;
                            obj.TextureSlot = m_random.Int(0, 3);
                            obj.FadeRate = 1f;
                            obj.BaseColor = particle.BaseColor;
                            obj.HighDamping = true;
                            obj.RotationSpeed = 0f;
                        }
                    }
                }
                else {
                    particle.IsActive = false;
                }
            }
            return !flag;
        }
    }
}