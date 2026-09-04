using Engine;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 功能花基类：自带魔力缓存并以 IManaReceiver 身份注册进魔力网络，可接收产魔源投递的魔力。
    /// </summary>
    public abstract class TileFunctionalFlower : TilePhytoFlower, IManaReceiver {
        public ManaStorage ManaStorage { get; }

        /// <summary>
        /// 花朵魔力上限。子类重写为常量值即可（重写体不得依赖实例字段，构造期会被读取）。
        /// </summary>
        public virtual float MaxMana => 1000f;

        /// <summary>
        /// 是否接收网络投递的魔力。不需要魔力的功能花重写为 false，避免分摊产魔源的输出。
        /// </summary>
        public virtual bool ReceivesMana => true;

        protected TileFunctionalFlower(Point3 position) : base(position) {
            ManaStorage = new ManaStorage(MaxMana);
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
