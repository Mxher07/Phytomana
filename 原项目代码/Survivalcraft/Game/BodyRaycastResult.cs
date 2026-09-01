using Engine;
using TemplatesDatabase;

namespace Game {
    public struct BodyRaycastResult {
        public BodyRaycastResult() { }
        public Ray3 Ray;

        public ComponentBody ComponentBody;

        public float Distance;

        /// <summary>
        ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
        /// </summary>
        public ValuesDictionary ValuesDictionaryForMods = new();

        public Vector3 HitPoint() => Ray.Sample(Distance);
    }
}