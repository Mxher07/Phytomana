using Engine;

namespace Phytomana.Api {
    /// <summary>
    /// 魔力网络将魔力从产魔源投递至接收器时触发（每次投递、每个接收器一次）。
    /// 处理器可修改 <see cref="Amount"/> 改变投递量（仍受接收器剩余容量限制），或将 <see cref="Cancelled"/> 置真取消本次投递。
    /// </summary>
    public class ManaTransmitEvent : IPhytoEvent {
        public Point3 Source { get; }

        public Point3 Receiver { get; }

        public float Amount { get; set; }

        public bool Cancelled { get; set; }

        public ManaTransmitEvent(Point3 source, Point3 receiver, float amount) {
            Source = source;
            Receiver = receiver;
            Amount = amount;
        }
    }
}
