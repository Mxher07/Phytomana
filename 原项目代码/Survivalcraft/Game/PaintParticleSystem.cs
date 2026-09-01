using Engine;

namespace Game {
    public class PaintParticleSystem : ParticleSystem<PaintParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public Vector3 Velocity;

            public float TimeToLive;

            public float HighDampingFactor;

            public bool NoGravity;

            public float Alpha;
        }

        public Random m_random = new();

        public SubsystemTerrain m_subsystemTerrain;

        public Color m_color;

        public PaintParticleSystem(SubsystemTerrain terrain, Vector3 position, Vector3 normal, Color color) : base(20) {
            m_subsystemTerrain = terrain;
            Texture = terrain.Project.FindSubsystem<SubsystemBlocksTexture>(true).BlocksTexture;
            int num = Terrain.ToCell(position.X);
            int num2 = Terrain.ToCell(position.Y);
            int num3 = Terrain.ToCell(position.Z);
            int x = 0;
            x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num + 1, num2, num3));
            x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num - 1, num2, num3));
            x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2 + 1, num3));
            x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2 - 1, num3));
            x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2, num3 + 1));
            x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2, num3 - 1));
            TextureSlotsCount = 16;
            float s = LightingManager.LightIntensityByLightValue[x];
            m_color = color * s;
            m_color.A = color.A;
            Vector3 vector = Vector3.Normalize(Vector3.Cross(normal, new Vector3(0.37f, 0.15f, 0.17f)));
            Vector3 v = Vector3.Normalize(Vector3.Cross(normal, vector));
            for (int i = 0; i < Particles.Length; i++) {
                Particle obj = Particles[i];
                obj.IsActive = true;
                Vector2 vector2 = new(m_random.Float(-1f, 1f), m_random.Float(-1f, 1f));
                obj.Position = position + 0.4f * (vector2.X * vector + vector2.Y * v) + 0.03f * normal;
                obj.Color = m_color;
                obj.Size = new Vector2(m_random.Float(0.025f, 0.035f));
                obj.TimeToLive = m_random.Float(0.5f, 1.5f);
                obj.Velocity = 1f * (vector2.X * vector + vector2.Y * v) + m_random.Float(-3f, 0.5f) * normal;
                obj.TextureSlot = 15;
                obj.Alpha = m_random.Float(0.3f, 0.6f);
            }
        }

        public override bool Simulate(float dt) {
            dt = Math.Clamp(dt, 0f, 0.1f);
            float num = MathF.Pow(0.2f, dt);
            float num2 = MathF.Pow(1E-07f, dt);
            bool flag = false;
            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (!particle.IsActive) {
                    continue;
                }
                flag = true;
                particle.TimeToLive -= dt;
                if (particle.TimeToLive > 0f) {
                    Vector3 position = particle.Position;
                    Vector3 vector = position + particle.Velocity * dt;
                    TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(
                        position,
                        vector,
                        false,
                        true,
                        (value, _) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable_(value)
                    );
                    if (terrainRaycastResult.HasValue) {
                        particle.Velocity = Vector3.Zero;
                        particle.Position = terrainRaycastResult.Value.HitPoint(0.03f);
                        particle.HighDampingFactor = m_random.Float(0.5f, 1f);
                        if (terrainRaycastResult.Value.CellFace.Face >= 4) {
                            particle.NoGravity = true;
                        }
                    }
                    else {
                        particle.Position = vector;
                    }
                    if (!particle.NoGravity) {
                        particle.Velocity.Y += -9.81f * dt;
                    }
                    particle.Velocity *= particle.HighDampingFactor > 0f ? num2 * particle.HighDampingFactor : num;
                    particle.Color = m_color * MathUtils.Saturate(1.5f * particle.TimeToLive * particle.Alpha);
                }
                else {
                    particle.IsActive = false;
                }
            }
            return !flag;
        }
    }
}