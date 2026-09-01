using Engine;
using Engine.Graphics;

namespace Game {
    public class BurntDebrisParticleSystem : ParticleSystem<BurntDebrisParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public Vector3 Velocity;

            public float TimeToLive;
        }

        public Random m_random = new();

        public SubsystemTerrain m_subsystemTerrain;

        public BurntDebrisParticleSystem(SubsystemTerrain terrain, int x, int y, int z) : this(terrain, new Vector3(x + 0.5f, y + 0.5f, z + 0.5f)) { }

        public BurntDebrisParticleSystem(SubsystemTerrain terrain, Vector3 position) : base(15) {
            m_subsystemTerrain = terrain;
            Texture = ContentManager.Get<Texture2D>("Textures/FireParticle");
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
            TextureSlotsCount = 3;
            Color white = Color.White;
            float num4 = LightingManager.LightIntensityByLightValue[x];
            white *= num4;
            white.A = 255;
            for (int i = 0; i < Particles.Length; i++) {
                Particle obj = Particles[i];
                obj.IsActive = true;
                obj.Position = position + 0.5f * new Vector3(m_random.Float(-1f, 1f), m_random.Float(-1f, 1f), m_random.Float(-1f, 1f));
                obj.Color = white;
                obj.Size = new Vector2(0.5f);
                obj.TimeToLive = m_random.Float(0.75f, 2f);
                obj.Velocity = new Vector3(3f * m_random.Float(-1f, 1f), 2f * m_random.Float(-1f, 1f), 3f * m_random.Float(-1f, 1f));
                obj.TextureSlot = 8;
            }
        }

        public override bool Simulate(float dt) {
            dt = Math.Clamp(dt, 0f, 0.1f);
            float num = MathF.Pow(0.04f, dt);
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
                        Plane plane = terrainRaycastResult.Value.CellFace.CalculatePlane();
                        vector = position;
                        if (plane.Normal.X != 0f) {
                            particle.Velocity *= new Vector3(-0.1f, 0.1f, 0.1f);
                        }
                        if (plane.Normal.Y != 0f) {
                            particle.Velocity *= new Vector3(0.1f, -0.1f, 0.1f);
                        }
                        if (plane.Normal.Z != 0f) {
                            particle.Velocity *= new Vector3(0.1f, 0.1f, -0.1f);
                        }
                    }
                    particle.Position = vector;
                    particle.Velocity.Y += -10f * dt;
                    particle.Velocity *= num;
                    particle.Color *= MathUtils.Saturate(particle.TimeToLive);
                    particle.TextureSlot = (int)(8.99f * MathUtils.Saturate(2f - particle.TimeToLive));
                }
                else {
                    particle.IsActive = false;
                }
            }
            return !flag;
        }
    }
}