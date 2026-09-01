using Silk.NET.OpenGLES;

namespace Engine.Graphics {
    /// <summary>
    /// Morph Target 纹理，将 morph targets 数据打包为 TEXTURE_2D_ARRAY
    /// 参考 glTF-Sample-Renderer 的 primitive.js 实现
    /// 纹理布局：
    /// - 类型：TEXTURE_2D_ARRAY (RGBA32F)
    /// - 宽度/高度：ceil(sqrt(vertexCount))
    /// - 层数：targetCount * attributeCount
    /// - 层组织：[POSITION targets][NORMAL targets][TANGENT targets]...
    /// </summary>
    public class MorphTargetTexture : GraphicsResource {
        public bool m_disposed;

        public float[] m_layerData;

        // 保留原始数据用于 device reset 后重新上传
        public IReadOnlyList<Vector3>[] m_savedPositions;
        public IReadOnlyList<Vector3>[] m_savedNormals;
        public IReadOnlyList<Vector4>[] m_savedTangents;
        public IReadOnlyList<Vector2>[] m_savedTexCoords0;
        public IReadOnlyList<Vector2>[] m_savedTexCoords1;
        public IReadOnlyList<Vector4>[] m_savedColors0;

        // 定义属性的规范顺序
        public static readonly string[] CanonicalAttributeOrder = ["POSITION", "NORMAL", "TANGENT", "TEXCOORD_0", "TEXCOORD_1", "COLOR_0"];

        public readonly List<string> m_activeAttributes = [];

        /// <summary>
        /// 纹理句柄
        /// </summary>
        public int TextureHandle { get; private set; }

        /// <summary>
        /// 纹理尺寸（宽高相等）
        /// </summary>
        public int TextureSize { get; }

        /// <summary>
        /// 顶点数量
        /// </summary>
        public int VertexCount { get; }

        /// <summary>
        /// Morph target 数量
        /// </summary>
        public int TargetCount { get; }

        /// <summary>
        /// 纹理层数
        /// </summary>
        public int LayerCount { get; }

        /// <summary>
        /// 活跃属性列表
        /// </summary>
        public IReadOnlyList<string> ActiveAttributes => m_activeAttributes;

        /// <summary>
        /// 是否有 POSITION morph targets
        /// </summary>
        public bool HasPosition => PositionOffset >= 0;

        /// <summary>
        /// 是否有 NORMAL morph targets
        /// </summary>
        public bool HasNormal => NormalOffset >= 0;

        /// <summary>
        /// 是否有 TANGENT morph targets
        /// </summary>
        public bool HasTangent => TangentOffset >= 0;

        /// <summary>
        /// 是否有 TEXCOORD_0 morph targets
        /// </summary>
        public bool HasTexCoord0 => TexCoord0Offset >= 0;

        /// <summary>
        /// 是否有 TEXCOORD_1 morph targets
        /// </summary>
        public bool HasTexCoord1 => TexCoord1Offset >= 0;

        /// <summary>
        /// 是否有 COLOR_0 morph targets
        /// </summary>
        public bool HasColor0 => Color0Offset >= 0;

        /// <summary>
        /// POSITION 层偏移
        /// </summary>
        public int PositionOffset { get; private set; } = -1;

        /// <summary>
        /// NORMAL 层偏移
        /// </summary>
        public int NormalOffset { get; private set; } = -1;

        /// <summary>
        /// TANGENT 层偏移
        /// </summary>
        public int TangentOffset { get; private set; } = -1;

        /// <summary>
        /// TEXCOORD_0 层偏移
        /// </summary>
        public int TexCoord0Offset { get; private set; } = -1;

        /// <summary>
        /// TEXCOORD_1 层偏移
        /// </summary>
        public int TexCoord1Offset { get; private set; } = -1;

        /// <summary>
        /// COLOR_0 层偏移
        /// </summary>
        public int Color0Offset { get; private set; } = -1;

