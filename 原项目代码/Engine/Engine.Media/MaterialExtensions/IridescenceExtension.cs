using SharpGLTF.Schema2;
using GltfMaterial = SharpGLTF.Schema2.Material;

namespace Engine.Media {
    /// <summary>
    /// KHR_materials_iridescence 扩展
    /// </summary>
    public class IridescenceExtension : MaterialExtension {
        public override string ExtensionName => "KHR_materials_iridescence";

        public float Factor { get; set; }
        public float IOR { get; set; } = 1.3f;
        public float ThicknessMinimum { get; set; } = 100f;
        public float ThicknessMaximum { get; set; } = 400f;
        public ModelMaterialTexture Texture { get; set; }
        public ModelMaterialTexture ThicknessTexture { get; set; }

        public override bool IsEnabled => Factor > 0f;

        public override IEnumerable<MaterialTextureSlot> GetTextureSlots() {
            if (Texture != null) {
                yield return MaterialTextureSlot.Iridescence;
            }
            if (ThicknessTexture != null) {
                yield return MaterialTextureSlot.IridescenceThickness;
            }
        }

        public override void LoadFromGltf(GltfMaterial material, ModelData modelData) {
            MaterialChannel? channel;
            channel = material.FindChannel("Iridescence");
            Factor = GetChannelFactor(channel, "IridescenceFactor", 0f);
            IOR = GetChannelFactor(channel, "IndexOfRefraction", 1.3f);
            Texture = LoadTextureFromChannel(modelData, channel);
            channel = material.FindChannel("IridescenceThickness");
            ThicknessMinimum = GetChannelFactor(channel, "Minimum", 100f);
            ThicknessMaximum = GetChannelFactor(channel, "Maximum", 400f);
            ThicknessTexture = LoadTextureFromChannel(modelData, channel);
        }

        /// <summary>
        /// 附加着色器 defines（参考官方 getDefines 模式）
        /// </summary>
        public override void AppendDefines(IShaderDefineBuilder defines) {
            if (!IsEnabled) {
                return;
            }
            defines.AddMaterialExtension("IRIDESCENCE");
            if (Texture != null) {
                defines.AddTextureMap("IRIDESCENCE");
                if (Texture.HasUVTransform) {
                    defines.AddUVTransform("IRIDESCENCE");
                }
            }
            if (ThicknessTexture != null) {
                defines.AddTextureMap("IRIDESCENCE_THICKNESS");
                if (ThicknessTexture.HasUVTransform) {
                    defines.AddUVTransform("IRIDESCENCETHICKNESS");
                }
            }
        }
    }
}