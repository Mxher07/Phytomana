using GltfMaterial = SharpGLTF.Schema2.Material;

namespace Engine.Media {
    /// <summary>
    /// KHR_materials_dispersion 扩展
    /// 支持色散效果（棱镜分解光）
    /// </summary>
    public class DispersionExtension : MaterialExtension {
        public override string ExtensionName => "KHR_materials_dispersion";

        /// <summary>
        /// 色散强度（Abbe number 的倒数）
        /// </summary>
        public float Dispersion { get; set; }

        /// <summary>
        /// Dispersion 启用条件：色散值大于 0
        /// </summary>
        public override bool IsEnabled => Dispersion > 0f;

        public override IEnumerable<MaterialTextureSlot> GetTextureSlots() {
            yield break;
        }

        public override void LoadFromGltf(GltfMaterial material, ModelData modelData) {
            Dispersion = material.Dispersion;
        }

        public override void AppendDefines(IShaderDefineBuilder defines) {
            if (IsEnabled) {
                defines.AddMaterialExtension("DISPERSION");
            }
        }
    }
}