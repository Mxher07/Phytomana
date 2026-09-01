using Engine;
using Engine.Graphics;

namespace Game {
    public class PrecipitationShaftParticleSystem : ParticleSystemBase {
        public class Particle {
            public bool IsActive;

            public bool GenerateSplash;

            public byte TextureSlot;

            public Vector3 Position;

            public Vector2 TexCoord1;

            public Vector2 TexCoord2;

            public Vector2 TexCoord3;

            public Vector2 TexCoord4;

            public float Speed;

            public float YLimit;
        }

        public const float m_viewHeight = 10f;

        public const int m_particlesCount = 4;

        public SubsystemWeather m_subsystemWeather;

        public GameWidget m_gameWidget;

        public Random m_random;

        public TexturedBatch3D m_batch;

        public Particle[] m_particles = new Particle[m_particlesCount];

        public PrecipitationType m_precipitationType;

        public bool m_isVisible;

        public bool m_isEmpty;

        public float? m_lastViewY;

        public float m_toCreate;

        public float m_averageSpeed;

        public Texture2D m_texture;

        public Vector2 m_size;

        public float m_intensity;

        public int m_yLimit;

        public int m_topmostValue;

        public int m_topmostBelowValue;

        public double m_lastUpdateTime = -1.7976931348623157E+308;

        public float m_lastSkylightIntensity = -3.40282347E+38f;

        public bool m_needsInitialize;

        public Point2 Point { get; set; }

        public PrecipitationShaftParticleSystem(GameWidget gameWidget, SubsystemWeather subsystemWeather, Random random, Point2 point) {
            m_gameWidget = gameWidget;
            m_subsystemWeather = subsystemWeather;
            m_random = random;
            Point = point;
            for (int i = 0; i < m_particles.Length; i++) {
                m_particles[i] = new Particle();
            }
            Initialize();
        }