        /// <summary>
        /// 创建 Morph Target 纹理
        /// </summary>
        public MorphTargetTexture(int vertexCount, int targetCount, IReadOnlySet<string> attributes) {
            VertexCount = vertexCount;
            TargetCount = targetCount;

            // 按规范顺序排序属性
            foreach (string canonicalAttr in CanonicalAttributeOrder) {
                if (attributes.Contains(canonicalAttr)) {
                    m_activeAttributes.Add(canonicalAttr);
                }
            }

            // 计算纹理尺寸
            TextureSize = (int)Math.Ceiling(Math.Sqrt(vertexCount));

            // 计算层数和属性偏移
            int layerIndex = 0;
            foreach (string attr in m_activeAttributes) {
                switch (attr) {
                    case "POSITION": PositionOffset = layerIndex; break;
                    case "NORMAL": NormalOffset = layerIndex; break;
                    case "TANGENT": TangentOffset = layerIndex; break;
                    case "TEXCOORD_0": TexCoord0Offset = layerIndex; break;
                    case "TEXCOORD_1": TexCoord1Offset = layerIndex; break;
                    case "COLOR_0": Color0Offset = layerIndex; break;
                }
                layerIndex += targetCount;
            }
            LayerCount = targetCount * m_activeAttributes.Count;

            // 预分配层数据数组
            int layerPixelCount = TextureSize * TextureSize;
            m_layerData = new float[layerPixelCount * 4];
            CreateTexture();
        }

        public unsafe void CreateTexture() {
            TextureHandle = (int)GLWrapper.GL.GenTexture();
            GLWrapper.GL.BindTexture(TextureTarget.Texture2DArray, (uint)TextureHandle);
            GLWrapper.GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GLWrapper.GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GLWrapper.GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GLWrapper.GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GLWrapper.GL.TexImage3D(
                TextureTarget.Texture2DArray,
                0,
                InternalFormat.Rgba32f,
                (uint)TextureSize,
                (uint)TextureSize,
                (uint)LayerCount,
                0,
                PixelFormat.Rgba,
                PixelType.Float,
                null
            );
            GLWrapper.GL.BindTexture(TextureTarget.Texture2DArray, 0);
        }

        /// <summary>
        /// 上传 morph target 数据到纹理
        /// </summary>
        /// <param name="positions">POSITION deltas 数组（每个 target 一个数组）</param>
        /// <param name="normals">NORMAL deltas 数组（每个 target 一个数组）</param>
        /// <param name="tangents">TANGENT deltas 数组（每个 target 一个数组）</param>
        /// <param name="texCoords0">TEXCOORD_0 deltas 数组（每个 target 一个数组）</param>
        /// <param name="texCoords1">TEXCOORD_1 deltas 数组（每个 target 一个数组）</param>
        /// <param name="colors0">COLOR_0 deltas 数组（每个 target 一个数组）</param>
        public void UploadData(IReadOnlyList<Vector3>[] positions,
            IReadOnlyList<Vector3>[] normals,
            IReadOnlyList<Vector4>[] tangents,
            IReadOnlyList<Vector2>[] texCoords0,
            IReadOnlyList<Vector2>[] texCoords1,
            IReadOnlyList<Vector4>[] colors0) {
            GLWrapper.GL.BindTexture(TextureTarget.Texture2DArray, (uint)TextureHandle);
            for (int t = 0; t < TargetCount; t++) {
                // POSITION
                if (PositionOffset >= 0
                    && positions != null
                    && t < positions.Length
                    && positions[t] != null) {
                    UploadAttributeLayer(positions[t], PositionOffset + t);
                }

                // NORMAL
                if (NormalOffset >= 0
                    && normals != null
                    && t < normals.Length
                    && normals[t] != null) {
                    UploadAttributeLayer(normals[t], NormalOffset + t);
                }

                // TANGENT
                if (TangentOffset >= 0
                    && tangents != null
                    && t < tangents.Length
                    && tangents[t] != null) {
                    UploadAttributeLayer(tangents[t], TangentOffset + t);
                }

                // TEXCOORD_0
                if (TexCoord0Offset >= 0
                    && texCoords0 != null
                    && t < texCoords0.Length
                    && texCoords0[t] != null) {
                    UploadAttributeLayer(texCoords0[t], TexCoord0Offset + t);
                }

                // TEXCOORD_1
                if (TexCoord1Offset >= 0
                    && texCoords1 != null
                    && t < texCoords1.Length
                    && texCoords1[t] != null) {
                    UploadAttributeLayer(texCoords1[t], TexCoord1Offset + t);
                }

                // COLOR_0
                if (Color0Offset >= 0
                    && colors0 != null
                    && t < colors0.Length
                    && colors0[t] != null) {
                    UploadAttributeLayer(colors0[t], Color0Offset + t);
                }
            }
            GLWrapper.GL.BindTexture(TextureTarget.Texture2DArray, 0);

            // 保留原始数据用于 device reset 后重新上传
            m_savedPositions = positions;
            m_savedNormals = normals;
            m_savedTangents = tangents;
            m_savedTexCoords0 = texCoords0;
            m_savedTexCoords1 = texCoords1;
            m_savedColors0 = colors0;
        }

