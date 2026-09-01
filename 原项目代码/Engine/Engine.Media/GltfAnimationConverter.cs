using Engine.Animation;
using Engine.Graphics;
using SharpGLTF.Animations;
using SharpGLTF.Schema2;

namespace Engine.Media {
    public static class GltfAnimationConverter {
        public static void ConvertAnimations(ModelRoot modelRoot, ModelData modelData) {
            // 构建 material source index → ModelMaterial 查找表
            Dictionary<int, ModelMaterial> materialsByIndex = new();
            foreach (ModelMaterial mat in modelData.Materials) {
                if (mat.SourceMaterialIndex >= 0) {
                    materialsByIndex[mat.SourceMaterialIndex] = mat;
                }
            }
            foreach (SharpGLTF.Schema2.Animation anim in modelRoot.LogicalAnimations) {
                ModelAnimation modelAnim = new() { Name = anim.Name ?? $"Animation{anim.LogicalIndex}", Duration = anim.Duration };
                foreach (AnimationChannel channel in anim.Channels) {
                    if (channel.TargetNodePath == PropertyPath.pointer) {
                        // KHR_animation_pointer 通道
                        string path = channel.TargetPointerPath;
                        if (path != null
                            && path.StartsWith("/nodes/")) {
                            Action<float, Model> nodeTarget = CreateNodeVisibilityTarget(
                                channel,
                                modelData.GltfNodeToMeshIndex,
                                modelData.GltfNodeToLightIndex
                            );
                            if (nodeTarget != null) {
                                modelAnim.NodeVisibilityTargets.Add(nodeTarget);
                            }
                        }
                        else {
                            Action<float> target = CreatePointerTarget(channel, materialsByIndex);
                            if (target != null) {
                                modelAnim.PointerTargets.Add(target);
                            }
                        }
                    }
                    else {
                        // 标准骨骼动画通道
                        ModelAnimation.AnimationChannel modelChannel = new() {
                            TargetBoneName = channel.TargetNode?.Name ?? $"Node{channel.TargetNode?.LogicalIndex ?? 0}",
                            Property = ConvertAnimationProperty(channel.TargetNodePath)
                        };
                        modelChannel.Sampler = ConvertSamplerByPath(channel);
                        modelAnim.Channels.Add(modelChannel);
                    }
                }
                modelData.Animations.Add(modelAnim);
            }
        }

        public static ModelAnimation.AnimationProperty ConvertAnimationProperty(PropertyPath path) {
            return path switch {
                PropertyPath.translation => ModelAnimation.AnimationProperty.Translation,
                PropertyPath.rotation => ModelAnimation.AnimationProperty.Rotation,
                PropertyPath.scale => ModelAnimation.AnimationProperty.Scale,
                PropertyPath.weights => ModelAnimation.AnimationProperty.Weights,
                _ => ModelAnimation.AnimationProperty.Translation
            };
        }

        public static ModelAnimation.AnimationSampler ConvertSamplerByPath(AnimationChannel channel) {
            ModelAnimation.AnimationSampler result = new();
            PropertyPath path = channel.TargetNodePath;
            try {
                // 获取关键帧数量和时长
                float duration = channel.LogicalParent.Duration;
                int keyCount = EstimateKeyFrameCount(channel);
                if (keyCount == 0) {
                    return result;
                }

                // 均匀采样动画数据
                List<float> times = new();
                List<Vector3> translations = new();
                List<Quaternion> rotations = new();
                List<Vector3> scales = new();
                List<float[]> weights = new();
                for (int i = 0; i <= keyCount; i++) {
                    float t = duration * i / keyCount;
                    times.Add(t);
                    if (path == PropertyPath.translation) {
                        IAnimationSampler<System.Numerics.Vector3> sampler = channel.GetSamplerOrNull<System.Numerics.Vector3>();
                        if (sampler != null) {
                            ICurveSampler<System.Numerics.Vector3> curveSampler = sampler.CreateCurveSampler(true);
                            System.Numerics.Vector3 value = curveSampler.GetPoint(t);
                            translations.Add(new Vector3(value.X, value.Y, value.Z));
                        }
                    }
                    else if (path == PropertyPath.rotation) {
                        IAnimationSampler<System.Numerics.Quaternion> sampler = channel.GetSamplerOrNull<System.Numerics.Quaternion>();
                        if (sampler != null) {
                            ICurveSampler<System.Numerics.Quaternion> curveSampler = sampler.CreateCurveSampler(true);
                            System.Numerics.Quaternion value = curveSampler.GetPoint(t);
                            rotations.Add(new Quaternion(value.X, value.Y, value.Z, value.W));
                        }
                    }
                    else if (path == PropertyPath.scale) {
                        IAnimationSampler<System.Numerics.Vector3> sampler = channel.GetSamplerOrNull<System.Numerics.Vector3>();
                        if (sampler != null) {
                            ICurveSampler<System.Numerics.Vector3> curveSampler = sampler.CreateCurveSampler(true);
                            System.Numerics.Vector3 value = curveSampler.GetPoint(t);
                            scales.Add(new Vector3(value.X, value.Y, value.Z));
                        }
                    }
                    else if (path == PropertyPath.weights) {
                        IAnimationSampler<float[]> sampler = channel.GetSamplerOrNull<float[]>();
                        if (sampler != null) {
                            ICurveSampler<float[]> curveSampler = sampler.CreateCurveSampler(true);
                            float[] value = curveSampler.GetPoint(t);
                            weights.Add(value);
                        }
                    }
                }
                result.KeyTimes = times.ToArray();
                result.Translations = translations.ToArray();
                result.Rotations = rotations.ToArray();
                result.Scales = scales.ToArray();
                result.Weights = weights.ToArray();
                result.Interpolation = ModelAnimation.InterpolationType.Linear;
            }
            catch (Exception ex) {
                // 动画转换失败时记录错误，但继续处理其他动画
                // 注意：这里不抛出异常，允许部分动画数据加载成功
                Log.Warning($"[GltfLoader] Animation conversion warning: {ex.Message}");
            }
            return result;
        }

