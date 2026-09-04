using Engine;
using Phytomana.Api;

namespace Phytomana {
    /// <summary>
    /// 魔法池的逐坐标魔力节点。生存战争中 Block 为单例，逐位置状态由本节点承载，
    /// 生命周期由 SubsystemManaPoolBehavior 管理。
    /// </summary>
    public class ManaPool : IManaReceiver {
        public const float MaxMana = 3800f;

        public Point3 Position { get; }

        public ManaStorage ManaStorage { get; }

        public ManaPool(Point3 position) {
            Position = position;
            ManaStorage = new ManaStorage(MaxMana);
        }
    }
}
