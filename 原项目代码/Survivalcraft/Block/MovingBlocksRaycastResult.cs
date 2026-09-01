using Engine;
using TemplatesDatabase;

namespace Game {
    public struct MovingBlocksRaycastResult {
        public MovingBlocksRaycastResult() { }

        public Ray3 Ray;

        public IMovingBlockSet MovingBlockSet;

        public float Distance;
        public Vector3 HitPoint() => Ray.Position + Ray.Direction * Distance;

        public MovingBlock MovingBlock;

        public int CollisionBoxIndex;

        public BoundingBox? BlockBoundingBox;

        public int BlockValue => MovingBlock?.Value ?? -1;

        /// <summary>
        ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
        /// </summary>
        public ValuesDictionary ValuesDictionaryForMods = new();
    }
}