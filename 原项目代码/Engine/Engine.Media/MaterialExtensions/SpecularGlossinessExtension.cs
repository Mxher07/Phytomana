using SharpGLTF.Schema2;
using GltfMaterial = SharpGLTF.Schema2.Material;

namespace Engine.Media {
    /// <summary>
    /// KHR_materials_pbrSpecularGlossiness 扩展
    /// 旧版 PBR 工作流，使用 Specular-Glossiness 替代 Metallic-Roughness
    /// </summary>
    public class SpecularGlossinessExtension : MaterialExtension {
        public override string ExtensionName => "KHR_materials_pbrSpecularGlossiness";

        /// <summary>
        /// 漫反射颜色因子 (RGBA)
        /// </summary>
        public Vector4 DiffuseFactor { get; set; } = Vector4.One;

        /// <summary>
        /// 高光颜色因子 (RGB)
        /// </summary>
        public Vector3 SpecularFactor { get; set; } = Vector3.One;

        /// <summary>
        /// 光泽度因子 (0-1)
        /// </summary>
        public float GlossinessFactor { get; set; } = 1f;

        /// <summary>
        /// 漫反射纹理
        /// </summary>
        public ModelMaterialTexture DiffuseTexture { get; set; }

        /// <summary>
        /// 高光光泽度纹理 (RGB=Specular, A=Glossiness)
        /// </summary>
        public ModelMaterialTexture SpecularGlossinessTexture { get; set; }

        /// <summary>
        /// 只有在材质实际使用了 SpecularGlossiness 扩展时才启用
        /// </summary>
        public override bool IsEnabled => _isEnabled;

        bool _isEnabled;

        public override IEnumerable<MaterialTextureSlot> GetTextureSlots() {
            if (DiffuseTexture != null) {
                yield return MaterialTextureSlot.Diffuse;
            }
            if (SpecularGlossinessTexture != null) {
                yield return MaterialTextureSlot.SpecularGlossiness;
            }
        }

        public override void LoadFromGltf(GltfMaterial material, ModelData modelData) {
            MaterialChannel? diffuseChannel = material.FindChannel("Diffuse");
            if (diffuseChannel != null) {
                DiffuseFactor = diffuseChannel.Value.Color;
                DiffuseTexture = LoadTextureFromChannel(modelData, diffuseChannel);
                _isEnabled = true;
            }
            MaterialChannel? sgChannel = material.FindChannel("SpecularGlossiness");
            if (sgChannel != null) {
                foreach (IMaterialParameter param in sgChannel.Value.Parameters) {
                    if (param.Name == "SpecularFactor"
                        && param.ValueType == typeof(System.Numerics.Vector3)) {
                        System.Numerics.Vector3 spec = (System.Numerics.Vector3)param.Value;
                        SpecularFactor = spec;
                    }
                    else if (param.Name == "GlossinessFactor"
                        && param.ValueType == typeof(float)) {
                        GlossinessFactor = (float)param.Value;
                    }
                }
                SpecularGlossinessTexture = LoadTextureFromChannel(modelData, sgChannel);
                _isEnabled = true;
            }
        }

        public override void AppendDefines(IShaderDefineBuilder defines) {
            defines.Add("MATERIAL_SPECULARGLOSSINESS");
            if (DiffuseTexture != null) {
                defines.AddTextureMap("DIFFUSE");
                if (DiffuseTexture.HasUVTransform) {
                    defines.AddUVTransform("DIFFUSE");
                }
            }
            if (SpecularGlossinessTexture != null) {
                defines.AddTextureMap("SPECULAR_GLOSSINESS");
                if (SpecularGlossinessTexture.HasUVTransform) {
                    defines.AddUVTransform("SPECULARGLOSSINESS");
                }
            }
        }
    }
}