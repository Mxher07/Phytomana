using System;
using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 黏土沙馨：消耗魔力将工作范围内的沙子方块变为粘土（粘土以物品形式掉落，而非替换为粘土块）。
    /// 工作范围：以花为中心水平 ±4、竖直 ±3 的 9×6×9 长方体。
    /// 内部魔力缓存 368mn；每次转化 1 格沙消耗 20mn，并放两组粒子：
    /// 黄色 0.35f 大小、存活 1.5f，从花根飞向目标沙块；0.25s 后淡灰色同款粒子从目标飞回花根。
    /// 每 5 tick（0.25 秒）工作一次。可被绑定模式法杖开关机，默认开机。
    /// 取魔方式与其余功能花一致：范围搜寻魔法池；亦接受发射器主动供魔。
    /// </summary>
    public class TileClaySandFlower : TileFunctionalFlower {
        public const float DefaultMaxMana = 368f;
        public const float ManaPerConversion = 20f;
        public const double WorkInterval = 0.25;

        public const int HorizontalRange = 4;
        public const int VerticalRange = 3;

        /// <summary>粒子飞行时长。</summary>
        public const float ParticleFlightTime = 1.5f;

        /// <summary>回飞粒子相对目标时刻的延迟（秒）。</summary>
        public const double ReturnDelay = 0.25;

        /// <summary>开关机状态（存档保存，默认开机）。</summary>
        public bool m_powered = true;

        /// <summary>待结算的回飞粒子（0.25s 后从目标飞回花根）。</summary>
        public double m_returnParticleTime;

        /// <summary>回飞粒子的出发点（上一次转化的目标沙块）。</summary>
        public Point3 m_lastTargetCell;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemMana m_subsystemMana;

        public ManaNetworkManager m_network;

        public List<IManaReceiver> m_receiverBuffer = [];

        public int m_sandIndex;

        public int m_clayIndex;

        public const float PoolSearchRange = 11f;

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("ClaySandFlower", DefaultMaxMana);

        public TileClaySandFlower(Point3 position) : base(position) { }

        public void TogglePower(ComponentPlayer player) {
            m_powered = !m_powered;
            string key = m_powered ? "ClaySandPowerOn" : "ClaySandPowerOff";
            player?.ComponentGui.DisplaySmallMessage(
                LanguageControl.Get("ClaySandMessages", key), Color.White, false, false);
        }

        public override void SaveData(ValuesDictionary values) {
            base.SaveData(values);
            values.SetValue("Powered", m_powered);
        }

        public override void LoadData(ValuesDictionary values) {
            base.LoadData(values);
            m_powered = values.GetValue("Powered", true);
        }

        public override void FlowerTick() {
            ResolveSubsystems();
            if (!m_powered) {
                return;
            }
            double time = TotalTime;
            SettleReturnParticles(time);
            if (time < m_cooldown) {
                return;
            }
            m_cooldown = time + WorkInterval;
            // 未绑链时先自行从魔法池取食（储满即停）
            if (!HasIncomingLink() && !ManaStorage.IsFull) {
                TryDrawFromPool();
            }
            if (ManaStorage.Current < ManaPerConversion) {
                return;
            }
            Point3? target = FindNearestSand();
            if (target == null) {
                return;
            }
            Point3 targetCell = target.Value;
            ManaStorage.Take(ManaPerConversion);
            // 沙子 → 粘土（粘土以物品形式掉落，而非粘土块替换）
            Scheduler.m_subsystemTerrain.DestroyCell(
                0,
                targetCell.X,
                targetCell.Y,
                targetCell.Z,
                Terrain.MakeBlockValue(m_clayIndex),
                false,
                false
            );
            Vector3 flowerCenter = new(Position.X + 0.5f, Position.Y + 0.5f, Position.Z + 0.5f);
            Vector3 targetCenter = new(targetCell.X + 0.5f, targetCell.Y + 0.5f, targetCell.Z + 0.5f);
            // 黄色粒子：从花根飞向目标沙块
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                flowerCenter,
                0.35f,
                ParticleFlightTime,
                new Color(255, 200, 50),
                targetCenter,
                1
            ));
            // 0.25s 后淡灰色粒子：从目标飞回花根
            m_lastTargetCell = targetCell;
            m_returnParticleTime = time + ReturnDelay;
            SetState(FlowerState.Working);
        }

        /// <summary>结算延迟的回飞粒子（淡灰色，从目标沙块飞回花根）。</summary>
        public void SettleReturnParticles(double time) {
            if (m_returnParticleTime <= 0.0 || time < m_returnParticleTime) {
                return;
            }
            m_returnParticleTime = 0.0;
            Vector3 flowerCenter = new(Position.X + 0.5f, Position.Y + 0.5f, Position.Z + 0.5f);
            Vector3 targetCenter = new(m_lastTargetCell.X + 0.5f, m_lastTargetCell.Y + 0.5f, m_lastTargetCell.Z + 0.5f);
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                targetCenter,
                0.35f,
                ParticleFlightTime,
                Color.LightGray,
                flowerCenter,
                1
            ));
        }

        /// <summary>工作范围内离花最近的一格沙子；没有则返回 null。</summary>
        public Point3? FindNearestSand() {
            Point3? best = null;
            int bestDistance = int.MaxValue;
            for (int dx = -HorizontalRange; dx <= HorizontalRange; dx++) {
                for (int dz = -HorizontalRange; dz <= HorizontalRange; dz++) {
                    for (int dy = -VerticalRange; dy <= VerticalRange; dy++) {
                        Point3 cell = new(Position.X + dx, Position.Y + dy, Position.Z + dz);
                        int value = Scheduler.m_subsystemTerrain.Terrain.GetCellValue(cell.X, cell.Y, cell.Z);
                        if (Terrain.ExtractContents(value) != m_sandIndex) {
                            continue;
                        }
                        int distance = dx * dx + dy * dy + dz * dz;
                        if (distance < bestDistance) {
                            bestDistance = distance;
                            best = cell;
                        }
                    }
                }
            }
            return best;
        }

        /// <summary>是否有发射器链路指向自己（被绑链后不再自行取食）。</summary>
        public bool HasIncomingLink() {
            foreach (ManaLink link in m_subsystemMana.m_links) {
                if (link.To == Position) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>未被绑链时自行从魔法池取食（储满即停）。</summary>
        public void TryDrawFromPool() {
            ManaPool best = null;
            float bestDistance = float.MaxValue;
            m_network.GetActiveReceivers(m_receiverBuffer);
            foreach (IManaReceiver receiver in m_receiverBuffer) {
                if (receiver is not ManaPool pool
                    || pool.Position == Position
                    || pool.ManaStorage.IsEmpty) {
                    continue;
                }
                float dx = pool.Position.X - Position.X;
                float dy = pool.Position.Y - Position.Y;
                float dz = pool.Position.Z - Position.Z;
                if (MathF.Abs(dx) > PoolSearchRange
                    || MathF.Abs(dy) > PoolSearchRange
                    || MathF.Abs(dz) > PoolSearchRange) {
                    continue;
                }
                float distance = dx * dx + dy * dy + dz * dz;
                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = pool;
                }
            }
            if (best == null) {
                return;
            }
            float take = MathF.Min(best.ManaStorage.Current, ManaStorage.Free);
            if (take <= 0f) {
                return;
            }
            best.ManaStorage.Take(take);
            ManaStorage.TryAdd(take);
        }

        public void ResolveSubsystems() {
            if (m_subsystemParticles != null) {
                return;
            }
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemMana = Project.FindSubsystem<SubsystemMana>(true);
            m_network = Project.FindSubsystem<ManaNetworkManager>(true);
            m_sandIndex = BlocksManager.GetBlockIndex<SandBlock>();
            m_clayIndex = BlocksManager.GetBlockIndex<ClayBlock>();
        }
    }
}