        public override bool Simulate(float dt) {
            if (m_subsystemWeather.SubsystemTime.GameTime - m_lastUpdateTime > 1.0
                || MathF.Abs(m_lastSkylightIntensity - m_subsystemWeather.SubsystemSky.SkyLightIntensity) > 0.1f) {
                m_lastUpdateTime = m_subsystemWeather.SubsystemTime.GameTime;
                m_lastSkylightIntensity = m_subsystemWeather.SubsystemSky.SkyLightIntensity;
                PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(Point.X, Point.Y);
                m_intensity = precipitationShaftInfo.Intensity;
                m_yLimit = precipitationShaftInfo.YLimit;
                m_topmostValue = m_subsystemWeather.SubsystemTerrain.Terrain.GetCellValue(Point.X, precipitationShaftInfo.YLimit - 1, Point.Y);
                m_topmostBelowValue = m_subsystemWeather.SubsystemTerrain.Terrain.GetCellValue(Point.X, precipitationShaftInfo.YLimit - 2, Point.Y);
                if (precipitationShaftInfo.Type != m_precipitationType) {
                    m_needsInitialize = true;
                }
            }
            Camera activeCamera = m_gameWidget.ActiveCamera;
            if (!m_isEmpty
                || (m_intensity > 0f && m_yLimit < activeCamera.ViewPosition.Y + 5f)) {
                Vector2 v = Vector2.Normalize(new Vector2(activeCamera.ViewDirection.X, activeCamera.ViewDirection.Z));
                Vector2 v2 = Vector2.Normalize(
                    new Vector2(Point.X + 0.5f - activeCamera.ViewPosition.X + 0.7f * v.X, Point.Y + 0.5f - activeCamera.ViewPosition.Z + 0.7f * v.Y)
                );
                float num = Vector2.Dot(v, v2);
                m_isVisible = num > 0.5f;
                if (m_isVisible) {
                    if (m_needsInitialize) {
                        m_needsInitialize = false;
                        Initialize();
                    }
                    float y = activeCamera.ViewPosition.Y;
                    float num2 = y - 5f;
                    float num3 = y + 5f;
                    float num4;
                    float num5;
                    if (m_lastViewY.HasValue) {
                        if (y < m_lastViewY.Value) {
                            num4 = num2;
                            num5 = m_lastViewY.Value - 5f;
                        }
                        else {
                            num4 = m_lastViewY.Value + 5f;
                            num5 = num3;
                        }
                    }
                    else {
                        num4 = num2;
                        num5 = num3;
                    }
                    float num6 = (num5 - num4) / 10f * m_particles.Length * m_intensity;
                    int num7 = (int)num6 + (m_random.Float(0f, 1f) < num6 - (int)num6 ? 1 : 0);
                    m_lastViewY = y;
                    m_toCreate += m_particles.Length * m_intensity / 10f * m_averageSpeed * dt;
                    m_isEmpty = true;
                    float num8 = m_precipitationType == PrecipitationType.Rain ? 0f : 0.03f;
                    for (int i = 0; i < m_particles.Length; i++) {
                        Particle particle = m_particles[i];
                        if (particle.IsActive) {
                            if (particle.YLimit == 0f
                                && particle.Position.Y <= m_yLimit + num8) {
                                RaycastParticle(particle);
                            }
                            bool flag = particle.YLimit != 0f && particle.Position.Y <= particle.YLimit + num8;
                            if (!flag
                                && particle.Position.Y >= num2
                                && particle.Position.Y <= num3) {
                                particle.Position.Y -= particle.Speed * dt;
                                m_isEmpty = false;
                                continue;
                            }
                            particle.IsActive = false;
                            if (particle.GenerateSplash && flag) {
                                if (m_precipitationType == PrecipitationType.Rain
                                    && m_random.Bool(0.5f)) {
                                    m_subsystemWeather.RainSplashParticleSystem.AddSplash(
                                        m_topmostValue,
                                        new Vector3(particle.Position.X, particle.YLimit + num8, particle.Position.Z),
                                        m_subsystemWeather.RainColor
                                    );
                                }
                                if (m_precipitationType == PrecipitationType.Snow) {
                                    m_subsystemWeather.SnowSplashParticleSystem.AddSplash(
                                        m_topmostValue,
                                        new Vector3(particle.Position.X, particle.YLimit + num8, particle.Position.Z),
                                        m_size,
                                        m_subsystemWeather.SnowColor,
                                        particle.TextureSlot
                                    );
                                }
                            }
                        }
                        else if (num7 > 0) {
                            particle.Position.X = Point.X + m_random.Float(0f, 1f);
                            particle.Position.Y = m_random.Float(num4, num5);
                            particle.Position.Z = Point.Y + m_random.Float(0f, 1f);
                            particle.IsActive = particle.Position.Y >= m_yLimit;
                            particle.YLimit = 0f;
                            num7--;
                        }
                        else if (m_toCreate >= 1f) {
                            particle.Position.X = Point.X + m_random.Float(0f, 1f);
                            particle.Position.Y = m_random.Float(num3 - m_averageSpeed * dt, num3);
                            particle.Position.Z = Point.Y + m_random.Float(0f, 1f);
                            particle.IsActive = particle.Position.Y >= m_yLimit;
                            particle.YLimit = 0f;
                            m_toCreate -= 1f;
                        }
                    }
                    m_toCreate -= MathF.Floor(m_toCreate);
                }
                else {
                    m_needsInitialize = true;
                }
            }
            return false;
        }

        public override void Draw(Camera camera) {
            if (!m_isVisible
                || m_isEmpty
                || camera.GameWidget != m_gameWidget) {
                return;
            }
            m_batch ??= SubsystemParticles.PrimitivesRenderer.TexturedBatch(
                m_texture,
                false,
                0,
                DepthStencilState.DepthRead,
                null,
                BlendState.AlphaBlend,
                SamplerState.PointClamp
            );
            float num = camera.ViewPosition.Y + 5f;
            Vector3 viewDirection = camera.ViewDirection;
            Vector3 vector = Vector3.Normalize(Vector3.Cross(viewDirection, Vector3.UnitY));
            Vector3 v = m_precipitationType == PrecipitationType.Rain ? Vector3.UnitY : Vector3.Normalize(Vector3.Cross(viewDirection, vector));
            Vector3 vector2 = vector * m_size.X;
            Vector3 vector3 = v * m_size.Y;
            if (m_precipitationType == PrecipitationType.Rain) {
                Vector3 v2 = -vector2 - vector3;
                Vector3 v3 = vector2 - vector3;
                Vector3 v4 = vector3;
                for (int i = 0; i < m_particles.Length; i++) {
                    Particle particle = m_particles[i];
                    if (particle.IsActive) {
                        Vector3 p = particle.Position + v2;
                        Vector3 p2 = particle.Position + v3;
                        Vector3 p3 = particle.Position + v4;
                        Color color = m_subsystemWeather.RainColor * MathUtils.Min(0.6f * (num - particle.Position.Y), 1f);
                        m_batch.QueueTriangle(
                            p,
                            p2,
                            p3,
                            particle.TexCoord1,
                            particle.TexCoord2,
                            particle.TexCoord3,
                            color
                        );
                    }
                }
                return;
            }
            Vector3 v5 = -vector2 - vector3;
            Vector3 v6 = vector2 - vector3;
            Vector3 v7 = vector2 + vector3;
            Vector3 v8 = -vector2 + vector3;
            for (int j = 0; j < m_particles.Length; j++) {
                Particle particle2 = m_particles[j];
                if (particle2.IsActive) {
                    Vector3 p4 = particle2.Position + v5;
                    Vector3 p5 = particle2.Position + v6;
                    Vector3 p6 = particle2.Position + v7;
                    Vector3 p7 = particle2.Position + v8;
                    Color color2 = m_subsystemWeather.SnowColor * MathUtils.Min(0.6f * (num - particle2.Position.Y), 1f);
                    m_batch.QueueQuad(
                        p4,
                        p5,
                        p6,
                        p7,
                        particle2.TexCoord1,
                        particle2.TexCoord2,
                        particle2.TexCoord3,
                        particle2.TexCoord4,
                        color2
                    );
                }
            }
        }

