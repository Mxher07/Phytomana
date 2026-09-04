using Engine;

namespace Phytomana.Api {
    /// <summary>
    /// 产魔花产出魔力时触发。处理器可修改 <see cref="Amount"/> 改变产量，或将 <see cref="Cancelled"/> 置真取消本次产出。
    /// </summary>
    public class ManaGenerateEvent : IPhytoEvent {
        public Point3 Position { get; }

        public float Amount { get; set; }

        public bool Cancelled { get; set; }

        public ManaGenerateEvent(Point3 position, float amount) {
            Position = position;
            Amount = amount;
        }
    }
}
