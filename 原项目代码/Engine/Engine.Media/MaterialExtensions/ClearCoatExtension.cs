using SharpGLTF.Schema2;
using GltfMaterial = SharpGLTF.Schema2.Material;

namespace Engine.Media {
    /// <summary>
    /// KHR_materials_clearcoat 扩展
    /// </summary>
    public class ClearCoatExtension : MaterialExtension {
        public override string ExtensionName => "KHR_materials_clearcoat";

        public float Factor { get; set; }
        public float RoughnessFactor { get; set; }
        public float NormalScale { get; set; } = 1f;
        public ModelMaterialTexture Texture { get; set; }
        public ModelMaterialTexture RoughnessTexture { get; set; }
        public ModelMaterialTexture NormalTexture { get; set; }

        public override bool IsEnabled => Factor > 0f;

        public override IEnumerable<MaterialTextureSlot> GetTextureSlots() {
            if (Texture != null) {
                yield return MaterialTextureSlot.ClearCoat;
            }
            if (RoughnessTexture != null) {
                yield return MaterialTextureSlot.ClearCoatRoughness;
            }
            if (NormalTexture != null) {
                yield return MaterialTextureSlot.ClearCoatNormal;
            }
        }

        public override void LoadFromGltf(GltfMaterial material, ModelData modelData) {
            MaterialChannel? channel;
            channel = material.FindChannel("ClearCoat");
            Factor = GetChannelFactor(channel, "ClearCoatFactor", 0f);
            Texture = LoadTextureFromChannel(modelData, channel);
            channel = material.FindChannel("ClearCoatRoughness");
            RoughnessFactor = GetChannelFactor(channel, "RoughnessFactor", 0f);
            RoughnessTexture = LoadTextureFromChannel(modelData, channel);
            channel = material.FindChannel("ClearCoatNormal");
            NormalScale = GetChannelFactor(channel, "NormalScale", 1f);
            NormalTexture = LoadTextureFromChannel(modelData, channel);
        }

        /// <summary>
        /// 附加着色器 defines（参考官方 getDefines 模式）
        /// </summary>
        public override void AppendDefines(IShaderDefineBuilder defines) {
            if (!IsEnabled) {
                return;
            }
            defines.AddMaterialExtension("CLEARCOAT");
            if (Texture != null) {
                defines.AddTextureMap("CLEARCOAT");
                if (Texture.HasUVTransform) {
                    defines.AddUVTransform("CLEARCOAT");
                }
            }
            if (RoughnessTexture != null) {
                defines.AddTextureMap("CLEARCOAT_ROUGHNESS");
                if (RoughnessTexture.HasUVTransform) {
                    defines.AddUVTransform("CLEARCOATROUGHNESS");
                }
            }
            if (NormalTexture != null) {
                defines.AddTextureMap("CLEARCOAT_NORMAL");
                if (NormalTexture.HasUVTransform) {
                    defines.AddUVTransform("CLEARCOATNORMAL");
                }
            }
        }
    }
}