using Engine;
using Phytomana.Api;

namespace Phytomana {
    /// <summary>
    /// 魔力发射器的逐坐标魔力节点（中继器）。同时实现 IManaReceiver 与 IManaSource：
    /// 以接收器身份收取产魔源投递的魔力，再以产魔源身份把魔力中继给魔法池等下游接收器。
    /// 生命周期由 SubsystemManaSpreaderBehavior 管理。
    /// </summary>
    public class ManaSpreader : IManaSource, IManaReceiver {
        public const float MaxMana = 1200f;

        public Point3 Position { get; }

        public ManaStorage ManaStorage { get; }

        public ManaSpreader(Point3 position) {
            Position = position;
            ManaStorage = new ManaStorage(ManaBlockRegistry.GetMaxMana("ManaSpreaderBlock", MaxMana));
        }
    }
}
