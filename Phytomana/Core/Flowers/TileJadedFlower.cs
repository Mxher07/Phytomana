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

        public int m_sumeruFlowerIndex;

        public System.Random m_random = new();

        public TileJadedFlower(Point3 position) : base(position) {
            m_powered = false;
        }

        public const float PoolSearchRange = 11f;

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("JadedFlower", DefaultMaxMana);

        public override string PoweredMessageKey => "JadedMessages";

        public override string PoweredOnKey => "JadedPowerOn";

        public override string PoweredOffKey => "JadedPowerOff";

        public override void FlowerTick() {
            ResolveSubsystems();
            if (!m_powered) {
                return;
            }
            double time = TotalTime;
            // 未绑链时先自行从魔法池取食（储满即停），再尝试生成
            if (!HasIncomingLink() && !ManaStorage.IsFull) {
                TryDrawFromPool(PoolSearchRange);
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

        public void ResolveSubsystems() {
            if (m_subsystemParticles != null) {
                return;
            }
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_sumeruFlowerIndex = BlocksManager.GetBlockIndex<SumeruFlowerBlock>();
        }
    }
}