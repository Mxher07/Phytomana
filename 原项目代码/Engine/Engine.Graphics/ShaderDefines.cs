using System.Text;
using Engine.Media;

namespace Engine.Graphics {
    /// <summary>
    /// 着色器预处理器定义列表
    /// 对应官方 renderer.js 中的 defines 数组
    /// </summary>
    public class ShaderDefines : IShaderDefineBuilder {
        public readonly List<string> m_defines = [];

        // 缓存的 hash，避免每次遍历计算
        public int m_cachedHash;
        public bool m_hashValid;

        /// <summary>
        /// 确定性字符串 hash（string.GetHashCode 在 .NET Core 每次进程不同，不能用于持久化缓存）
        /// </summary>
        static int StableStringHash(string s) {
            unchecked {
                int h = 0;
                foreach (char c in s) {
                    h = h * 31 + c;
                }
                return h;
            }
        }

        /// <summary>
        /// 增量更新 hash
        /// </summary>
        public void UpdateHash(string define) {
            unchecked {
                int h = StableStringHash(define);
                if (m_hashValid) {
                    m_cachedHash = m_cachedHash * 31 + h;
                }
                else {
                    m_cachedHash = 17 * 31 + h;
                    m_hashValid = true;
                }
            }
        }

        /// <summary>
        /// 从另一个 ShaderDefines 复制 hash（用于 Clone）
        /// </summary>
        public void CopyHashFrom(ShaderDefines other) {
            m_cachedHash = other.m_cachedHash;
            m_hashValid = other.m_hashValid;
        }

        /// <summary>
        /// 添加一个 define（如 "MATERIAL_CLEARCOAT 1"）
        /// </summary>
        public void Add(string define, int value = 1) {
            string str = $"{define} {value}";
            m_defines.Add(str);
            UpdateHash(str);
        }

        /// <summary>
        /// 添加一个完整的 define 字符串（不带额外格式化）
        /// 如 "SCATTER_SAMPLES_COUNT 55"
        /// </summary>
        public void AddRaw(string define) {
            m_defines.Add(define);
            UpdateHash(define);
        }

        /// <summary>
        /// 添加纹理 define（如 "HAS_NORMAL_MAP 1"）
        /// </summary>
        public void AddTextureMap(string textureName) {
            string str = $"HAS_{textureName.ToUpper()}_MAP 1";
            m_defines.Add(str);
            UpdateHash(str);
        }

        /// <summary>
        /// 添加 UV Transform define（如 "HAS_BASECOLOR_UV_TRANSFORM 1"）
        /// </summary>
        public void AddUVTransform(string textureName) {
            string str = $"HAS_{textureName.ToUpper()}_UV_TRANSFORM 1";
            m_defines.Add(str);
            UpdateHash(str);
        }

        /// <summary>
        /// 添加材质扩展 define（如 "MATERIAL_CLEARCOAT 1"）
        /// </summary>
        public void AddMaterialExtension(string extensionName) {
            string str = $"MATERIAL_{extensionName.ToUpper()} 1";
            m_defines.Add(str);
            UpdateHash(str);
        }

        /// <summary>
        /// 添加顶点属性 define
        /// </summary>
        public void AddVertexAttribute(string attributeName, int componentCount) {
            string suffix = componentCount switch {
                2 => "VEC2",
                3 => "VEC3",
                4 => "VEC4",
                _ => ""
            };
            if (!string.IsNullOrEmpty(suffix)) {
                string str = $"HAS_{attributeName.ToUpper()}_{suffix} 1";
                m_defines.Add(str);
                UpdateHash(str);
            }
        }

        /// <summary>
        /// 设置权重数量（用于骨骼动画）
        /// </summary>
        public void SetWeightCount(int count) {
            string str = $"WEIGHT_COUNT {count}";
            m_defines.Add(str);
            UpdateHash(str);
        }

        /// <summary>
        /// 设置 Joint 数量（用于骨骼动画）
        /// </summary>
        public void SetJointCount(int count) {
            string str = $"JOINT_COUNT {count}";
            m_defines.Add(str);
            UpdateHash(str);
        }

