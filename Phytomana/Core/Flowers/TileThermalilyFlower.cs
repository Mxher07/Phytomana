using System;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;

namespace Phytomana {
    /// <summary>
    /// 热力百合：周期性扫描六邻域，吞噬一格岩浆产出大量魔力，岩浆随之凝固为石头。
    /// 岩浆有限，产量高但有冷却，适合 placed 在岩浆湖旁的采矿基地。
    /// </summary>
    public class TileThermalilyFlower : TileGeneratingFlower {
        public const float DefaultManaPerMagma = 250f;
        public const float DefaultMaxMana = 1200f;
        public const double ScanInterval = 1.5;
        public const double AbsorbVisualTime = 2.0;
        public const double AbsorbParticleInterval = 0.5;

        public static readonly Point3[] NeighborOffsets = [
            new(1, 0, 0),
            new(-1, 0, 0),
            new(0, 1, 0),
            new(0, -1, 0),
            new(0, 0, 1),
            new(0, 0, -1)
        ];

        public SubsystemParticles m_subsystemParticles;

        public SubsystemAudio m_subsystemAudio;

        public int m_magmaIndex;

        public int m_stoneIndex;

        public double m_nextScanTime;

        public double m_nextAbsorbParticleTime;

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("ThermalilyFlower", PhytoConfig.Instance.ThermalilyMaxMana);

        public TileThermalilyFlower(Point3 position) : base(position) { }

        public override float GetProductionRate() => 0f;

        public override void FlowerTick() {
            ResolveSubsystems();
            double time = TotalTime;
            if (State == FlowerState.Working) {
                // 吞噬表演期：喷吐粒子，到时恢复空闲
                if (time >= m_nextAbsorbParticleTime) {
                    m_nextAbsorbParticleTime = time + AbsorbParticleInterval;
                    SpawnParticle(new Color(255, 120, 30));
                }
                if (time >= m_timer) {
                    SetState(FlowerState.Idle);
                }
                return;
            }
            if (ManaStorage.IsFull) {
                return;
            }
            if (time < m_nextScanTime) {
                return;
            }
            m_nextScanTime = time + ScanInterval;
            TrySwallowMagma(time);
        }

        public void TrySwallowMagma(double time) {
            foreach (Point3 offset in NeighborOffsets) {
                Point3 cell = new(Position.X + offset.X, Position.Y + offset.Y, Position.Z + offset.Z);
                int contents = Scheduler.m_subsystemTerrain.Terrain.GetCellContents(cell);
                if (contents != m_magmaIndex) {
                    continue;
                }
                // 岩浆凝固为石头，花朵吞噬产魔
                Scheduler.m_subsystemTerrain.ChangeCell(cell.X, cell.Y, cell.Z, Terrain.MakeBlockValue(m_stoneIndex));
                GenerateMana(PhytoConfig.Instance.ThermalilyManaPerMagma);
                m_timer = time + AbsorbVisualTime;
                m_nextAbsorbParticleTime = time;
                SetState(FlowerState.Working);
                m_subsystemAudio.PlaySound("Audio/Magma", 1f, 0f, 0f, 0f);
                return;
            }
        }

        public void SpawnParticle(Color color) {
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(Position.X + 0.5f, Position.Y + 0.8f, Position.Z + 0.5f),
                0.8f,
                1.4f,
                color
            ));
        }

        public void ResolveSubsystems() {
            if (m_subsystemParticles != null) {
                return;
            }
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_magmaIndex = BlocksManager.GetBlockIndex<MagmaBlock>();
            m_stoneIndex = BlocksManager.GetBlockIndex<GraniteBlock>();
        }
    }
}