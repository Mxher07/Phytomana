using SharpGLTF.Schema2;
using GltfMaterial = SharpGLTF.Schema2.Material;

namespace Engine.Media {
    /// <summary>
    /// KHR_materials_sheen 扩展
    /// 支持天鹅绒、布料等材质的 sheen 效果
    /// </summary>
    public class SheenExtension : MaterialExtension {
        public override string ExtensionName => "KHR_materials_sheen";

        /// <summary>
        /// Sheen 颜色因子
        /// </summary>
        public Vector3 ColorFactor { get; set; } = Vector3.Zero;

        /// <summary>
        /// Sheen 粗糙度因子
        /// </summary>
        public float RoughnessFactor { get; set; }

        /// <summary>
        /// Sheen 颜色纹理
        /// </summary>
        public ModelMaterialTexture ColorTexture { get; set; }

        /// <summary>
        /// Sheen 粗糙度纹理
        /// </summary>
        public ModelMaterialTexture RoughnessTexture { get; set; }

        /// <summary>
        /// Sheen 启用条件：颜色因子不为零
        /// </summary>
        public override bool IsEnabled => ColorFactor.Length() > 0f;

        public override IEnumerable<MaterialTextureSlot> GetTextureSlots() {
            if (ColorTexture != null) {
                yield return MaterialTextureSlot.SheenColor;
            }
            if (RoughnessTexture != null) {
                yield return MaterialTextureSlot.SheenRoughness;
            }
        }

        public override void LoadFromGltf(GltfMaterial material, ModelData modelData) {
            MaterialChannel? channel = material.FindChannel("SheenColor");
            if (channel != null) {
                System.Numerics.Vector4 color = channel.Value.Color;
                ColorFactor = new Vector3(color.X, color.Y, color.Z);
                ColorTexture = LoadTextureFromChannel(modelData, channel);
            }
            channel = material.FindChannel("SheenRoughness");
            if (channel != null) {
                RoughnessFactor = GetChannelFactor(channel, "RoughnessFactor", 0f);
                RoughnessTexture = LoadTextureFromChannel(modelData, channel);
            }
        }

        public override void AppendDefines(IShaderDefineBuilder defines) {
            if (IsEnabled) {
                defines.AddMaterialExtension("SHEEN");
                if (ColorTexture != null) {
                    defines.AddTextureMap("SHEEN_COLOR");
                    if (ColorTexture.HasUVTransform) {
                        defines.AddUVTransform("SHEENCOLOR");
                    }
                }
                if (RoughnessTexture != null) {
                    defines.AddTextureMap("SHEEN_ROUGHNESS");
                    if (RoughnessTexture.HasUVTransform) {
                        defines.AddUVTransform("SHEENROUGHNESS");
                    }
                }
            }
        }
    }
}