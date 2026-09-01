using SharpGLTF.Schema2;
using GltfMaterial = SharpGLTF.Schema2.Material;

namespace Engine.Media {
    /// <summary>
    /// KHR_materials_emissive_strength 扩展
    /// 支持发光强度控制（用于 HDR 发光）
    /// </summary>
    public class EmissiveStrengthExtension : MaterialExtension {
        public override string ExtensionName => "KHR_materials_emissive_strength";

        /// <summary>
        /// 发光强度（默认 1.0，可大于 1 用于 HDR）
        /// </summary>
        public float EmissiveStrength { get; set; } = 1f;

        /// <summary>
        /// Emissive Strength 启用条件：强度不为默认值
        /// </summary>
        public override bool IsEnabled => EmissiveStrength != 1f;

        public override IEnumerable<MaterialTextureSlot> GetTextureSlots() {
            yield break;
        }

        public override void LoadFromGltf(GltfMaterial material, ModelData modelData) {
            MaterialChannel? channel = material.FindChannel("Emissive");
            if (channel != null) {
                EmissiveStrength = GetChannelFactor(channel, "EmissiveStrength", 1f);
            }
        }

        public override void AppendDefines(IShaderDefineBuilder defines) {
            if (IsEnabled) {
                defines.AddMaterialExtension("EMISSIVE_STRENGTH");
            }
        }
    }
}