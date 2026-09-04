using Engine;

namespace Phytomana.Api {
    /// <summary>
    /// 魔力接收契约。实现方向魔力网络注册后，可接收范围内产魔源投递的魔力。
    /// </summary>
    public interface IManaReceiver {
        Point3 Position { get; }

        ManaStorage ManaStorage { get; }
    }
}
