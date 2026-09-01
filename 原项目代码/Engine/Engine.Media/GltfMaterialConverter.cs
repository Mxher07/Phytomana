using System.Reflection;
using SharpGLTF.IO;
using SharpGLTF.Schema2;
using GltfImage = SharpGLTF.Schema2.Image;
using GltfTexture = SharpGLTF.Schema2.Texture;
using GltfMaterial = SharpGLTF.Schema2.Material;

namespace Engine.Media {
    public static class GltfMaterialConverter {
        /// <summary>
        /// 加载纹理和材质，建立索引映射
        /// </summary>
        public static void ConvertTexturesAndMaterials(ModelRoot modelRoot,
            ModelData modelData,
            Dictionary<GltfTexture, int> textureToIndex,
            Dictionary<GltfMaterial, int> materialToIndex) {
            // 1. 分析纹理用途，确定 sRGB vs Linear
            Dictionary<int, bool> textureIsSrgb = new();
            foreach (GltfTexture tex in modelRoot.LogicalTextures) {
                textureIsSrgb[tex.LogicalIndex] = true; // 默认 sRGB
            }
            foreach (GltfMaterial material in modelRoot.LogicalMaterials) {
                AnalyzeTextureColorSpace(material, textureIsSrgb);
            }

            // 2. 加载所有纹理（延迟加载模式）
            foreach (GltfTexture gltfTexture in modelRoot.LogicalTextures) {
                GltfImage image = gltfTexture.PrimaryImage ?? gltfTexture.FallbackImage;
                if (image?.Content == null) {
                    continue;
                }
                int texIndex = modelData.Textures.Count;
                textureToIndex[gltfTexture] = texIndex;
                modelData.GltfTextureToModelIndex[gltfTexture.LogicalIndex] = texIndex;
                bool isSrgb = textureIsSrgb.GetValueOrDefault(gltfTexture.LogicalIndex, true);
                ModelTextureInfo texInfo = new() { Name = image.Name ?? $"Texture{image.LogicalIndex}", IsSrgb = isSrgb };
                string sourcePath = image.Content.SourcePath;
                if (!string.IsNullOrEmpty(sourcePath)) {
                    texInfo.SourceImage = image.Content;
                }
                else {
                    texInfo.SourceImage = image.Content;
                }
                texInfo.SetSampler(gltfTexture.Sampler);
                modelData.Textures.Add(texInfo);
            }

            // 3. 加载所有材质
            foreach (GltfMaterial gltfMaterial in modelRoot.LogicalMaterials) {
                int matIndex = modelData.Materials.Count;
                materialToIndex[gltfMaterial] = matIndex;
                ModelMaterial mat = new() { Name = gltfMaterial.Name ?? $"Material{gltfMaterial.LogicalIndex}" };
                LoadMaterialProperties(gltfMaterial, mat, modelData);
                modelData.Materials.Add(mat);
            }
        }

        /// <summary>
        /// 分析纹理颜色空间（sRGB vs Linear）
        /// </summary>
        public static void AnalyzeTextureColorSpace(GltfMaterial material, Dictionary<int, bool> textureIsSrgb) {
            // sRGB channels: BaseColor, Emissive
            // Linear channels: Normal, MetallicRoughness, Occlusion

            void MarkTexture(string channelKey, bool isSrgb) {
                MaterialChannel? channel = material.FindChannel(channelKey);
                if (channel?.Texture is { } tex) {
                    textureIsSrgb[tex.LogicalIndex] = isSrgb;
                }
            }

            MarkTexture("BaseColor", true);
            MarkTexture("Emissive", true);
            MarkTexture("Normal", false);
            MarkTexture("MetallicRoughness", false);
            MarkTexture("Occlusion", false);
        }

