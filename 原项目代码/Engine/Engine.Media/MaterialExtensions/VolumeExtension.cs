using SharpGLTF.Schema2;
using GltfMaterial = SharpGLTF.Schema2.Material;

namespace Engine.Media {
    /// <summary>
    /// KHR_materials_volume 扩展
    /// 支持体积吸收、厚度控制
    /// </summary>
    public class VolumeExtension : MaterialExtension {
        public override string ExtensionName => "KHR_materials_volume";

        /// <summary>
        /// 厚度因子
        /// </summary>
        public float ThicknessFactor { get; set; }

        /// <summary>
        /// 厚度纹理
        /// </summary>
        public ModelMaterialTexture ThicknessTexture { get; set; }

        /// <summary>
        /// 衰减距离（0 或 float.MaxValue 表示无限，不衰减）
        /// 官方规范默认值是无限大
        /// </summary>
        public float AttenuationDistance { get; set; } = float.MaxValue;

        /// <summary>
        /// 衰减颜色
        /// </summary>
        public Vector3 AttenuationColor { get; set; } = Vector3.One;

        /// <summary>
        /// 标记扩展是否被加载
        /// </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// 只要扩展被加载就认为是启用的
        /// </summary>
        public override bool IsEnabled => IsLoaded;

        public override IEnumerable<MaterialTextureSlot> GetTextureSlots() {
            if (ThicknessTexture != null) {
                yield return MaterialTextureSlot.Thickness;
            }
        }

        public override void LoadFromGltf(GltfMaterial material, ModelData modelData) {
            MaterialChannel? thicknessChannel = material.FindChannel("VolumeThickness");
            MaterialChannel? attenuationChannel = material.FindChannel("VolumeAttenuation");
            if (thicknessChannel != null
                || attenuationChannel != null) {
                IsLoaded = true;
                if (thicknessChannel != null) {
                    ThicknessFactor = GetChannelFactor(thicknessChannel, "ThicknessFactor", 0f);
                    ThicknessTexture = LoadTextureFromChannel(modelData, thicknessChannel);
                }
                if (attenuationChannel != null) {
                    float attDist = GetChannelFactor(attenuationChannel, "AttenuationDistance", float.MaxValue);
                    AttenuationDistance = attDist == 0f ? float.MaxValue : attDist;
                    System.Numerics.Vector4 color = attenuationChannel.Value.Color;
                    AttenuationColor = new Vector3(color.X, color.Y, color.Z);
                }
            }
        }

        public override void AppendDefines(IShaderDefineBuilder defines) {
            if (IsEnabled) {
                defines.AddMaterialExtension("VOLUME");
                if (ThicknessTexture != null) {
                    defines.AddTextureMap("THICKNESS");
                    if (ThicknessTexture.HasUVTransform) {
                        defines.AddUVTransform("THICKNESS");
                    }
                }
            }
        }
    }
}