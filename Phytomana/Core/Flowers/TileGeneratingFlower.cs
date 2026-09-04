using Engine;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 产魔花基类：自带魔力缓存并以 IManaSource 身份注册进魔力网络，
    /// 缓存中的魔力由网络按距离/视线均分投递给接收器，花朵自身不写投递逻辑。
    /// </summary>
    public abstract class TileGeneratingFlower : TilePhytoFlower, IManaSource {
        public ManaStorage ManaStorage { get; }

        /// <summary>
        /// 花朵魔力上限。子类重写为常量值即可（重写体不得依赖实例字段，构造期会被读取）。
        /// </summary>
        public virtual float MaxMana => 1000f;

        /// <summary>
        /// 是否正在产出魔力（法杖状态查询用）。
        /// </summary>
        public virtual bool IsProducing => State == FlowerState.Working;

        /// <summary>
        /// 是否正在流失魔力（法杖状态查询用）。
        /// </summary>
        public virtual bool IsLosingMana => false;

        protected TileGeneratingFlower(Point3 position) : base(position) {
            ManaStorage = new ManaStorage(MaxMana);
        }

        /// <summary>
        /// 当前产魔速率 mn/s（法杖状态查询用）。
        /// </summary>
        public virtual float GetProductionRate() => 0f;

        /// <summary>
        /// 产出魔力的唯一入口：经事件总线广播 ManaGenerateEvent（外部 Mod 可改量或拦截）后注入缓存。
        /// 返回实际注入量。子类业务逻辑产魔一律调用本方法，禁止直接写 ManaStorage。
        /// </summary>
        protected float GenerateMana(float amount) {
            if (amount <= 0f) {
                return 0f;
            }
            ManaGenerateEvent evt = new(Position, amount);
            PhytoEventBus.Fire(evt);
            if (evt.Cancelled || evt.Amount <= 0f) {
                return 0f;
            }
            return ManaStorage.TryAdd(evt.Amount);
        }

        public override void SaveData(ValuesDictionary values) {
            base.SaveData(values);
            values.SetValue("Mana", ManaStorage.SaveData());
        }

        public override void LoadData(ValuesDictionary values) {
            base.LoadData(values);
            ManaStorage.LoadData(values.GetValue("Mana", 0f));
        }
    }
}
