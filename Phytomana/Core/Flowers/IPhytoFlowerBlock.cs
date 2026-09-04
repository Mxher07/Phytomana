using Engine;

namespace Phytomana {
    /// <summary>
    /// 花朵方块契约。实现本接口的方块会被 SubsystemFlowerBehavior 统一接管：
    /// 放置/破坏/区块装卸时自动创建、注册、销毁花朵节点，无需为每朵花单独写行为子系统。
    /// </summary>
    public interface IPhytoFlowerBlock {
        TilePhytoFlower CreateFlower(Point3 position);
    }
}
