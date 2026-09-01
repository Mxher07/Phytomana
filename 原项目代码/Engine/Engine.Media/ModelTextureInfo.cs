using Engine.Graphics;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;

namespace Engine.Media {
    /// <summary>
    /// 纹理类型枚举
    /// </summary>
    public enum ModelTextureType {
        None,
        BaseColor, // sRGB
        Normal, // Linear
        MetallicRoughness, // Linear
        Occlusion, // Linear
        Emissive // sRGB
    }

    /// <summary>
    /// 纹理信息（支持延迟加载）
    /// </summary>
    public class ModelTextureInfo {
        /// <summary>
        /// 纹理名称
        /// </summary>
        public string Name;

        /// <summary>
        /// 纹理类型
        /// </summary>
        public ModelTextureType Type;

        /// <summary>
        /// 是否为 sRGB 颜色空间
        /// </summary>
        public bool IsSrgb;

        /// <summary>
        /// 原始图像数据（用于延迟加载）
        /// </summary>
        public MemoryImage SourceImage;

        /// <summary>
        /// 采样器状态（从 glTF TextureSampler 转换）
        /// </summary>
        public SamplerState SamplerState { get; private set; }

        /// <summary>
        /// 从 glTF TextureSampler 创建 SamplerState
        /// </summary>
        public static SamplerState CreateSamplerState(TextureSampler sampler) {
            if (sampler == null) {
                // glTF 默认值：Linear 过滤，Repeat 环绕
                return SamplerState.LinearWrap;
            }
            SamplerState state = new();

            // 设置过滤模式
            state.FilterMode = TranslateFilterMode(sampler.MinFilter, sampler.MagFilter);

            // 设置环绕模式
            state.AddressModeU = TranslateAddressMode(sampler.WrapS);
            state.AddressModeV = TranslateAddressMode(sampler.WrapT);
            return state;
        }

        /// <summary>
        /// 设置采样器状态
        /// </summary>
        public void SetSampler(TextureSampler sampler) {
            SamplerState = CreateSamplerState(sampler);
        }

        public static TextureFilterMode TranslateFilterMode(TextureMipMapFilter minFilter, TextureInterpolationFilter magFilter) {
            // glTF 默认：LINEAR (minFilter) 和 LINEAR (magFilter)
            return (minFilter, magFilter) switch {
                (TextureMipMapFilter.NEAREST, TextureInterpolationFilter.NEAREST) => TextureFilterMode.Point,
                (TextureMipMapFilter.LINEAR, TextureInterpolationFilter.NEAREST) => TextureFilterMode.MinLinearMagPointMipPoint,
                (TextureMipMapFilter.NEAREST_MIPMAP_NEAREST, TextureInterpolationFilter.NEAREST) => TextureFilterMode.Point,
                (TextureMipMapFilter.LINEAR_MIPMAP_NEAREST, TextureInterpolationFilter.NEAREST) => TextureFilterMode.MinLinearMagPointMipPoint,
                (TextureMipMapFilter.NEAREST_MIPMAP_LINEAR, TextureInterpolationFilter.NEAREST) => TextureFilterMode.MinLinearMagPointMipLinear,
                (TextureMipMapFilter.LINEAR_MIPMAP_LINEAR, TextureInterpolationFilter.NEAREST) => TextureFilterMode.MinLinearMagPointMipLinear,
                (TextureMipMapFilter.NEAREST, TextureInterpolationFilter.LINEAR) => TextureFilterMode.MinPointMagLinearMipPoint,
                (TextureMipMapFilter.LINEAR, TextureInterpolationFilter.LINEAR) => TextureFilterMode.Linear,
                (TextureMipMapFilter.NEAREST_MIPMAP_NEAREST, TextureInterpolationFilter.LINEAR) => TextureFilterMode.MinPointMagLinearMipPoint,
                (TextureMipMapFilter.LINEAR_MIPMAP_NEAREST, TextureInterpolationFilter.LINEAR) => TextureFilterMode.LinearMipPoint,
                (TextureMipMapFilter.NEAREST_MIPMAP_LINEAR, TextureInterpolationFilter.LINEAR) => TextureFilterMode.MinPointMagLinearMipLinear,
                (TextureMipMapFilter.LINEAR_MIPMAP_LINEAR, TextureInterpolationFilter.LINEAR) => TextureFilterMode.Linear,
                _ => TextureFilterMode.Linear
            };
        }

        public static TextureAddressMode TranslateAddressMode(TextureWrapMode wrapMode) {
            return wrapMode switch {
                TextureWrapMode.CLAMP_TO_EDGE => TextureAddressMode.Clamp,
                TextureWrapMode.MIRRORED_REPEAT => TextureAddressMode.MirrorWrap,
                _ => TextureAddressMode.Wrap // REPEAT is default
            };
        }
    }
}