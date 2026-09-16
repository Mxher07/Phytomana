using System;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 暴食花：吞食台面上掉落的食物（成组逐件扣），按营养价值消化一段时间后
    /// 一次性产出魔力。营养价值越高，消化越久、产魔越多。
    /// </summary>
    public class TileGourmaryllisFlower : TileGeneratingFlower {
        public const float DefaultManaPerNutrition = 12f;
        public const float DefaultMaxMana = 1000f;

        /// <summary>最终产能倍率：与「营养 × 每点产魔」相乘得到实际产魔（本花最终产能 ×1.25）。</summary>
        public const float ProductionMultiplier = 1.25f;
        public const double DigestionBaseTime = 3.0;
        public const double DigestionTimePerNutrition = 0.25;
        public const double ChewParticleInterval = 1.0;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemAudio m_subsystemAudio;

        /// <summary>当前正在消化的食物将产出的魔力（需随存档保存）。</summary>
        public float m_pendingMana;

        /// <summary>本次消化的总时长（秒），供法杖按 平均产出/时长 显示产能速率。</summary>
        public double m_digestionDuration;

        public double m_nextChewParticleTime;

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("GourmaryllisFlower", PhytoConfig.Instance.GourmaryllisMaxMana);

        public TileGourmaryllisFlower(Point3 position) : base(position) { }

        public override float GetProductionRate() {
            // 产能速率 = 本餐产出 / 消化时长（进食型花朵的产魔是离散的，按平均值展示才符合直觉）
            return State == FlowerState.Working && m_digestionDuration > 0.0
                ? (float)(m_pendingMana / m_digestionDuration)
                : 0f;
        }

        public override void FlowerTick() {
            ResolveSubsystems();
            double time = TotalTime;
            if (State == FlowerState.Working) {
                // 消化中：到时结算产魔
                if (time >= m_timer) {
                    GenerateMana(m_pendingMana);
                    m_pendingMana = 0f;
                    SetState(FlowerState.Idle);
                    SpawnParticle(new Color(102, 204, 255));
                }
                else if (time >= m_nextChewParticleTime) {
                    m_nextChewParticleTime = time + ChewParticleInterval;
                    SpawnParticle(new Color(250, 170, 60));
                }
                return;
            }
            if (ManaStorage.IsFull) {
                return;
            }
            TryEatFood(time);
        }

        public void TryEatFood(double time) {
            Pickable best = null;
            float bestNutrition = 0f;
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                if (!IsPickableInCell(pickable, Position)) {
                    continue;
                }
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(pickable.Value)];
                float nutrition = block.GetNutritionalValue(pickable.Value);
                if (nutrition <= 0f) {
                    continue;
                }
                if (best == null || nutrition > bestNutrition) {
                    best = pickable;
                    bestNutrition = nutrition;
                }
            }
            if (best == null) {
                return;
            }
            // 只吃一件：成组掉落物逐件扣除（与进食逻辑一致）
            best.Count = MathUtils.Max(best.Count - 1, 0);
            if (best.Count == 0) {
                best.ToRemove = true;
            }
            m_pendingMana = bestNutrition * PhytoConfig.Instance.GourmaryllisManaPerNutrition * ProductionMultiplier;
            m_digestionDuration = DigestionBaseTime + bestNutrition * DigestionTimePerNutrition;
            m_timer = time + m_digestionDuration;
            m_nextChewParticleTime = time + ChewParticleInterval;
            SetState(FlowerState.Working);
        }

        public void SpawnParticle(Color color) {
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(Position.X + 0.5f, Position.Y + 0.8f, Position.Z + 0.5f),
                0.6f,
                1.2f,
                color
            ));
        }

        public void ResolveSubsystems() {
            if (m_subsystemPickables != null) {
                return;
            }
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
        }

        public override void SaveData(ValuesDictionary values) {
            base.SaveData(values);
            values.SetValue("PendingMana", m_pendingMana);
            values.SetValue("DigestionDuration", m_digestionDuration);
        }

        public override void LoadData(ValuesDictionary values) {
            base.LoadData(values);
            m_pendingMana = values.GetValue("PendingMana", 0f);
            m_digestionDuration = values.GetValue("DigestionDuration", 0.0);
        }
    }
}