        public static Action<float> CreatePointerTarget(AnimationChannel channel, Dictionary<int, ModelMaterial> materialsByIndex) {
            string path = channel.TargetPointerPath;
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            string[] segments = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 3) {
                return null;
            }
            try {
                if (segments[0] == "materials") {
                    return CreateMaterialPointerTarget(segments, channel, materialsByIndex);
                }
            }
            catch (Exception ex) {
                Log.Warning($"[GltfLoader] Pointer target failed for {path}: {ex.Message}");
            }
            return null;
        }

        public static Action<float> CreateMaterialPointerTarget(string[] segments,
            AnimationChannel channel,
            Dictionary<int, ModelMaterial> materialsByIndex) {
            if (!int.TryParse(segments[1], out int materialIndex)) {
                return null;
            }
            if (!materialsByIndex.TryGetValue(materialIndex, out ModelMaterial mat)) {
                return null;
            }
            string propertyPath = string.Join("/", segments, 2, segments.Length - 2);

            // Core PBR
            switch (propertyPath) {
                case "pbrMetallicRoughness/baseColorFactor":
                    return CreateVec4Target(
                        channel,
                        v => {
                            mat.BaseColorFactor = v;
                            mat.Version++;
                        }
                    );
                case "pbrMetallicRoughness/metallicFactor":
                    return CreateFloatTarget(
                        channel,
                        v => {
                            mat.MetallicFactor = v;
                            mat.Version++;
                        }
                    );
                case "pbrMetallicRoughness/roughnessFactor":
                    return CreateFloatTarget(
                        channel,
                        v => {
                            mat.RoughnessFactor = v;
                            mat.Version++;
                        }
                    );
                case "emissiveFactor":
                    return CreateVec3Target(
                        channel,
                        v => {
                            mat.EmissiveFactor = v;
                            mat.Version++;
                        }
                    );
                case "alphaCutoff":
                    return CreateFloatTarget(
                        channel,
                        v => {
                            mat.AlphaCutoff = v;
                            mat.Version++;
                        }
                    );
            }

            // Texture transform
            if (propertyPath.Contains("/extensions/KHR_texture_transform/")) {
                return CreateTextureTransformTarget(propertyPath, channel, mat);
            }

            // Extensions
            if (propertyPath.StartsWith("extensions/")) {
                return CreateExtensionTarget(propertyPath.Substring(11), channel, mat);
            }
            return null;
        }