        /// <summary>
        /// 添加 Morph Target 相关 defines
        /// </summary>
        public void SetMorphTargetDefines(int targetCount,
            bool hasPosition,
            bool hasNormal,
            bool hasTangent,
            bool hasTexCoord0,
            bool hasTexCoord1,
            bool hasColor0,
            int positionOffset,
            int normalOffset,
            int tangentOffset,
            int texCoord0Offset,
            int texCoord1Offset,
            int color0Offset) {
            if (targetCount <= 0) {
                return;
            }
            Add("USE_MORPHING");
            Add("HAS_MORPH_TARGETS");
            AddRaw($"WEIGHT_COUNT {targetCount}");
            if (hasPosition) {
                Add("HAS_MORPH_TARGET_POSITION");
                AddRaw($"MORPH_TARGET_POSITION_OFFSET {positionOffset}");
            }
            if (hasNormal) {
                Add("HAS_MORPH_TARGET_NORMAL");
                AddRaw($"MORPH_TARGET_NORMAL_OFFSET {normalOffset}");
            }
            if (hasTangent) {
                Add("HAS_MORPH_TARGET_TANGENT");
                AddRaw($"MORPH_TARGET_TANGENT_OFFSET {tangentOffset}");
            }
            if (hasTexCoord0) {
                Add("HAS_MORPH_TARGET_TEXCOORD_0");
                AddRaw($"MORPH_TARGET_TEXCOORD_0_OFFSET {texCoord0Offset}");
            }
            if (hasTexCoord1) {
                Add("HAS_MORPH_TARGET_TEXCOORD_1");
                AddRaw($"MORPH_TARGET_TEXCOORD_1_OFFSET {texCoord1Offset}");
            }
            if (hasColor0) {
                Add("HAS_MORPH_TARGET_COLOR_0");
                AddRaw($"MORPH_TARGET_COLOR_0_OFFSET {color0Offset}");
            }
        }

        /// <summary>
        /// 设置 Alpha 模式
        /// </summary>
        public void SetAlphaMode(ModelAlphaMode mode) {
            int modeValue = mode switch {
                ModelAlphaMode.Opaque => 0,
                ModelAlphaMode.Mask => 1,
                ModelAlphaMode.Blend => 2,
                _ => 0
            };
            // Remove existing ALPHAMODE define if present
            // 注意：移除后需要重新计算 hash
            bool removed = m_defines.RemoveAll(d => d.StartsWith("ALPHAMODE ")) > 0;
            string str = $"ALPHAMODE {modeValue}";
            m_defines.Add(str);

            // 如果移除了旧值，需要重新计算整个 hash
            if (removed) {
                m_hashValid = false;
                m_cachedHash = 0;
                foreach (string define in m_defines) {
                    UpdateHash(define);
                }
            }
            else {
                UpdateHash(str);
            }
        }

