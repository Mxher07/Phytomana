using System;
using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;
using Random = Game.Random;

namespace Phytomana {
    /// <summary>
    /// 泉沫珠：吸收周围静水产魔（产魔花）。投递由魔力网络负责。
    /// </summary>
    public class TileWaterDonFlower : TileGeneratingFlower {
        public const double ScanInterval = 1.0;
        public const double AbsorbDuration = 3.0;
        public const double ParticleInterval = 7.5;
        public const float DefaultManaRate = 7f / 1.65f;
        public const float DefaultMaxMana = 240f;

        public SubsystemParticles m_subsystemParticles;

        public Random m_random = new();

        public double m_nextScanTime;

        public double m_absorbEndTime;

        public double m_nextParticleTime;

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("WaterDonFlower", PhytoConfig.Instance.WaterDonMaxMana);

        public TileWaterDonFlower(Point3 position) : base(position) { }

        public override void OnPlaced() {
            InitializeTimers();
        }

        public override void OnChunkLoad() {
            InitializeTimers();
        }

        public override float GetProductionRate() => IsProducing ? PhytoConfig.Instance.WaterDonManaRate : 0f;

        public void InitializeTimers() {
            m_nextScanTime = TotalTime;
            m_nextParticleTime = TotalTime + ParticleInterval;
        }

        public override void FlowerTick() {
            ResolveSubsystems();
            double time = TotalTime;
            if (State == FlowerState.Working) {
                if (time >= m_absorbEndTime) {
                    SetState(FlowerState.Idle);
                    m_nextScanTime = time;
                }
                else {
                    GenerateMana(PhytoConfig.Instance.WaterDonManaRate * DeltaTime);
                }
            }
            else if (time >= m_nextScanTime) {
                m_nextScanTime = time + ScanInterval;
                TryEatWater();
            }
            if (time >= m_nextParticleTime) {
                m_nextParticleTime = time + ParticleInterval;
                SpawnManaParticle(Position, 0.3f);
            }
        }

        public void TryEatWater() {
            if (ManaStorage.IsFull) {
                return;
            }
            Terrain terrain = Scheduler.m_subsystemTerrain.Terrain;
            List<Point3> staticWaterCells = [];
            for (int dx = -1; dx <= 1; dx++) {
                for (int dz = -1; dz <= 1; dz++) {
                    int x = Position.X + dx;
                    int z = Position.Z + dz;
                    int value = terrain.GetCellValue(x, Position.Y, z);
                    if (Terrain.ExtractContents(value) == WaterBlock.Index
                        && FluidBlock.GetLevel(Terrain.ExtractData(value)) == 0) {
                        staticWaterCells.Add(new Point3(x, Position.Y, z));
                    }
                }
            }
            if (staticWaterCells.Count == 0) {
                return;
            }
            Point3 waterCell = staticWaterCells[m_random.Int(staticWaterCells.Count)];
            Scheduler.m_subsystemTerrain.DestroyCell(0, waterCell.X, waterCell.Y, waterCell.Z, 0, false, false);
            SpawnManaParticle(waterCell, 0.3f, 0.25f, 1.5f, Color.SkyBlue);
            SetState(FlowerState.Working);
            m_absorbEndTime = TotalTime + AbsorbDuration;
        }

        public void SpawnManaParticle(Point3 point, float yOffset) {
            SpawnManaParticle(point, yOffset, 0.25f, 1.5f, Color.SkyBlue);
        }

        public void SpawnManaParticle(Point3 point, float yOffset, float size, float duration, Color color) {
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(point.X + 0.5f, point.Y + yOffset, point.Z + 0.5f),
                size,
                duration,
                color
            ));
        }

        public void ResolveSubsystems() {
            if (m_subsystemParticles != null) {
                return;
            }
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
        }

        public override void SaveData(ValuesDictionary values) {
            base.SaveData(values);
            values.SetValue("AbsorbRemaining", State == FlowerState.Working ? Math.Max(0.0, m_absorbEndTime - TotalTime) : 0.0);
        }

        public override void LoadData(ValuesDictionary values) {
            base.LoadData(values);
            double remaining = values.GetValue("AbsorbRemaining", 0.0);
            if (remaining > 0.0) {
                m_absorbEndTime = TotalTime + remaining;
            }
        }
    }
}