        public static Action<float> CreateTextureTransformTarget(string propertyPath, AnimationChannel channel, ModelMaterial mat) {
            const string suffix = "/extensions/KHR_texture_transform/";
            int idx = propertyPath.IndexOf(suffix);
            if (idx < 0) {
                return null;
            }
            string texturePath = propertyPath.Substring(0, idx);
            string propName = propertyPath.Substring(idx + suffix.Length);
            ModelMaterialTexture tex = GetMaterialTexture(mat, texturePath);
            if (tex == null) {
                return null;
            }
            return propName switch {
                "offset" => CreateVec2Target(
                    channel,
                    v => {
                        tex.Offset = v;
                        tex.RecomputeUVTransform();
                        mat.Version++;
                    }
                ),
                "scale" => CreateVec2Target(
                    channel,
                    v => {
                        tex.Scale = v;
                        tex.RecomputeUVTransform();
                        mat.Version++;
                    }
                ),
                "rotation" => CreateFloatTarget(
                    channel,
                    v => {
                        tex.Rotation = v;
                        tex.RecomputeUVTransform();
                        mat.Version++;
                    }
                ),
                _ => null
            };
        }

        public static ModelMaterialTexture GetMaterialTexture(ModelMaterial mat, string texturePath) {
            if (texturePath == "pbrMetallicRoughness/baseColorTexture") {
                return mat.BaseColorTexture;
            }
            if (texturePath == "pbrMetallicRoughness/metallicRoughnessTexture") {
                return mat.MetallicRoughnessTexture;
            }
            if (texturePath == "normalTexture") {
                return mat.NormalTexture;
            }
            if (texturePath == "occlusionTexture") {
                return mat.OcclusionTexture;
            }
            if (texturePath == "emissiveTexture") {
                return mat.EmissiveTexture;
            }
            if (texturePath == "extensions/KHR_materials_clearcoat/clearcoatTexture") {
                return mat.ClearCoat?.Texture;
            }
            if (texturePath == "extensions/KHR_materials_clearcoat/clearcoatRoughnessTexture") {
                return mat.ClearCoat?.RoughnessTexture;
            }
            if (texturePath == "extensions/KHR_materials_clearcoat/clearcoatNormalTexture") {
                return mat.ClearCoat?.NormalTexture;
            }
            if (texturePath == "extensions/KHR_materials_sheen/sheenColorTexture") {
                return mat.Sheen?.ColorTexture;
            }
            if (texturePath == "extensions/KHR_materials_sheen/sheenRoughnessTexture") {
                return mat.Sheen?.RoughnessTexture;
            }
            if (texturePath == "extensions/KHR_materials_transmission/transmissionTexture") {
                return mat.Transmission?.Texture;
            }
            if (texturePath == "extensions/KHR_materials_volume/thicknessTexture") {
                return mat.Volume?.ThicknessTexture;
            }
            if (texturePath == "extensions/KHR_materials_iridescence/iridescenceTexture") {
                return mat.Iridescence?.Texture;
            }
            if (texturePath == "extensions/KHR_materials_iridescence/iridescenceThicknessTexture") {
                return mat.Iridescence?.ThicknessTexture;
            }
            if (texturePath == "extensions/KHR_materials_specular/specularTexture") {
                return mat.Specular?.SpecularTexture;
            }
            if (texturePath == "extensions/KHR_materials_specular/specularColorTexture") {
                return mat.Specular?.SpecularColorTexture;
            }
            if (texturePath == "extensions/KHR_materials_anisotropy/anisotropyTexture") {
                return mat.Anisotropy?.AnisotropyTexture;
            }
            if (texturePath == "extensions/KHR_materials_diffuse_transmission/diffuseTransmissionTexture") {
                return mat.DiffuseTransmission?.Texture;
            }
            if (texturePath == "extensions/KHR_materials_diffuse_transmission/diffuseTransmissionColorTexture") {
                return mat.DiffuseTransmission?.ColorTexture;
            }
            return null;
        }

