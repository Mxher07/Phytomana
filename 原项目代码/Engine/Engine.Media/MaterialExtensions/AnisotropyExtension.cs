using SharpGLTF.Schema2;
using GltfMaterial = SharpGLTF.Schema2.Material;

namespace Engine.Media {
    /// <summary>
    /// KHR_materials_anisotropy 扩展
    /// 支持各向异性反射（如拉丝金属）
    /// </summary>
    public class AnisotropyExtension : MaterialExtension {
        public override string ExtensionName => "KHR_materials_anisotropy";

        /// <summary>
        /// 各向异性强度 (0-1)
        /// </summary>
        public float AnisotropyStrength { get; set; }

        /// <summary>
        /// 各向异性旋转角度（弧度）
        /// </summary>
        public float AnisotropyRotation { get; set; }

        /// <summary>
        /// 各向异性纹理
        /// </summary>
        public ModelMaterialTexture AnisotropyTexture { get; set; }

        /// <summary>
        /// Anisotropy 启用条件：强度大于 0
        /// </summary>
        public override bool IsEnabled => AnisotropyStrength > 0f;

        public override IEnumerable<MaterialTextureSlot> GetTextureSlots() {
            if (AnisotropyTexture != null) {
                yield return MaterialTextureSlot.Anisotropy;
            }
        }

        public override void LoadFromGltf(GltfMaterial material, ModelData modelData) {
            MaterialChannel? channel = material.FindChannel("Anisotropy");
            if (channel != null) {
                AnisotropyStrength = GetChannelFactor(channel, "AnisotropyStrength", 0f);
                AnisotropyRotation = GetChannelFactor(channel, "AnisotropyRotation", 0f);
                AnisotropyTexture = LoadTextureFromChannel(modelData, channel);
            }
        }

        public override void AppendDefines(IShaderDefineBuilder defines) {
            if (IsEnabled) {
                defines.AddMaterialExtension("ANISOTROPY");
                if (AnisotropyTexture != null) {
                    defines.AddTextureMap("ANISOTROPY");
                    if (AnisotropyTexture.HasUVTransform) {
                        defines.AddUVTransform("ANISOTROPY");
                    }
                }
            }
        }
    }
}