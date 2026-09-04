using Engine;
using Phytomana.Api;

namespace Phytomana {
    /// <summary>
    /// 框架自测用产魔源节点：仅实现 IManaSource，无额外玩法。
    /// </summary>
    public class TestManaSource : IManaSource {
        public const float MaxMana = 200f;

        public Point3 Position { get; }

        public ManaStorage ManaStorage { get; }

        public TestManaSource(Point3 position) {
            Position = position;
            ManaStorage = new ManaStorage(MaxMana);
        }
    }
}
