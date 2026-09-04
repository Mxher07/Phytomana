using Engine;

namespace Phytomana {
    /// <summary>
    /// 框架自测用产魔花：仅重写 FlowerTick 向缓存注魔，验证基座判定、休眠、分片 Tick、网络投递与存档。
    /// </summary>
    public class TileTestGeneratingFlower : TileGeneratingFlower {
        public const float ManaRate = 25f;

        public override float MaxMana => 300f;

        public TileTestGeneratingFlower(Point3 position) : base(position) { }

        public override void FlowerTick() {
            GenerateMana(ManaRate * DeltaTime);
        }
    }
}
