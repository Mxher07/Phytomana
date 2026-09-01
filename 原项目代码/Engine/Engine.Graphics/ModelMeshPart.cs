namespace Engine.Graphics {
    public class ModelMeshPart : IDisposable {
        public BoundingBox m_boundingBox;
        public object m_tag;

        public string TexturePath;

        public VertexBuffer VertexBuffer { get; set; }

        public IndexBuffer IndexBuffer { get; set; }

        public int StartIndex { get; set; }

        public int IndicesCount { get; set; }

        /// <summary>
        /// 材质索引 (-1 表示无材质)
        /// </summary>
        public int MaterialIndex { get; set; } = -1;

        public PrimitiveType PrimitiveType { get; set; } = PrimitiveType.TriangleList;

        public BoundingBox BoundingBox {
            get => m_boundingBox;
            set => m_boundingBox = value;
        }

        public object Tag {
            get => m_tag;
            set => m_tag = value;
        }

        #region Morph Target Support

        /// <summary>
        /// Morph target 纹理（包含 morph target 数据的 Texture2DArray）
        /// </summary>
        public MorphTargetTexture MorphTargetTexture { get; set; }

        /// <summary>
        /// Morph target 数量
        /// </summary>
        public int MorphTargetCount { get; set; }

        /// <summary>
        /// Morph target 权重数组（动画系统更新）
        /// </summary>
        public float[] MorphWeights { get; set; }

        /// <summary>
        /// 是否有 morph targets（需要同时满足数量和纹理存在）
        /// </summary>
        public bool HasMorphTargets => MorphTargetTexture != null && MorphTargetCount > 0;

        /// <summary>
        /// Morph target 纹理偏移量（用于着色器 uniform）
        /// 从 MorphTargetTexture 自动获取
        /// </summary>
        public int MorphTargetPositionOffset => MorphTargetTexture?.PositionOffset ?? 0;
        public int MorphTargetNormalOffset => MorphTargetTexture?.NormalOffset ?? 0;
        public int MorphTargetTangentOffset => MorphTargetTexture?.TangentOffset ?? 0;
        public int MorphTargetTexCoord0Offset => MorphTargetTexture?.TexCoord0Offset ?? 0;
        public int MorphTargetTexCoord1Offset => MorphTargetTexture?.TexCoord1Offset ?? 0;
        public int MorphTargetColor0Offset => MorphTargetTexture?.Color0Offset ?? 0;

        /// <summary>
        /// Morph target 属性标志（从 MorphTargetTexture 自动获取）
        /// </summary>
        public bool HasMorphTargetPosition => MorphTargetTexture?.HasPosition ?? false;
        public bool HasMorphTargetNormal => MorphTargetTexture?.HasNormal ?? false;
        public bool HasMorphTargetTangent => MorphTargetTexture?.HasTangent ?? false;
        public bool HasMorphTargetTexCoord0 => MorphTargetTexture?.HasTexCoord0 ?? false;
        public bool HasMorphTargetTexCoord1 => MorphTargetTexture?.HasTexCoord1 ?? false;
        public bool HasMorphTargetColor0 => MorphTargetTexture?.HasColor0 ?? false;

        #endregion

        #region GPU Instancing Support

        /// <summary>
        /// 实例数量（大于 0 表示使用 GPU 实例化）
        /// </summary>
        public int InstanceCount { get; set; }

        /// <summary>
        /// 是否使用 GPU 实例化
        /// </summary>
        public bool UseInstancing => InstanceCount > 0;

        /// <summary>
        /// glTF EXT_mesh_gpu_instancing 实例局部矩阵
        /// </summary>
        public System.Numerics.Matrix4x4[] InstanceMatrices { get; set; }

        #endregion

        public void Dispose() {
            if (VertexBuffer != null) {
                VertexBuffer.Dispose();
                VertexBuffer = null;
            }
            if (IndexBuffer != null) {
                IndexBuffer.Dispose();
                IndexBuffer = null;
            }
            if (MorphTargetTexture != null) {
                MorphTargetTexture.Dispose();
                MorphTargetTexture = null;
            }
        }
    }
}