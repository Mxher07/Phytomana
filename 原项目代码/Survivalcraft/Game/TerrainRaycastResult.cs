using Engine;
using TemplatesDatabase;

namespace Game {
    public struct TerrainRaycastResult {
        public TerrainRaycastResult() { }

        public Ray3 Ray;

        public int Value;

        public CellFace CellFace;

        public int CollisionBoxIndex;

        public float Distance;

        public Vector3 HitPoint(float offsetFromSurface = 0f) =>
            Ray.Position + Ray.Direction * Distance + CellFace.FaceToVector3(CellFace.Face) * offsetFromSurface;

        /// <summary>
        ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
        /// </summary>
        public ValuesDictionary ValuesDictionaryForMods = new();
    }
}