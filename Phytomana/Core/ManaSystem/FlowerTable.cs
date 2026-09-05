using System.Collections.Generic;
using Engine;
using Phytomana.Api;

namespace Phytomana {
    /// <summary>
    /// 花药台的逐坐标节点。对应植物魔法「花瓣药剂台」：方块为单例，
    /// 逐位置的「是否有水 / 已投入原料 / 魔力」由本节点承载，
    /// 生命周期与持久化由 SubsystemFlowerTableBehavior 管理。
    /// </summary>
    public class FlowerTable : IManaReceiver {
        public Point3 Position { get; }

        public ManaStorage ManaStorage { get; }

        /// <summary>是否已注入水（拿水桶右键注入，空桶右键取回）。无水时不吸收原料。</summary>
        public bool HasWater;

        /// <summary>已投入的原料：方块内容索引 → 数量。投入的材料与配方完全一致后，投掷种子即可合成。</summary>
        public Dictionary<int, int> Ingredients = [];

        public FlowerTable(Point3 position) {
            Position = position;
            ManaStorage = new ManaStorage(ManaBlockRegistry.GetMaxMana("FlowerTableBlock", SubsystemFlowerTableBehavior.DefaultMaxMana));
        }
    }
}