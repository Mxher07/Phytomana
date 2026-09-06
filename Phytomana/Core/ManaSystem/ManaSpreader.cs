using Engine;
using Phytomana.Api;

namespace Phytomana {
    /// <summary>
    /// 魔力发射器的逐坐标魔力节点。仅实现 IManaReceiver：收取 3×3×3 邻域内产魔花的自动投递；
    /// 对下游（其他发射器/魔法池）不自动中继，只能通过生息法杖绑定的链路输送魔力
    /// （链路转移见 SubsystemGrownStaffBehavior.UpdateLinkTransfers）。
    /// 生命周期由 SubsystemManaSpreaderBehavior 管理。
    /// </summary>
    public class ManaSpreader : IManaReceiver {
        public const float MaxMana = 1200f;

        public Point3 Position { get; }

        public ManaStorage ManaStorage { get; }

        public ManaSpreader(Point3 position) {
            Position = position;
            ManaStorage = new ManaStorage(ManaBlockRegistry.GetMaxMana("ManaSpreaderBlock", MaxMana));
        }
    }
}
