using SharpGLTF.Schema2;
using GltfMaterial = SharpGLTF.Schema2.Material;

namespace Engine.Media {
    /// <summary>
    /// KHR_materials_transmission 扩展
    /// 支持折射透射效果（玻璃、水）
    /// </summary>
    public class TransmissionExtension : MaterialExtension {
        public override string ExtensionName => "KHR_materials_transmission";

        /// <summary>
        /// 透射因子（0-1）
        /// </summary>
        public float Factor { get; set; }

        /// <summary>
        /// 透射纹理
        /// </summary>
        public ModelMaterialTexture Texture { get; set; }

        public override bool IsEnabled => Factor > 0;

        public override IEnumerable<MaterialTextureSlot> GetTextureSlots() {
            if (Texture != null) {
                yield return MaterialTextureSlot.Transmission;
            }
        }

        public override void LoadFromGltf(GltfMaterial material, ModelData modelData) {
            MaterialChannel? channel = material.FindChannel("Transmission");
            if (channel != null) {
                Factor = GetChannelFactor(channel, "TransmissionFactor", 0f);
                Texture = LoadTextureFromChannel(modelData, channel);
            }
        }

        public override void AppendDefines(IShaderDefineBuilder defines) {
            if (IsEnabled) {
                defines.AddMaterialExtension("TRANSMISSION");
                if (Texture != null) {
                    defines.AddTextureMap("TRANSMISSION");
                    if (Texture.HasUVTransform) {
                        defines.AddUVTransform("TRANSMISSION");
                    }
                }
            }
        }
    }
}