using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;

namespace Phytomana {
    /// <summary>
    /// 夜影花：与日耀花互补的被动产魔花，仅在夜晚（天空光照低于阈值）缓慢产出魔力。
    /// 产出速率与上限由配置驱动（NightshadeManaRate / NightshadeMaxMana）。
    /// </summary>
    public class TileNightshadeFlower : TileGeneratingFlower {
        public const float DefaultManaRate = 0.9f;
        public const float DefaultMaxMana = 800f;

        /// <summary>最终产能倍率：与配置速率相乘得到实际产魔（本花最终产能 ×0.35）。</summary>
        public const float RateMultiplier = 0.35f;
        public const double ProductionParticleInterval = 4.0;
        public const float NightSkyLightThreshold = 0.35f;

        public SubsystemSky m_subsystemSky;

        public SubsystemParticles m_subsystemParticles;

        public double m_nextProductionParticleTime;

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("NightshadeFlower", PhytoConfig.Instance.NightshadeMaxMana);

        public TileNightshadeFlower(Point3 position) : base(position) { }

        public override void OnPlaced() {
            m_nextProductionParticleTime = TotalTime;
        }

        public override void OnChunkLoad() {
            base.OnChunkLoad();
            m_nextProductionParticleTime = TotalTime;
        }

        public override float GetProductionRate() {
            return IsProducing ? PhytoConfig.Instance.NightshadeManaRate * RateMultiplier : 0f;
        }

        public override void FlowerTick() {
            ResolveSubsystems();
            double time = TotalTime;
            bool isNight = m_subsystemSky.SkyLightIntensity < NightSkyLightThreshold;
            if (isNight && !ManaStorage.IsFull) {
                if (State != FlowerState.Working) {
                    SetState(FlowerState.Working);
                }
                GenerateMana(GetProductionRate() * DeltaTime);
                if (time >= m_nextProductionParticleTime) {
                    m_nextProductionParticleTime = time + ProductionParticleInterval;
                    SpawnParticle(new Color(150, 100, 220));
                }
            }
            else if (State != FlowerState.Idle) {
                SetState(FlowerState.Idle);
            }
        }

        public void SpawnParticle(Color color) {
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(Position.X + 0.5f, Position.Y + 0.8f, Position.Z + 0.5f),
                0.6f,
                1.6f,
                color
            ));
        }

        public void ResolveSubsystems() {
            if (m_subsystemSky != null) {
                return;
            }
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
        }
    }
}