        /// <summary>
        /// 加载材质属性
        /// </summary>
        public static void LoadMaterialProperties(GltfMaterial gltfMaterial, ModelMaterial mat, ModelData modelData) {
            // BaseColor
            MaterialChannel? channel = gltfMaterial.FindChannel("BaseColor");
            if (channel != null) {
                System.Numerics.Vector4 color = channel.Value.Color;
                mat.BaseColorFactor = new Vector4(color.X, color.Y, color.Z, color.W);
                mat.BaseColorTexture = LoadMaterialTexture(channel, modelData);
            }

            // MetallicRoughness
            channel = gltfMaterial.FindChannel("MetallicRoughness");
            if (channel != null) {
                mat.MetallicFactor = GetFactorSafe(channel.Value, "MetallicFactor", 1f);
                mat.RoughnessFactor = GetFactorSafe(channel.Value, "RoughnessFactor", 1f);
                mat.MetallicRoughnessTexture = LoadMaterialTexture(channel, modelData);
            }

            // Normal
            channel = gltfMaterial.FindChannel("Normal");
            if (channel != null) {
                mat.NormalScale = GetFactorSafe(channel.Value, "NormalScale", 1f);
                mat.NormalTexture = LoadMaterialTexture(channel, modelData);
            }

            // Occlusion
            channel = gltfMaterial.FindChannel("Occlusion");
            if (channel != null) {
                mat.OcclusionStrength = GetFactorSafe(channel.Value, "OcclusionStrength", 1f);
                mat.OcclusionTexture = LoadMaterialTexture(channel, modelData);
            }

            // Emissive
            channel = gltfMaterial.FindChannel("Emissive");
            if (channel != null) {
                System.Numerics.Vector4 emissive = channel.Value.Color;
                mat.EmissiveFactor = new Vector3(emissive.X, emissive.Y, emissive.Z);
                mat.EmissiveTexture = LoadMaterialTexture(channel, modelData);
            }

            // Alpha mode
            mat.AlphaMode = gltfMaterial.Alpha switch {
                AlphaMode.BLEND => ModelAlphaMode.Blend,
                AlphaMode.MASK => ModelAlphaMode.Mask,
                _ => ModelAlphaMode.Opaque
            };
            mat.AlphaCutoff = gltfMaterial.AlphaCutoff;
            mat.DoubleSided = gltfMaterial.DoubleSided;

            // 源材质索引
            mat.SourceMaterialIndex = gltfMaterial.LogicalIndex;

            // 加载材质扩展
            LoadMaterialExtensions(gltfMaterial, mat, modelData);
        }

        /// <summary>
        /// 加载材质扩展
        /// </summary>
        public static void LoadMaterialExtensions(GltfMaterial gltfMaterial, ModelMaterial mat, ModelData modelData) {
            foreach (JsonSerializable jsonSerializable in gltfMaterial.Extensions) {
                Type type = jsonSerializable.GetType();
                string extName = null;

                // 获取扩展名称
                if (jsonSerializable is ExtraProperties) {
                    MethodInfo method = type.GetMethod("GetSchemaName", BindingFlags.Instance | BindingFlags.NonPublic);
                    extName = (string)method?.Invoke(jsonSerializable, null);
                }
                else {
                    PropertyInfo nameProp = type.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
                    extName = (string)nameProp?.GetValue(jsonSerializable);
                }
                if (extName == null
                    || !MaterialExtensionManager.IsExtensionEnabled(extName)) {
                    continue;
                }

                // 创建扩展实例
                MaterialExtension extension = MaterialExtensionRegistry.Create(extName);
                if (extension == null) {
                    continue;
                }

                // 加载扩展数据
                extension.LoadFromGltf(gltfMaterial, modelData);
                if (!extension.IsEnabled) {
                    continue;
                }

                // 存储到材质
                switch (extension) {
                    case ClearCoatExtension cc: mat.ClearCoat = cc; break;
                    case IridescenceExtension irid: mat.Iridescence = irid; break;
                    case TransmissionExtension trans: mat.Transmission = trans; break;
                    case VolumeExtension vol: mat.Volume = vol; break;
                    case SheenExtension sheen: mat.Sheen = sheen; break;
                    case SpecularExtension spec: mat.Specular = spec; break;
                    case IorExtension ior: mat.Ior = ior; break;
                    case EmissiveStrengthExtension emissiveStr: mat.EmissiveStrength = emissiveStr; break;
                    case DispersionExtension disp: mat.Dispersion = disp; break;
                    case AnisotropyExtension aniso: mat.Anisotropy = aniso; break;
                    case DiffuseTransmissionExtension diffTrans: mat.DiffuseTransmission = diffTrans; break;
                    case VolumeScatterExtension volScatter: mat.VolumeScatter = volScatter; break;
                    case UnlitExtension unlit: mat.Unlit = unlit; break;
                    case SpecularGlossinessExtension sg: mat.SpecularGlossiness = sg; break;
                }
            }
        }

        /// <summary>
        /// 从材质通道加载 ModelMaterialTexture
        /// </summary>
        public static ModelMaterialTexture LoadMaterialTexture(MaterialChannel? channel, ModelData modelData) {
            if (channel?.Texture == null) {
                return null;
            }
            int textureIndex = modelData.GetTextureIndex(channel.Value.Texture.LogicalIndex);
            if (textureIndex < 0) {
                return null;
            }
            int uvIndex = channel.Value.TextureCoordinate;
            ModelMaterialTexture matTex = new(textureIndex, uvIndex);

            // 读取 KHR_texture_transform 扩展
            TextureTransform transform = channel.Value.TextureTransform;
            if (transform != null) {
                matTex.SetTransform(
                    new Vector2(transform.Offset.X, transform.Offset.Y),
                    new Vector2(transform.Scale.X, transform.Scale.Y),
                    transform.Rotation
                );
            }
            return matTex;
        }

        public static float GetFactorSafe(MaterialChannel channel, string factorName, float defaultValue) {
            try {
                return channel.GetFactor(factorName);
            }
            catch {
                return defaultValue;
            }
        }
    }
}