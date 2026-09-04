using Engine;

namespace Phytomana.Api {
    /// <summary>
    /// 花朵状态切换时触发（进入休眠/工作/死亡等）。只读通知事件。
    /// </summary>
    public class FlowerStateChangedEvent : IPhytoEvent {
        public Point3 Position { get; }

        public FlowerState OldState { get; }

        public FlowerState NewState { get; }

        /// <summary>
        /// 花朵节点类型名（GetType().Name），用于区分花种。
        /// </summary>
        public string FlowerType { get; }

        public FlowerStateChangedEvent(Point3 position, FlowerState oldState, FlowerState newState, string flowerType) {
            Position = position;
            OldState = oldState;
            NewState = newState;
            FlowerType = flowerType;
        }
    }
}
