using Engine.Graphics;

namespace Engine.Media {
    public class ModelMeshPartData {
        public int BuffersDataIndex;

        public int StartIndex;

        public int IndicesCount;

        public BoundingBox BoundingBox;

        /// <summary>
        /// 材质索引 (-1 表示无材质)
        /// </summary>
        public int MaterialIndex = -1;

        public PrimitiveType PrimitiveType = PrimitiveType.TriangleList;

        public int InstanceCount;

        public System.Numerics.Matrix4x4[] InstanceMatrices;

        // Morph Target
        public MorphTargetTexture MorphTargetTexture;
        public int MorphTargetCount;
        public float[] MorphWeights;
    }
}