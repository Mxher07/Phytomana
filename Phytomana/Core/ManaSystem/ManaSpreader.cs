using Engine;
using Phytomana.Api;

namespace Phytomana {
    /// <summary>
    /// 魔力发射器的逐坐标魔力节点。以 IManaReceiver 身份接入魔力网络接收产魔源投递的魔力，
    /// 法杖链路再从它把魔力转发到魔法池等目标。生命周期由 SubsystemManaSpreaderBehavior 管理。
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