        public unsafe void UploadAttributeLayer<T>(IReadOnlyList<T> attributeData, int layerIndex) where T : unmanaged {
            // 清零层数据
            Array.Clear(m_layerData, 0, m_layerData.Length);
            int vertexCount = Math.Min(attributeData.Count, VertexCount);

            // 填充数据（VEC2/VEC3 填充为 VEC4/RGBA）
            if (attributeData is IReadOnlyList<Vector3> vec3Data) {
                for (int i = 0; i < vertexCount; i++) {
                    int offset = i * 4;
                    Vector3 v = vec3Data[i];
                    m_layerData[offset + 0] = v.X;
                    m_layerData[offset + 1] = v.Y;
                    m_layerData[offset + 2] = v.Z;
                    m_layerData[offset + 3] = 0f; // padding
                }
            }
            else if (attributeData is IReadOnlyList<Vector4> vec4Data) {
                for (int i = 0; i < vertexCount; i++) {
                    int offset = i * 4;
                    Vector4 v = vec4Data[i];
                    m_layerData[offset + 0] = v.X;
                    m_layerData[offset + 1] = v.Y;
                    m_layerData[offset + 2] = v.Z;
                    m_layerData[offset + 3] = v.W;
                }
            }
            else if (attributeData is IReadOnlyList<Vector2> vec2Data) {
                for (int i = 0; i < vertexCount; i++) {
                    int offset = i * 4;
                    Vector2 v = vec2Data[i];
                    m_layerData[offset + 0] = v.X;
                    m_layerData[offset + 1] = v.Y;
                    m_layerData[offset + 2] = 0f; // padding
                    m_layerData[offset + 3] = 0f; // padding
                }
            }

            // 上传到纹理
            fixed (float* ptr = m_layerData) {
                GLWrapper.GL.TexSubImage3D(
                    TextureTarget.Texture2DArray,
                    0,
                    0,
                    0,
                    layerIndex,
                    (uint)TextureSize,
                    (uint)TextureSize,
                    1,
                    PixelFormat.Rgba,
                    PixelType.Float,
                    ptr
                );
            }
        }

        /// <summary>
        /// 绑定 morph target 纹理到指定纹理单元
        /// </summary>
        public void Bind(TextureUnit unit) {
            GLWrapper.ActiveTexture(unit);
            GLWrapper.BindTexture(TextureTarget.Texture2DArray, TextureHandle, true);
        }

        public override void Dispose() {
            if (m_disposed) {
                return;
            }
            if (TextureHandle != 0) {
                GLWrapper.GL.DeleteTexture((uint)TextureHandle);
                TextureHandle = 0;
            }
            m_disposed = true;
            base.Dispose();
        }

        public override int GetGpuMemoryUsage() => TextureSize * TextureSize * LayerCount * 16; // RGBA32F = 16 bytes per pixel

        public override void HandleDeviceLost() {
            TextureHandle = 0;
        }

        public override void HandleDeviceReset() {
            CreateTexture();
            if (m_savedPositions != null
                || m_savedNormals != null
                || m_savedTangents != null) {
                UploadData(m_savedPositions, m_savedNormals, m_savedTangents, m_savedTexCoords0, m_savedTexCoords1, m_savedColors0);
            }
        }
    }
}