namespace Engine.Media {
    /// <summary>
    /// 材质数据（PBR 材质属性）
    /// </summary>
    public class ModelMaterial {
        /// <summary>
        /// 材质名称
        /// </summary>
        public string Name;

        // Core PBR factors
        public Vector4 BaseColorFactor = Vector4.One;
        public float MetallicFactor = 1f;
        public float RoughnessFactor = 1f;
        public Vector3 EmissiveFactor = Vector3.Zero;

        // Core PBR textures (with UV transform support)
        public ModelMaterialTexture BaseColorTexture;
        public ModelMaterialTexture MetallicRoughnessTexture;
        public ModelMaterialTexture NormalTexture;
        public ModelMaterialTexture OcclusionTexture;
        public ModelMaterialTexture EmissiveTexture;

        // Texture parameters
        public float NormalScale = 1f;
        public float OcclusionStrength = 1f;

        // Alpha
        public ModelAlphaMode AlphaMode = ModelAlphaMode.Opaque;
        public float AlphaCutoff = 0.5f;
        public bool DoubleSided;

        /// <summary>
        /// 源材质在 glTF 中的逻辑索引（用于 KHR_animation_pointer）
        /// </summary>
        public int SourceMaterialIndex { get; set; } = -1;

        /// <summary>
        /// 材质版本号，每次 pointer 动画修改属性时递增。
        /// 渲染器用于检测材质变化并重建 UBO。
        /// </summary>
        public int Version;

        // Extensions - known types for convenience
        public ClearCoatExtension ClearCoat { get; set; }
        public IridescenceExtension Iridescence { get; set; }
        public TransmissionExtension Transmission { get; set; }
        public VolumeExtension Volume { get; set; }
        public SheenExtension Sheen { get; set; }
        public SpecularExtension Specular { get; set; }
        public IorExtension Ior { get; set; }
        public EmissiveStrengthExtension EmissiveStrength { get; set; }
        public DispersionExtension Dispersion { get; set; }
        public AnisotropyExtension Anisotropy { get; set; }
        public DiffuseTransmissionExtension DiffuseTransmission { get; set; }
        public VolumeScatterExtension VolumeScatter { get; set; }
        public UnlitExtension Unlit { get; set; }
        public SpecularGlossinessExtension SpecularGlossiness { get; set; }

        // Dynamic extensions storage
        public readonly Dictionary<string, MaterialExtension> m_extensions = new();

        /// <summary>
        /// 将材质数据填充到着色器定义构建器
        /// </summary>
        public void PopulateDefines(IShaderDefineBuilder builder) {
            // Core textures + UV transforms
            if (BaseColorTexture?.HasTexture == true) {
                builder.AddTextureMap("BASE_COLOR");
                if (BaseColorTexture.HasUVTransform) {
                    builder.AddUVTransform("BASECOLOR");
                }
            }
            if (NormalTexture?.HasTexture == true) {
                builder.AddTextureMap("NORMAL");
                if (NormalTexture.HasUVTransform) {
                    builder.AddUVTransform("NORMAL");
                }
            }
            if (MetallicRoughnessTexture?.HasTexture == true) {
                builder.AddTextureMap("METALLIC_ROUGHNESS");
                if (MetallicRoughnessTexture.HasUVTransform) {
                    builder.AddUVTransform("METALLICROUGHNESS");
                }
            }
            if (OcclusionTexture?.HasTexture == true) {
                builder.AddTextureMap("OCCLUSION");
                if (OcclusionTexture.HasUVTransform) {
                    builder.AddUVTransform("OCCLUSION");
                }
            }
            if (EmissiveTexture?.HasTexture == true) {
                builder.AddTextureMap("EMISSIVE");
                if (EmissiveTexture.HasUVTransform) {
                    builder.AddUVTransform("EMISSIVE");
                }
            }

            // Extensions
            if (ClearCoat?.IsEnabled == true) {
                ClearCoat.AppendDefines(builder);
            }
            if (Iridescence?.IsEnabled == true) {
                Iridescence.AppendDefines(builder);
            }
            if (Transmission?.IsEnabled == true) {
                Transmission.AppendDefines(builder);
            }
            if (Volume?.IsEnabled == true) {
                Volume.AppendDefines(builder);
            }
            if (Sheen?.IsEnabled == true) {
                Sheen.AppendDefines(builder);
            }
            if (Specular?.IsEnabled == true) {
                Specular.AppendDefines(builder);
            }
            if (Ior?.IsEnabled == true) {
                Ior.AppendDefines(builder);
            }
            if (EmissiveStrength?.IsEnabled == true) {
                EmissiveStrength.AppendDefines(builder);
            }
            if (Dispersion?.IsEnabled == true) {
                Dispersion.AppendDefines(builder);
            }
            if (Anisotropy?.IsEnabled == true) {
                Anisotropy.AppendDefines(builder);
            }
            if (DiffuseTransmission?.IsEnabled == true) {
                DiffuseTransmission.AppendDefines(builder);
            }
            if (VolumeScatter?.IsEnabled == true) {
                VolumeScatter.AppendDefines(builder);
            }
            if (Unlit?.IsEnabled == true) {
                Unlit.AppendDefines(builder);
            }
            if (SpecularGlossiness?.IsEnabled == true) {
                SpecularGlossiness.AppendDefines(builder);
            }

            // Dynamic extensions
            foreach (MaterialExtension ext in m_extensions.Values) {
                if (ext?.IsEnabled == true) {
                    ext.AppendDefines(builder);
                }
            }
        }
    }

    /// <summary>
    /// Alpha 模式
    /// </summary>
    public enum ModelAlphaMode {
        Opaque,
        Mask,
        Blend
    }
}