        public static Action<float> CreateExtensionTarget(string extPath, AnimationChannel channel, ModelMaterial mat) {
            string[] parts = extPath.Split('/');
            if (parts.Length < 2) {
                return null;
            }
            string ext = parts[0], prop = parts[1];
            switch (ext) {
                case "KHR_materials_emissive_strength":
                    if (mat.EmissiveStrength != null
                        && prop == "emissiveStrength") {
                        return CreateFloatTarget(
                            channel,
                            v => {
                                mat.EmissiveStrength.EmissiveStrength = v;
                                mat.Version++;
                            }
                        );
                    }
                    break;
                case "KHR_materials_ior":
                    if (mat.Ior != null
                        && prop == "ior") {
                        return CreateFloatTarget(
                            channel,
                            v => {
                                mat.Ior.Ior = v;
                                mat.Version++;
                            }
                        );
                    }
                    break;
                case "KHR_materials_specular":
                    if (mat.Specular != null) {
                        if (prop == "specularFactor") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.Specular.SpecularFactor = v;
                                    mat.Version++;
                                }
                            );
                        }
                        if (prop == "specularColorFactor") {
                            return CreateVec3Target(
                                channel,
                                v => {
                                    mat.Specular.SpecularColorFactor = v;
                                    mat.Version++;
                                }
                            );
                        }
                    }
                    break;
                case "KHR_materials_sheen":
                    if (mat.Sheen != null) {
                        if (prop == "sheenColorFactor") {
                            return CreateVec3Target(
                                channel,
                                v => {
                                    mat.Sheen.ColorFactor = v;
                                    mat.Version++;
                                }
                            );
                        }
                        if (prop == "sheenRoughnessFactor") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.Sheen.RoughnessFactor = v;
                                    mat.Version++;
                                }
                            );
                        }
                    }
                    break;
                case "KHR_materials_clearcoat":
                    if (mat.ClearCoat != null) {
                        if (prop == "clearcoatFactor") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.ClearCoat.Factor = v;
                                    mat.Version++;
                                }
                            );
                        }
                        if (prop == "clearcoatRoughnessFactor") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.ClearCoat.RoughnessFactor = v;
                                    mat.Version++;
                                }
                            );
                        }
                    }
                    break;
                case "KHR_materials_transmission":
                    if (mat.Transmission != null
                        && prop == "transmissionFactor") {
                        return CreateFloatTarget(
                            channel,
                            v => {
                                mat.Transmission.Factor = v;
                                mat.Version++;
                            }
                        );
                    }
                    break;
                case "KHR_materials_volume":
                    if (mat.Volume != null) {
                        if (prop == "thicknessFactor") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.Volume.ThicknessFactor = v;
                                    mat.Version++;
                                }
                            );
                        }
                        if (prop == "attenuationDistance") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.Volume.AttenuationDistance = v;
                                    mat.Version++;
                                }
                            );
                        }
                        if (prop == "attenuationColor") {
                            return CreateVec3Target(
                                channel,
                                v => {
                                    mat.Volume.AttenuationColor = v;
                                    mat.Version++;
                                }
                            );
                        }
                    }
                    break;
                case "KHR_materials_iridescence":
                    if (mat.Iridescence != null) {
                        if (prop == "iridescenceFactor") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.Iridescence.Factor = v;
                                    mat.Version++;
                                }
                            );
                        }
                        if (prop == "iridescenceIor") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.Iridescence.IOR = v;
                                    mat.Version++;
                                }
                            );
                        }
                        if (prop == "iridescenceThicknessMinimum") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.Iridescence.ThicknessMinimum = v;
                                    mat.Version++;
                                }
                            );
                        }
                        if (prop == "iridescenceThicknessMaximum") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.Iridescence.ThicknessMaximum = v;
                                    mat.Version++;
                                }
                            );
                        }
                    }
                    break;
                case "KHR_materials_anisotropy":
                    if (mat.Anisotropy != null) {
                        if (prop == "anisotropyStrength") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.Anisotropy.AnisotropyStrength = v;
                                    mat.Version++;
                                }
                            );
                        }
                        if (prop == "anisotropyRotation") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.Anisotropy.AnisotropyRotation = v;
                                    mat.Version++;
                                }
                            );
                        }
                    }
                    break;
                case "KHR_materials_dispersion":
                    if (mat.Dispersion != null
                        && prop == "dispersion") {
                        return CreateFloatTarget(
                            channel,
                            v => {
                                mat.Dispersion.Dispersion = v;
                                mat.Version++;
                            }
                        );
                    }
                    break;
                case "KHR_materials_volume_scatter":
                    if (mat.VolumeScatter != null) {
                        if (prop == "multiscatterColor") {
                            return CreateVec3Target(
                                channel,
                                v => {
                                    mat.VolumeScatter.MultiscatterColor = v;
                                    mat.Version++;
                                }
                            );
                        }
                        if (prop == "scatterAnisotropy") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.VolumeScatter.ScatterAnisotropy = v;
                                    mat.Version++;
                                }
                            );
                        }
                    }
                    break;
                case "KHR_materials_diffuse_transmission":
                    if (mat.DiffuseTransmission != null) {
                        if (prop == "diffuseTransmissionFactor") {
                            return CreateFloatTarget(
                                channel,
                                v => {
                                    mat.DiffuseTransmission.Factor = v;
                                    mat.Version++;
                                }
                            );
                        }
                        if (prop == "diffuseTransmissionColorFactor") {
                            return CreateVec3Target(
                                channel,
                                v => {
                                    mat.DiffuseTransmission.ColorFactor = v;
                                    mat.Version++;
                                }
                            );
                        }
                    }
                    break;
            }
            return null;
        }

        // Typed closure factories — isolateMemory=true ensures independence from ModelRoot

        public static Action<float> CreateFloatTarget(AnimationChannel channel, Action<float> set) {
            IAnimationSampler<float> sampler = channel.GetSamplerOrNull<float>();
            if (sampler == null) {
                return null;
            }
            ICurveSampler<float> curve = sampler.CreateCurveSampler(true);
            return time => set(curve.GetPoint(time));
        }

        public static Action<float> CreateVec2Target(AnimationChannel channel, Action<Vector2> set) {
            IAnimationSampler<System.Numerics.Vector2> sampler = channel.GetSamplerOrNull<System.Numerics.Vector2>();
            if (sampler == null) {
                return null;
            }
            ICurveSampler<System.Numerics.Vector2> curve = sampler.CreateCurveSampler(true);
            return time => set(new Vector2(curve.GetPoint(time).X, curve.GetPoint(time).Y));
        }

        public static Action<float> CreateVec3Target(AnimationChannel channel, Action<Vector3> set) {
            IAnimationSampler<System.Numerics.Vector3> sampler = channel.GetSamplerOrNull<System.Numerics.Vector3>();
            if (sampler == null) {
                return null;
            }
            ICurveSampler<System.Numerics.Vector3> curve = sampler.CreateCurveSampler(true);
            return time => {
                System.Numerics.Vector3 v = curve.GetPoint(time);
                set(new Vector3(v.X, v.Y, v.Z));
            };
        }

        public static Action<float> CreateVec4Target(AnimationChannel channel, Action<Vector4> set) {
            IAnimationSampler<System.Numerics.Vector4> sampler = channel.GetSamplerOrNull<System.Numerics.Vector4>();
            if (sampler == null) {
                return null;
            }
            ICurveSampler<System.Numerics.Vector4> curve = sampler.CreateCurveSampler(true);
            return time => {
                System.Numerics.Vector4 v = curve.GetPoint(time);
                set(new Vector4(v.X, v.Y, v.Z, v.W));
            };
        }

        public static int EstimateKeyFrameCount(AnimationChannel channel) {
            float duration = channel.LogicalParent.Duration;
            int estimatedFrames = Math.Max(1, (int)(duration * 30f));
            return Math.Min(estimatedFrames, 300);
        }

        public static Action<float, Model> CreateNodeVisibilityTarget(AnimationChannel channel,
            Dictionary<int, int> nodeToMeshIndex,
            Dictionary<int, int> nodeToLightIndex) {
            string path = channel.TargetPointerPath;
            // Expected: /nodes/{index}/extensions/KHR_node_visibility/visible
            if (!path.StartsWith("/nodes/")) {
                return null;
            }
            string[] segments = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 5
                || segments[0] != "nodes"
                || segments[2] != "extensions") {
                return null;
            }
            if (!int.TryParse(segments[1], out int nodeIndex)) {
                return null;
            }
            IAnimationSampler<float> sampler = channel.GetSamplerOrNull<float>();
            if (sampler == null) {
                return null;
            }
            ICurveSampler<float> curve = sampler.CreateCurveSampler(true);

            // 查找此节点对应的 mesh 和/或 light
            int meshIndex = nodeToMeshIndex.TryGetValue(nodeIndex, out int mi) ? mi : -1;
            int lightIndex = nodeToLightIndex.TryGetValue(nodeIndex, out int li) ? li : -1;
            if (meshIndex < 0
                && lightIndex < 0) {
                return null;
            }
            return (time, model) => {
                if (model == null) {
                    return;
                }
                float value = curve.GetPoint(time);
                bool visible = value >= 0.5f;
                if (meshIndex >= 0
                    && meshIndex < model.Meshes.Count) {
                    // 一个 glTF mesh 可能被拆分为多个 ModelMesh（每个 primitive 一个），
                    // 通过 ParentBone 查找所有同级 mesh
                    ModelBone bone = model.Meshes[meshIndex].ParentBone;
                    for (int i = 0; i < model.Meshes.Count; i++) {
                        if (model.Meshes[i].ParentBone == bone) {
                            model.Meshes[i].IsVisible = visible;
                        }
                    }
                }
                if (lightIndex >= 0
                    && lightIndex < model.Lights.Count) {
                    model.Lights[lightIndex].IsVisible = visible;
                }
            };
        }
    }
}