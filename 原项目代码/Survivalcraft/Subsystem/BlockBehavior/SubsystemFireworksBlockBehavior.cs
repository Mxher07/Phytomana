using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemFireworksBlockBehavior : SubsystemBlockBehavior, IUpdateable {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemProjectiles m_subsystemProjectiles;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemNoise m_subsystemNoise;

        public SubsystemTime m_subsystemTime;

        public SubsystemSky m_subsystemSky;

        public SubsystemPlayers m_subsystemPlayers;

        public Random m_random = new();

        public float m_newYearCelebrationTimeRemaining;

        public const string fName = "SubsystemFireworksBlockBehavior";

        public override int[] HandledBlocks => [];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public void ExplodeFireworks(Vector3 position, int data) {
            for (int i = 0; i < 3; i++) {
                Vector3 v = new(m_random.Float(-3f, 3f), -15f, m_random.Float(-3f, 3f));
                if (m_subsystemTerrain.Raycast(position, position + v, false, true, null).HasValue) {
                    return;
                }
            }
            FireworksBlock.Shape shape = FireworksBlock.GetShape(data);
            float flickering = FireworksBlock.GetFlickering(data) ? 0.66f : 0f;
            float particleSize = FireworksBlock.GetAltitude(data) > 0 ? 1.1f : 1f;
            Color color = FireworksBlock.FireworksColors[FireworksBlock.GetColor(data)];
            m_subsystemParticles.AddParticleSystem(new FireworksParticleSystem(position, color, shape, flickering, particleSize));
            m_subsystemAudio.PlayRandomSound("Audio/FireworksPop", 1f, m_random.Float(-0.4f, 0f), position, 80f, true);
            m_subsystemNoise.MakeNoise(position, 1f, 60f);
        }

        public override void OnFiredAsProjectile(Projectile projectile) {
            int data = Terrain.ExtractData(projectile.Value);
            float num = FireworksBlock.GetAltitude(data) == 0 ? 0.8f : 1.3f;
            m_subsystemProjectiles.AddTrail(projectile, Vector3.Zero, new FireworksTrailParticleSystem());
            m_subsystemAudio.PlayRandomSound("Audio/FireworksWhoosh", 1f, m_random.Float(-0.2f, 0.2f), projectile.Position, 8f, true);
            m_subsystemNoise.MakeNoise(projectile.Position, 1f, 10f);
            m_subsystemTime.QueueGameTimeDelayedExecution(
                m_subsystemTime.GameTime + num,
                delegate {
                    if (!projectile.ToRemove) {
                        projectile.ToRemove = true;
                        ExplodeFireworks(projectile.Position, data);
                    }
                }
            );
        }

        public override bool OnHitAsProjectile(CellFace? cellFace, ComponentBody componentBody, WorldItem worldItem) => true;

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
        }

        public virtual void Update(float dt) {
            ComponentPlayer componentPlayer = m_subsystemPlayers.ComponentPlayers.Count > 0 ? m_subsystemPlayers.ComponentPlayers[0] : null;
            if (componentPlayer == null) {
                return;
            }
            if (m_newYearCelebrationTimeRemaining <= 0f
                && Time.PeriodicEvent(5.0, 0.0)
                && m_subsystemSky.SkyLightIntensity == 0f
                && !componentPlayer.ComponentSleep.IsSleeping) {
                DateTime now = DateTime.Now;
                if (now.Year > SettingsManager.NewYearCelebrationLastYear
                    && now.Month == 1
                    && now.Day == 1
                    && now.Hour == 0
                    && now.Minute < 59) {
                    SettingsManager.NewYearCelebrationLastYear = now.Year;
                    m_newYearCelebrationTimeRemaining = 180f;
                    componentPlayer.ComponentGui.DisplayLargeMessage(LanguageControl.Get(fName, "1"), LanguageControl.Get(fName, "2"), 15f, 3f);
                }
            }
            if (!(m_newYearCelebrationTimeRemaining > 0f)) {
                return;
            }
            m_newYearCelebrationTimeRemaining -= dt;
            float num = m_newYearCelebrationTimeRemaining > 10f
                ? MathUtils.Lerp(1f, 7f, 0.5f * MathF.Sin(0.25f * m_newYearCelebrationTimeRemaining) + 0.5f)
                : 20f;
            if (m_random.Float(0f, 1f) < num * dt) {
                Vector2 vector = m_random.Vector2(35f, 50f);
                Vector3 vector2 = componentPlayer.ComponentBody.Position + new Vector3(vector.X, 0f, vector.Y);
                TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(
                    new Vector3(vector2.X, 120f, vector2.Z),
                    new Vector3(vector2.X, 40f, vector2.Z),
                    false,
                    true,
                    null
                );
                if (terrainRaycastResult.HasValue) {
                    int data = 0;
                    data = FireworksBlock.SetShape(data, (FireworksBlock.Shape)m_random.Int(0, 7));
                    data = FireworksBlock.SetColor(data, m_random.Int(0, 7));
                    data = FireworksBlock.SetAltitude(data, m_random.Int(0, 1));
                    data = FireworksBlock.SetFlickering(data, m_random.Float(0f, 1f) < 0.25f);
                    int value = Terrain.MakeBlockValue(215, 0, data);
                    Vector3 position = new(
                        terrainRaycastResult.Value.CellFace.Point.X,
                        terrainRaycastResult.Value.CellFace.Point.Y + 1,
                        terrainRaycastResult.Value.CellFace.Point.Z
                    );
                    m_subsystemProjectiles.FireProjectile(
                        value,
                        position,
                        new Vector3(m_random.Float(-3f, 3f), 45f, m_random.Float(-3f, 3f)),
                        Vector3.Zero,
                        null
                    );
                }
            }
        }
    }
}