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

        /// <summary>吞噬「完整岩浆」（桶可装、level=0）时的最终产能倍率：正常产能 ×1.5。</summary>
        public const float FullMagmaMultiplier = 1.5f;

        /// <summary>吞噬非完整岩浆（流动/部分）时的最终产能倍率：正常产能 ×0.15（不享受 ×1.5）。</summary>
        public const float PartialMagmaMultiplier = 0.15f;
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

        /// <summary>最近一次吞噬获得的魔力（法杖按平均循环周期展示产能用；瞬态不存档）。</summary>
        public float m_lastAbsorbMana;

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("ThermalilyFlower", PhytoConfig.Instance.ThermalilyMaxMana);

        public TileThermalilyFlower(Point3 position) : base(position) { }

        /// <summary>产能速率 = 单次吞噬产出 / 吞噬循环周期（扫描 1.5s + 凝固表演 2.0s），供法杖展示。</summary>
        public override float GetProductionRate() {
            return State == FlowerState.Working && m_lastAbsorbMana > 0f
                ? m_lastAbsorbMana / (float)(ScanInterval + AbsorbVisualTime)
                : 0f;
        }

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
                int cellValue = Scheduler.m_subsystemTerrain.Terrain.GetCellValue(cell.X, cell.Y, cell.Z);
                if (Terrain.ExtractContents(cellValue) != m_magmaIndex) {
                    continue;
                }
                // 「桶可装的完整岩浆」（level=0）享受正常产能 ×1.5；
                // 流动的非完整岩浆产能 ×0.15，且不享受 ×1.5 —— 防止灌无尽薄岩浆白嫖。
                float multiplier = FluidBlock.GetLevel(Terrain.ExtractData(cellValue)) == 0
                    ? FullMagmaMultiplier
                    : PartialMagmaMultiplier;
                float absorb = PhytoConfig.Instance.ThermalilyManaPerMagma * multiplier;
                // 岩浆凝固为石头，花朵吞噬产魔
                Scheduler.m_subsystemTerrain.ChangeCell(cell.X, cell.Y, cell.Z, Terrain.MakeBlockValue(m_stoneIndex));
                m_lastAbsorbMana = absorb;
                GenerateMana(absorb);
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