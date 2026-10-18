using Engine.Core;
using Game;
using Game.Network;
using Game.Network.Enums;
using Game.Network.Packages;
using Game.Network.Project;
using Game.Network.Serialization;

namespace Phytomana {
    /// <summary>
    /// 功能花开关机状态同步包（服务端 → 全体客户端广播）。
    /// 开关机只改内存字段 m_powered（随花存档持久化），联机下服务端为权威；
    /// 操作者本机虽有本地预测，其他已连接客户端的花实例不会自行变化，
    /// 故服务端在 TogglePower 后广播本包，客户端收到后原位改写对应花的 m_powered，
    /// 保证各端本地预测（粒子/逻辑观感）与服务端权威状态一致。
    /// 包 ID 占用模组动态槽位段 [200,249]；与其他模组撞槽会在装载期显式报冲突。
    /// </summary>
    public class FlowerPowerSyncPackage : IPackage {
        /// <summary>模组动态槽位：249（段内取尾，降低与其他模组的撞槽概率）。</summary>
        public const byte PackageId = PackageManager.DynamicPackageIdMax;

        public byte ID => PackageId;

        public Client To { get; set; }
        public Client Except { get; set; }
        public Client From { get; set; }
        public ClientState MinNeedState => ClientState.Playing;

        public Point3 Point { get; set; }
        public bool Powered { get; set; }

        public FlowerPowerSyncPackage() {
        }

        public FlowerPowerSyncPackage(Point3 point, bool powered) {
            Point = point;
            Powered = powered;
        }

        public void WriteData(PackageStreamWriter writer) {
            writer.Write(Point);
            writer.Write(Powered);
        }

        public void ReadData(PackageStreamReader reader) {
            Point = reader.ReadPoint3();
            Powered = reader.ReadBoolean();
        }

        public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer) {
            if (isServer) {
                return;
            }
            FlowerTickScheduler scheduler = projectNet.FindSubsystem<FlowerTickScheduler>(false);
            if (scheduler != null
                && scheduler.TryGetFlower(Point, out TilePhytoFlower flower)
                && flower is TileFunctionalFlower functional) {
                functional.m_powered = Powered;
            }
        }
    }
}
