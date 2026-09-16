using System;
using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 传输箱洋：无视方块瞬间运输掉落物（功能花）。掉落在 5×5×5 工作范围内的掉落物，
    /// 在一阵白烟后瞬间出现在法杖绑定方块（或同侧魔力池）之上。
    /// 绑定方式与魔力池相同：法杖绑定模式下把发射器绑到目的地方块，花自动寻路到目标。
    /// 法杖工作模式指向本花：显示消息并放一枚 1.5f 大小、存活 2f 的白色粒子飞向绑定目标，无绑定则不产生粒子。
    /// 内部魔力缓存 500mn；基础运输延迟 60 tick；耗魔 = 基础 2 × 堆叠数量 × 欧几里得距离。
    /// 取魔方式：范围搜寻魔力池；亦接受发射器主动供魔。
    /// 注意：不要把绑定目标设在工作范围内的方块，否则会出现无限循环传送的无意义耗魔。
    /// </summary>
    public class TileTransChestV2Flower : TileFunctionalFlower {
        public const float DefaultMaxMana = 500f;
        public const float BaseTransportManaCost = 2f;
        public const int RangeHalf = 2;
        public const double BaseTransportDelayTicks = 60;
        public const double CheckInterval = 0.25;

        /// <summary>绑定目标（法杖绑定模式下写入，存档保存）。</summary>
        public Point3? m_bindTarget;

        /// <summary>待运掉落物登记：到期后瞬间出现在绑定目标上方（不随存档保存，读档后重新扫描）。</summary>
        public class PendingTransfer {
            public double Time;
            public int Value;
            public int Count;
        }

        public List<PendingTransfer> m_pendingTransfers = [];

        public SubsystemParticles m_subsystemParticles;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemTerrain m_subsystemTerrain;

        public const float PoolSearchRange = 11f;

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("TransChestV2Flower", DefaultMaxMana);

        public TileTransChestV2Flower(Point3 position) : base(position) { }

        public override void SaveData(ValuesDictionary values) {
            base.SaveData(values);
            if (m_bindTarget.HasValue) {
                values.SetValue("BindTargetX", m_bindTarget.Value.X);
                values.SetValue("BindTargetY", m_bindTarget.Value.Y);
                values.SetValue("BindTargetZ", m_bindTarget.Value.Z);
            }
        }

        public override void LoadData(ValuesDictionary values) {
            base.LoadData(values);
            if (values.ContainsKey("BindTargetX")) {
                int x = values.GetValue<int>("BindTargetX", 0);
                int y = values.GetValue<int>("BindTargetY", 0);
                int z = values.GetValue<int>("BindTargetZ", 0);
                m_bindTarget = new Point3(x, y, z);
            }
        }

        /// <summary>法杖绑定模式下把发射器绑到目的地方块（写入绑定目标）。</summary>
        public void SetBindTarget(Point3 target, ComponentPlayer player) {
            m_bindTarget = target;
            player?.ComponentGui.DisplaySmallMessage(
                LanguageControl.Get("TransChestV2Messages", "BindSet"), Color.Green, false, false);
        }

        /// <summary>
        /// 法杖工作模式指向本花：显示消息并放一枚 1.5f 大小、存活 2f 的白色粒子飞向绑定目标；
        /// 无绑定则只提示消息不产生粒子。
        /// </summary>
        public void OnStaffCheck(ComponentPlayer player) {
            if (m_bindTarget == null) {
                player?.ComponentGui.DisplaySmallMessage(
                    LanguageControl.Get("TransChestV2Messages", "NoTarget"), Color.White, false, false);
                return;
            }
            player?.ComponentGui.DisplaySmallMessage(
                string.Format(
                    LanguageControl.Get("TransChestV2Messages", "TargetFormat"),
                    m_bindTarget.Value.X,
                    m_bindTarget.Value.Y,
                    m_bindTarget.Value.Z
                ),
                Color.White,
                false,
                false
            );
            Vector3 muzzle = new(Position.X + 0.5f, Position.Y + 0.8f, Position.Z + 0.5f);
            Vector3 targetCenter = new(m_bindTarget.Value.X + 0.5f, m_bindTarget.Value.Y + 0.5f, m_bindTarget.Value.Z + 0.5f);
            m_subsystemParticles?.AddParticleSystem(new ManaParticleSystem(
                muzzle,
                1.5f,
                2f,
                Color.White,
                targetCenter,
                1
            ));
        }

        public override void FlowerTick() {
            ResolveSubsystems();
            double time = TotalTime;
            if (time < m_cooldown) {
                return;
            }
            m_cooldown = time + CheckInterval;
            // 未绑链时先自行从魔法池取食（储满即停）
            if (!HasIncomingLink() && !ManaStorage.IsFull) {
                TryDrawFromPool(PoolSearchRange);
            }
            SettleTransfers(time);
            ScanAndQueueTransfers(time);
        }

        /// <summary>到期运输：把登记在案的掉落物从原格移除，瞬移到绑定目标上方。</summary>
        public void SettleTransfers(double time) {
            for (int i = m_pendingTransfers.Count - 1; i >= 0; i--) {
                PendingTransfer pending = m_pendingTransfers[i];
                if (time < pending.Time) {
                    continue;
                }
                m_pendingTransfers.RemoveAt(i);
                Point3 targetCell = m_bindTarget ?? Position;
                SpawnPickableAboveBlock(targetCell, pending.Value, pending.Count);
            }
        }

        /// <summary>在目标方块顶面生成掉落物（Y+1 格，悬浮于顶面之上）。</summary>
        public void SpawnPickableAboveBlock(Point3 cell, int value, int count) {
            Vector3 position = new(cell.X + 0.5f, cell.Y + 1.0f, cell.Z + 0.5f);
            m_subsystemPickables.AddPickable(value, count, position, null, null);
        }

        /// <summary>
        /// 扫描工作范围内的掉落物，按 耗魔 = 基础2 × 堆叠数 × 欧几里得距离 登记运输；
        /// 魔量不足时跳过（本轮不扣魔、不登记，下轮重试）。
        /// </summary>
        public void ScanAndQueueTransfers(double time) {
            if (m_bindTarget == null) {
                return;
            }
            Vector3 flowerCenter = new(Position.X + 0.5f, Position.Y + 0.5f, Position.Z + 0.5f);
            Vector3 targetCenter = new(m_bindTarget.Value.X + 0.5f, m_bindTarget.Value.Y + 0.5f, m_bindTarget.Value.Z + 0.5f);
            float distance = Vector3.Distance(flowerCenter, targetCenter);
            if (distance < 0.01f) {
                return;
            }
            Vector3 targetTop = targetCenter + new Vector3(0f, 0.5f, 0f);
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                // 5×5×5 工作范围（以花为中心）
                Vector3 position = pickable.Position;
                if (MathF.Abs(position.X - flowerCenter.X) > RangeHalf
                    || MathF.Abs(position.Y - flowerCenter.Y) > RangeHalf
                    || MathF.Abs(position.Z - flowerCenter.Z) > RangeHalf) {
                    continue;
                }
                int stackCount = Math.Max(1, pickable.Count);
                float manaCost = BaseTransportManaCost * stackCount * distance;
                if (ManaStorage.Current < manaCost) {
                    continue;
                }
                ManaStorage.Take(manaCost);
                // 移除原掉落物（登记新瞬移）
                pickable.ToRemove = true;
                m_pendingTransfers.Add(new PendingTransfer {
                    Time = time + BaseTransportDelayTicks / 20.0,
                    Value = pickable.Value,
                    Count = stackCount
                });
                // 白烟粒子：从掉落物位置升起
                m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                    pickable.Position,
                    0.4f,
                    0.8f,
                    Color.White
                ));
                // 白色追踪粒子：从花根飞向目标顶面
                Vector3 muzzle = new(Position.X + 0.5f, Position.Y + 0.8f, Position.Z + 0.5f);
                m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                    muzzle,
                    0.3f,
                    1.2f,
                    Color.White,
                    targetTop,
                    1
                ));
            }
        }

        public void ResolveSubsystems() {
            if (m_subsystemParticles != null) {
                return;
            }
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
        }
    }
}
