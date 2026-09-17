using Engine;
using Game;

namespace Phytomana.Api {
    /// <summary>
    /// 生息法杖（工作模式）右键点中某个可交互方块时触发（按被点方块坐标路由）。
    /// 订阅方（如符文台）可实现 <see cref="TryHandleStaffInteraction"/> 消费该事件，
    /// 把处理结果写回 <see cref="Handled"/>；框架据此决定是否继续走默认逻辑。
    /// 这让法杖行为子系统无需直接依赖具体功能方块子系统（依赖反转，经事件总线解耦）。
    /// </summary>
    public class StaffInteractionEvent : IPhytoEvent {
        public Point3 Point { get; }

        public ComponentPlayer Player { get; }

        /// <summary>订阅方若消费了该交互则置真，法杖不再走后续默认分支。</summary>
        public bool Handled { get; set; }

        public StaffInteractionEvent(Point3 point, ComponentPlayer player) {
            Point = point;
            Player = player;
        }
    }

    /// <summary>
    /// 法杖交互的处理者契约：功能方块子系统（如符文台）实现之并订阅 <see cref="StaffInteractionEvent"/>。
    /// </summary>
    public interface IStaffInteractionHandler {
        void TryHandleStaffInteraction(StaffInteractionEvent evt);
    }
}