        public void RaycastParticle(Particle particle) {
            particle.YLimit = m_yLimit;
            particle.GenerateSplash = true;
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(m_topmostValue)];
            if (!block.IsTransparent_(m_topmostValue)) {
                return;
            }
            Ray3 ray = new(new Vector3(particle.Position.X - Point.X, 1f, particle.Position.Z - Point.Y), -Vector3.UnitY);
            float? num = block.Raycast(ray, m_subsystemWeather.SubsystemTerrain, m_topmostValue, false, out int _, out BoundingBox _);
            if (num.HasValue) {
                particle.YLimit -= num.Value;
                return;
            }
            particle.YLimit -= 1f;
            if (BlocksManager.Blocks[Terrain.ExtractContents(m_topmostBelowValue)]
                .IsFaceTransparent(m_subsystemWeather.SubsystemTerrain, 4, m_topmostBelowValue)) {
                particle.GenerateSplash = false;
            }
        }

        public void Initialize() {
            m_precipitationType = m_subsystemWeather.GetPrecipitationShaftInfo(Point.X, Point.Y).Type;
            m_lastViewY = null;
            m_toCreate = m_random.Float(0f, 0.9f);
            m_batch = null;
            m_lastSkylightIntensity = -3.40282347E+38f;
            switch (m_precipitationType) {
                case PrecipitationType.Rain: {
                    float num4 = 8f;
                    float num5 = 12f;
                    m_averageSpeed = (num4 + num5) / 2f;
                    m_size = new Vector2(0.02f, 0.15f);
                    m_texture = ContentManager.Get<Texture2D>("Textures/RainParticle");
                    for (int j = 0; j < m_particles.Length; j++) {
                        Particle obj = m_particles[j];
                        obj.IsActive = false;
                        obj.TexCoord1 = new Vector2(0f, 1f);
                        obj.TexCoord2 = new Vector2(1f, 1f);
                        obj.TexCoord3 = new Vector2(0.5f, 0f);
                        obj.Speed = m_random.Float(num4, num5);
                    }
                    break;
                }
                case PrecipitationType.Snow: {
                    float num = 0.25f;
                    float num2 = 0.5f;
                    float num3 = 3f;
                    m_averageSpeed = (num2 + num3) / 2f;
                    m_size = new Vector2(0.07f, 0.07f);
                    m_texture = ContentManager.Get<Texture2D>("Textures/SnowParticle");
                    for (int i = 0; i < m_particles.Length; i++) {
                        Particle particle = m_particles[i];
                        particle.IsActive = false;
                        particle.TextureSlot = (byte)m_random.Int(0, 15);
                        Vector2 v = new Vector2(particle.TextureSlot % 4, particle.TextureSlot / 4) * num;
                        particle.TexCoord1 = v + new Vector2(0f, 0f);
                        particle.TexCoord2 = v + new Vector2(num, 0f);
                        particle.TexCoord3 = v + new Vector2(num, num);
                        particle.TexCoord4 = v + new Vector2(0f, num);
                        particle.Speed = m_random.Float(num2, num3);
                    }
                    break;
                }
                default: throw new InvalidOperationException("Unknown precipitation type.");
            }
        }
    }
}