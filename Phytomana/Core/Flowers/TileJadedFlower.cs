using System;
using System.Collections.Generic;
using Engine;
using Game;
using Phytomana.Api;


namespace Phytomana {
    /// <summary>
    /// 翡翠菜：消耗魔力在周围快速生成随机颜色的须弥花（功能花）。
    /// 内部魔力缓存 135mn；工作范围：水平 ±4（9×9），垂直 -2~+6（以花所在平面为基准）。
    /// 每生成一株消耗 35mn，每 30 tick（1.5 秒）工作一次；
    /// 所选位置不可用（非泥土/草地表面或格非空气）时直接跳过本轮，不重试。
    /// 接收魔力池投递，也可被发射器绑链供应。
    /// </summary>
    public class TileJadedFlower : TileFunctionalFlower {
        public const float DefaultMaxMana = 260f;
        public const float ManaPerFlower = 35f;
        public const double WorkInterval = 1.5;
        public const int HorizontalRange = 4;
        public const int VerticalTop = 6;
        public const int VerticalBottom = -2;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemMana m_subsystemMana;

        public ManaNetworkManager m_network;

        public List<IManaReceiver> m_receiverBuffer = [];

        public int m_sumeruFlowerIndex;

        public System.Random m_random = new();

        /// <summary>开关机状态（存档保存，默认关机）。</summary>
        public bool m_powered;

        public const float PoolSearchRange = 11f;

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("JadedFlower", DefaultMaxMana);

        public TileJadedFlower(Point3 position) : base(position) { }

        public void TogglePower(ComponentPlayer player) {
            m_powered = !m_powered;
            string key = m_powered ? "JadedPowerOn" : "JadedPowerOff";
            player?.ComponentGui.DisplaySmallMessage(
                LanguageControl.Get("JadedMessages", key), Color.White, false, false);
        }

        public override void FlowerTick() {
            ResolveSubsystems();
            if (!m_powered) {
                return;
            }
            double time = TotalTime;
            // 未绑链时先自行从魔法池取食（储满即停），再尝试生成
            if (!HasIncomingLink() && !ManaStorage.IsFull) {
                TryDrawFromPool();
            }
            if (time < m_cooldown) {
                return;
            }
            m_cooldown = time + WorkInterval;
            if (ManaStorage.Current < ManaPerFlower) {
                return;
            }
            // 随机选一个生成位置：水平 ±4，垂直 -2~+6
            Point3 target = new(
                Position.X + m_random.Next(-HorizontalRange, HorizontalRange + 1),
                Position.Y + m_random.Next(VerticalBottom, VerticalTop + 1),
                Position.Z + m_random.Next(-HorizontalRange, HorizontalRange + 1)
            );
            int ground = m_subsystemTerrain.Terrain.GetCellContents(target.X, target.Y - 1, target.Z);
            int groundContents = Terrain.ExtractContents(ground);
            // 表面必须是泥土或草地（完整的），且目标格为空气；不可用直接跳过本轮，不重试
            if (groundContents != BlocksManager.GetBlockIndex<DirtBlock>()
                && groundContents != BlocksManager.GetBlockIndex<GrassBlock>()) {
                return;
            }
            if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(target.X, target.Y, target.Z)) != 0) {
                return;
            }
            ManaStorage.Take(ManaPerFlower);
            int colorIndex = m_random.Next(16);
            int flowerValue = Terrain.MakeBlockValue(m_sumeruFlowerIndex, 0, colorIndex << 1);
            m_subsystemTerrain.ChangeCell(target.X, target.Y, target.Z, flowerValue);
            // 随目标花色的魔法粒子：从花根飞向被生成花的位置
            Color particleColor = SumeruFlowerBlock.GetColorValue(colorIndex << 1, m_subsystemTerrain.SubsystemPalette);
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(Position.X + 0.5f, Position.Y + 0.5f, Position.Z + 0.5f),
                0.5f,
                1.5f,
                particleColor,
                new Vector3(target.X + 0.5f, target.Y + 0.5f, target.Z + 0.5f),
                1
            ));
        }

        /// <summary>是否有发射器链路指向自己。</summary>
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
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(Position.X + 0.5f, Position.Y + 0.2f, Position.Z + 0.5f),
                0.6f,
                1.6f,
                new Color(150, 100, 220)
            ));
        }

        public void ResolveSubsystems() {
            if (m_subsystemParticles != null) {
                return;
            }
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemMana = Project.FindSubsystem<SubsystemMana>(true);
            m_network = Project.FindSubsystem<ManaNetworkManager>(true);
            m_sumeruFlowerIndex = BlocksManager.GetBlockIndex<SumeruFlowerBlock>();
        }
    }
}