using GltfMaterial = SharpGLTF.Schema2.Material;

namespace Engine.Media {
    /// <summary>
    /// KHR_materials_unlit 扩展
    /// 不进行光照计算，直接输出基础颜色
    /// </summary>
    public class UnlitExtension : MaterialExtension {
        public override string ExtensionName => "KHR_materials_unlit";

        bool _isEnabled;

        /// <summary>
        /// 只有当材质实际有 KHR_materials_unlit 扩展时才启用
        /// </summary>
        public override bool IsEnabled => _isEnabled;

        public override IEnumerable<MaterialTextureSlot> GetTextureSlots() {
            yield break;
        }

        public override void LoadFromGltf(GltfMaterial material, ModelData modelData) {
            _isEnabled = material.Unlit;
        }

        public override void AppendDefines(IShaderDefineBuilder defines) {
            if (IsEnabled) {
                defines.AddMaterialExtension("UNLIT");
            }
        }
    }
}