        /// <summary>
        /// 生成完整的 defines 代码（包含 #version）
        /// </summary>
        public string GetDefinesCode() {
            StringBuilder sb = new(m_defines.Count * 32 + 20);
            sb.AppendLine($"#version {ShaderCache.GlslVersion} es");
            foreach (string define in m_defines) {
                sb.AppendLine($"#define {define}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 生成 defines 代码（不包含 #version，用于插入到着色器中）
        /// </summary>
        public string GetDefinesCodeWithoutVersion() {
            StringBuilder sb = new(m_defines.Count * 32);
            foreach (string define in m_defines) {
                sb.AppendLine($"#define {define}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取 defines 列表（用于 ShaderCache.SelectShader）
        /// </summary>
        public IEnumerable<string> GetDefinesList() => m_defines;

        /// <summary>
        /// 计算组合 hash（用于着色器缓存）
        /// 使用缓存避免重复计算
        /// </summary>
        public int ComputeHash() {
            if (m_hashValid) {
                return m_cachedHash;
            }
            unchecked {
                m_cachedHash = 17;
                foreach (string define in m_defines) {
                    m_cachedHash = m_cachedHash * 31 + StableStringHash(define);
                }
                m_hashValid = true;
            }
            return m_cachedHash;
        }

        public override string ToString() => string.Join(", ", m_defines);

        /// <summary>
        /// 创建当前 ShaderDefines 的副本
        /// </summary>
        public ShaderDefines Clone() {
            ShaderDefines clone = new();
            clone.m_defines.AddRange(m_defines);
            clone.CopyHashFrom(this);
            return clone;
        }

        /// <summary>
        /// 转换为 ShaderMacro 数组（用于与现有 Shader 系统集成）
        /// </summary>
        public ShaderMacro[] ToShaderMacros() {
            ShaderMacro[] macros = new ShaderMacro[m_defines.Count];
            for (int i = 0; i < m_defines.Count; i++) {
                string define = m_defines[i];
                int spaceIndex = define.IndexOf(' ');
                if (spaceIndex > 0) {
                    macros[i] = new ShaderMacro(define.Substring(0, spaceIndex), define.Substring(spaceIndex + 1));
                }
                else {
                    macros[i] = new ShaderMacro(define);
                }
            }
            return macros;
        }

        #region Static Factory Methods

        /// <summary>
        /// 创建默认的顶点着色器 defines
        /// </summary>
        public static ShaderDefines CreateVertexDefines(bool hasNormals,
            bool hasTangents,
            bool hasTexcoord0,
            bool hasTexcoord1,
            bool hasColor0,
            bool useSkinning = false,
            int weightCount = 4,
            int jointCount = 4,
            bool useMorphing = false,
            int morphTargetCount = 0) {
            ShaderDefines defines = new();
            if (hasNormals) {
                defines.AddVertexAttribute("NORMAL", 3);
            }
            if (hasTangents) {
                defines.AddVertexAttribute("TANGENT", 4);
            }
            if (hasTexcoord0) {
                defines.AddVertexAttribute("TEXCOORD_0", 2);
            }
            if (hasTexcoord1) {
                defines.AddVertexAttribute("TEXCOORD_1", 2);
            }
            if (hasColor0) {
                defines.Add("HAS_COLOR_0_VEC4"); // Assume vec4 by default
            }
            if (useSkinning) {
                defines.Add("USE_SKINNING");
                defines.Add("HAS_JOINTS_0_VEC4");
                defines.Add("HAS_WEIGHTS_0_VEC4");
                defines.SetWeightCount(weightCount);
                defines.SetJointCount(jointCount);
            }
            if (useMorphing && morphTargetCount > 0) {
                defines.Add("USE_MORPHING");
                defines.AddRaw($"MORPH_TARGET_COUNT {morphTargetCount}");
            }
            return defines;
        }

        /// <summary>
        /// 从 VertexDeclaration 创建着色器 defines
        /// </summary>
        public static ShaderDefines CreateFromVertexDeclaration(VertexDeclaration declaration, bool enableSkinning = true) {
            ShaderDefines defines = new();
            if (declaration == null) {
                return defines;
            }
            bool hasJoints = false;
            bool hasWeights = false;
            foreach (VertexElement element in declaration.VertexElements) {
                string semantic = element.SemanticName.ToUpperInvariant();
                int componentCount = element.Format.GetElementsCount();
                switch (semantic) {
                    case "NORMAL": defines.AddVertexAttribute("NORMAL", componentCount); break;
                    case "TANGENT": defines.AddVertexAttribute("TANGENT", componentCount); break;
                    case "TEXCOORD":
                        if (element.SemanticIndex == 0) {
                            defines.AddVertexAttribute("TEXCOORD_0", componentCount);
                        }
                        else if (element.SemanticIndex == 1) {
                            defines.AddVertexAttribute("TEXCOORD_1", componentCount);
                        }
                        break;
                    case "COLOR":
                        if (element.SemanticIndex == 0) {
                            defines.Add(componentCount == 3 ? "HAS_COLOR_0_VEC3" : "HAS_COLOR_0_VEC4");
                        }
                        break;
                    case "JOINTS": hasJoints = true; break;
                    case "WEIGHTS": hasWeights = true; break;
                }
            }

            // GPU Skinning
            if (enableSkinning
                && hasJoints
                && hasWeights) {
                defines.Add("USE_SKINNING");
                defines.Add("HAS_JOINTS_0_VEC4");
                defines.Add("HAS_WEIGHTS_0_VEC4");
                defines.SetWeightCount(4);
                defines.SetJointCount(4);
            }
            return defines;
        }

        /// <summary>
        /// 从 ModelMeshPart 创建顶点着色器 defines
        /// </summary>
        public static ShaderDefines CreateFromModelMeshPart(ModelMeshPart meshPart, bool enableSkinning = true, bool enableMorphing = true) {
            if (meshPart?.VertexBuffer == null) {
                return new ShaderDefines();
            }
            VertexDeclaration declaration = meshPart.VertexBuffer.VertexDeclaration;
            ShaderDefines defines = CreateFromVertexDeclaration(declaration, enableSkinning);
            if (meshPart.UseInstancing) {
                defines.Add("USE_INSTANCING");
            }
            if (!IsTrianglePrimitive(meshPart.PrimitiveType)) {
                defines.Add("NOT_TRIANGLE");
            }
            if (enableMorphing && meshPart.HasMorphTargets) {
                defines.SetMorphTargetDefines(
                    meshPart.MorphTargetCount,
                    meshPart.HasMorphTargetPosition,
                    meshPart.HasMorphTargetNormal,
                    meshPart.HasMorphTargetTangent,
                    meshPart.HasMorphTargetTexCoord0,
                    meshPart.HasMorphTargetTexCoord1,
                    meshPart.HasMorphTargetColor0,
                    meshPart.MorphTargetPositionOffset,
                    meshPart.MorphTargetNormalOffset,
                    meshPart.MorphTargetTangentOffset,
                    meshPart.MorphTargetTexCoord0Offset,
                    meshPart.MorphTargetTexCoord1Offset,
                    meshPart.MorphTargetColor0Offset
                );
            }
            return defines;
        }

        public static bool IsTrianglePrimitive(PrimitiveType type) => type is PrimitiveType.TriangleList
            or PrimitiveType.TriangleStrip
            or PrimitiveType.TriangleFan;

        #endregion
    }
}