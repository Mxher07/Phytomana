using Engine;

namespace Phytomana.Api {
    /// <summary>
    /// 产魔源契约。实现方向魔力网络注册后，网络会周期性检测范围内可投递的接收器并均分其储存的魔力。
    /// </summary>
    public interface IManaSource {
        Point3 Position { get; }

        ManaStorage ManaStorage { get; }
    }
}
