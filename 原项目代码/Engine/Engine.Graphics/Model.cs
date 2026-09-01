#nullable disable
using Engine.Animation;
using Engine.Media;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace Engine.Graphics {
    public class Model : IDisposable {
        /// <summary>
        /// PBR 渲染器设为 true：sRGB 纹理用 Srgb8Alpha8 格式，GPU 自动 sRGB→线性，shader 输出时转回 sRGB。
        /// 原版渲染器保持 false：纹理保持 Rgba8888，值直接输出。
        /// </summary>
        public static bool LoadTexturesInSrgb { get; set; }

        public ModelBone m_rootBone;

        public List<ModelBone> m_bones = [];

        public List<ModelMesh> m_meshes = [];

        public ModelBone RootBone => m_rootBone;

        public ReadOnlyList<ModelBone> Bones => new(m_bones);

        /// <summary>
        /// 骨骼别名表（别名 -> 真实骨骼名）。
        /// FindBone 找不到精确匹配时会查此表作回退。null 表示不启用别名（dae 模型默认）。
        /// 由 AnimationConfig.boneAliases 在 ComponentModel.SetModel 中写入。
        /// </summary>
        public Dictionary<string, string> BoneAliases { get; set; }

        public ReadOnlyList<ModelMesh> Meshes => new(m_meshes);

        public ModelData ModelData { get; set; }

        public bool m_hasTransmission;
        public bool m_hasScatter;
        public bool m_materialCacheComputed;

        /// <summary>
        /// 模型是否包含 transmission 材质（缓存，惰性计算）
        /// </summary>
        public bool HasTransmission {
            get {
                EnsureMaterialCache();
                return m_hasTransmission;
            }
        }

        /// <summary>
        /// 模型是否包含 volume scatter 材质（缓存，惰性计算）
        /// </summary>
        public bool HasScatter {
            get {
                EnsureMaterialCache();
                return m_hasScatter;
            }
        }

        public void EnsureMaterialCache() {
            if (m_materialCacheComputed) return;
            m_materialCacheComputed = true;
            if (ModelData?.Materials == null) return;
            foreach (ModelMesh mesh in m_meshes) {
                if (mesh?.MeshParts == null) continue;
                foreach (ModelMeshPart part in mesh.MeshParts) {
                    ModelMaterial mat = GetMaterial(part.MaterialIndex);
                    if (!m_hasTransmission && mat?.Transmission?.IsEnabled == true) m_hasTransmission = true;
                    if (!m_hasScatter && mat?.VolumeScatter?.IsEnabled == true) m_hasScatter = true;
                    if (m_hasTransmission && m_hasScatter) return;
                }
            }
        }

        /// <summary>
        /// 材质变更后调用，使缓存失效
        /// </summary>
        public void InvalidateMaterialCache() => m_materialCacheComputed = false;

        /// <summary>
        /// 蒙皮数据（如果有骨骼蒙皮）
        /// </summary>
        public ModelSkin Skin { get; set; }

        /// <summary>
        /// 动画数据列表
        /// </summary>
        public List<ModelAnimation> Animations { get; set; } = [];

        /// <summary>
        /// 灯光列表（KHR_lights_punctual）
        /// </summary>
        public List<ModelLight> Lights { get; set; } = [];

        /// <summary>
        /// 是否支持蒙皮
        /// </summary>
        public bool HasSkin => Skin != null;

        /// <summary>
        /// 是否有动画
        /// </summary>
        public bool HasAnimations => Animations.Count > 0;

        // 已加载的纹理缓存
        Dictionary<int, Texture2D> m_loadedTextures = new();

        // 默认白色纹理（用于无纹理模型）

        /// <summary>
        /// 获取默认白色纹理（用于无纹理模型）
        /// </summary>
        public static Texture2D DefaultWhiteTexture {
            get {
                if (field == null) {
                    field = CreateColorTexture(Color.White);
                }
                return field;
            }
        }

        public static Texture2D DefaultTransparentTexture {
            get {
                if (field == null) {
                    field = CreateColorTexture(Color.Transparent);
                }
                return field;
            }
        }

        /// <summary>
        /// 创建一个 1x1 的白色纹理
        /// </summary>
        public static Texture2D CreateColorTexture(Color color) {
            return Texture2D.Load(Image.LoadPixelData([new Rgba32(color.PackedValue)], 1, 1));
        }

        /// <summary>
        /// 获取指定索引的纹理（延迟加载）
        /// </summary>
        public Texture2D GetTexture(int textureIndex) {
            if (ModelData == null || textureIndex < 0 || textureIndex >= ModelData.Textures.Count) {
                return null;
            }

            // 检查缓存
            if (m_loadedTextures.TryGetValue(textureIndex, out Texture2D cached)) {
                return cached;
            }

            // 延迟加载纹理
            ModelTextureInfo texInfo = ModelData.Textures[textureIndex];
            if (texInfo.SourceImage.IsEmpty) {
                return null;
            }

            // 直接从 Stream 创建 Texture2D
            // sRGB 纹理（BaseColor、Emissive）使用 Srgb8Alpha8 格式，GPU 采样时自动 sRGB→线性解码
            Texture2D texture = null;
            try {
                using var stream = texInfo.SourceImage.Open();
                Engine.Media.Image img = Engine.Media.Image.Load(stream);
                int mipLevels = (int)Math.Floor(Math.Log2(Math.Max(img.Width, img.Height))) + 1;
                texture = LoadTexturesInSrgb && texInfo.IsSrgb
                    ? Texture2D.LoadSrgb(img.m_trueImage, mipLevels)
                    : Texture2D.Load(img, mipLevels);
                texture.Tag = texInfo.Name;
                texture.SamplerState = texInfo.SamplerState;
            }
            catch (System.Exception ex) {
                Log.Error($"[Model] Failed to load texture '{texInfo.Name}': {ex.Message}");
                return null;
            }

            m_loadedTextures[textureIndex] = texture;
            return texture;
        }

        /// <summary>
        /// 获取默认材质的 BaseColor 纹理
        /// 如果模型没有纹理，返回默认白色纹理（让材质颜色生效）
        /// </summary>
        public Texture2D GetDefaultBaseColorTexture() {
            if (ModelData?.Materials.Count > 0) {
                int texIndex = ModelData.Materials[0].BaseColorTexture?.TextureIndex ?? -1;
                if (texIndex >= 0) {
                    return GetTexture(texIndex);
                }
            }
            // 回退到旧方式（第一个纹理）
            if (ModelData?.Textures.Count > 0) {
                return GetTexture(0);
            }
            // 无纹理模型：返回默认白色纹理
            // 这样 MaterialColor（来自 BaseColorFactor）会直接显示
            return DefaultWhiteTexture;
        }

        /// <summary>
        /// 获取默认采样器状态（根据模型格式）
        /// glTF 默认使用 LinearWrap，Collada 默认使用 PointClamp
        /// </summary>
        public SamplerState GetDefaultSamplerState() {
            // 如果有纹理信息，使用第一个纹理的采样器
            if (ModelData?.Textures.Count > 0) {
                var texInfo = ModelData.Textures[0];
                if (texInfo.SamplerState != null) {
                    return texInfo.SamplerState;
                }
            }
            // glTF 无纹理模型使用 LinearWrap
            if (ModelData?.Materials.Count > 0) {
                return SamplerState.LinearWrap;
            }
            // 回退到默认的 PointClamp（Collada 默认）
            return SamplerState.PointClamp;
        }

        /// <summary>
        /// 获取默认材质的 BaseColor 颜色因子
        /// 用于无纹理模型或当 ComponentModel.DiffuseColor 未设置时
        /// </summary>
        public Vector4? GetDefaultBaseColorFactor() {
            if (ModelData?.Materials.Count > 0) {
                return ModelData.Materials[0].BaseColorFactor;
            }
            return null;
        }

        /// <summary>
        /// 获取指定索引的材质数据
        /// </summary>
        public ModelMaterial GetMaterial(int materialIndex) {
            if (ModelData == null || materialIndex < 0 || materialIndex >= ModelData.Materials.Count) {
                return null;
            }
            return ModelData.Materials[materialIndex];
        }

        public ModelBone FindBone(string name, bool throwIfNotFound = true) {
            // 1. 精确匹配（原逻辑，dae 模型 BoneAliases==null 时零行为变化）
            foreach (ModelBone bone in m_bones) {
                if (bone.Name == name) {
                    return bone;
                }
            }
            // 2. 别名回退：别名表存在且含此名时，用真实骨骼名再查一次
            if (BoneAliases != null && BoneAliases.TryGetValue(name, out string realName)) {
                foreach (ModelBone bone in m_bones) {
                    if (bone.Name == realName) {
                        return bone;
                    }
                }
            }
            // 3. 原 throw/null 逻辑
            return throwIfNotFound ? throw new InvalidOperationException("ModelBone not found.") : null;
        }

        public ModelMesh FindMesh(string name, bool throwIfNotFound = true) {
            foreach (ModelMesh mesh in m_meshes) {
                if (mesh.Name == name) {
                    return mesh;
                }
            }
            return throwIfNotFound ? throw new InvalidOperationException("ModelMesh not found.") : null;
        }

        public ModelBone NewBone(string name, Matrix transform, ModelBone parentBone) {
            ArgumentNullException.ThrowIfNull(name);
            if (parentBone == null
                && m_bones.Count > 0) {
                throw new InvalidOperationException("There can be only one root bone.");
            }
            if (parentBone != null
                && parentBone.Model != this) {
                throw new InvalidOperationException("Parent bone must belong to the same model.");
            }
            ModelBone modelBone = new() { Model = this, Index = m_bones.Count };
            m_bones.Add(modelBone);
            modelBone.Name = name;
            modelBone.Transform = transform;
            if (parentBone != null) {
                modelBone.ParentBone = parentBone;
                parentBone.m_childBones.Add(modelBone);
            }
            else {
                m_rootBone = modelBone;
            }
            return modelBone;
        }

        public void AddMesh(ModelMesh mesh) {
            m_meshes.Add(mesh);
        }

        public ModelMesh NewMesh(string name, ModelBone parentBone, BoundingBox boundingBox) {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(parentBone);
            return parentBone.Model != this
                ? throw new InvalidOperationException("Parent bone must belong to the same model.")
                : new ModelMesh { Name = name, ParentBone = parentBone, BoundingBox = boundingBox };
        }

        public void CopyAbsoluteBoneTransformsTo(Matrix[] absoluteTransforms) {
            ArgumentNullException.ThrowIfNull(absoluteTransforms);
            if (absoluteTransforms.Length < m_bones.Count) {
                throw new ArgumentOutOfRangeException(nameof(absoluteTransforms));
            }
            for (int i = 0; i < m_bones.Count; i++) {
                ModelBone modelBone = m_bones[i];
                if (modelBone.ParentBone == null) {
                    absoluteTransforms[i] = modelBone.Transform;
                }
                else {
                    Matrix.MultiplyRestricted(
                        ref modelBone.m_transform,
                        ref absoluteTransforms[modelBone.ParentBone.Index],
                        out absoluteTransforms[i]
                    );
                }
            }
        }

        public BoundingBox CalculateAbsoluteBoundingBox(Matrix[] absoluteTransforms) {
            ArgumentNullException.ThrowIfNull(absoluteTransforms);
            if (absoluteTransforms.Length < m_bones.Count) {
                throw new ArgumentOutOfRangeException(nameof(absoluteTransforms));
            }
            BoundingBox result = default;
            bool flag = false;
            foreach (ModelMesh mesh in Meshes) {
                if (flag) {
                    BoundingBox.Transform(ref mesh.m_boundingBox, ref absoluteTransforms[mesh.ParentBone.Index], out BoundingBox result2);
                    result = BoundingBox.Union(result, result2);
                }
                else {
                    BoundingBox.Transform(ref mesh.m_boundingBox, ref absoluteTransforms[mesh.ParentBone.Index], out result);
                    flag = true;
                }
            }
            return result;
        }

        public void Dispose() {
            InternalDispose();
        }

        public static Model Load(ModelData modelData, bool keepSourceVertexDataInTags = false) {
            Model model = new();
            model.Initialize(modelData, keepSourceVertexDataInTags);
            return model;
        }

        public static Model Load(Stream stream, bool keepSourceVertexDataInTags = false) => Load(ModelData.Load(stream), keepSourceVertexDataInTags);

        public static Model Load(string fileName, bool keepSourceVertexDataInTags = false) =>
            Load(ModelData.Load(fileName), keepSourceVertexDataInTags);

        public void Initialize(ModelData modelData, bool keepSourceVertexDataInTags) {
            ModelData = modelData;
            Skin = modelData.Skin;
            Animations = modelData.Animations;
            ArgumentNullException.ThrowIfNull(modelData);
            InternalDispose();
            // 纹理延迟加载，不在初始化时创建
            VertexBuffer[] array = new VertexBuffer[modelData.Buffers.Count];
            IndexBuffer[] array2 = new IndexBuffer[modelData.Buffers.Count];
            for (int i = 0; i < modelData.Buffers.Count; i++) {
                ModelBuffersData modelBuffersData = modelData.Buffers[i];
                array[i] = new VertexBuffer(
                    modelBuffersData.VertexDeclaration,
                    modelBuffersData.Vertices.Length / modelBuffersData.VertexDeclaration.VertexStride
                );
                array[i].SetData(modelBuffersData.Vertices, 0, modelBuffersData.Vertices.Length);
                array2[i] = new IndexBuffer(IndexFormat.ThirtyTwoBits, modelBuffersData.Indices.Length / 4);
                array2[i].SetData(modelBuffersData.Indices, 0, modelBuffersData.Indices.Length);
                if (keepSourceVertexDataInTags) {
                    array[i].Tag = modelBuffersData.Vertices;
                    array2[i].Tag = modelBuffersData.Indices;
                }
            }
            foreach (ModelBoneData bone in modelData.Bones) {
                NewBone(bone.Name, bone.Transform, bone.ParentBoneIndex >= 0 ? m_bones[bone.ParentBoneIndex] : null);
            }
            // 解析蒙皮骨骼引用
            Skin?.ResolveJoints(m_bones);
            foreach (ModelMeshData mesh in modelData.Meshes) {
                ModelMesh modelMesh = NewMesh(mesh.Name, m_bones[mesh.ParentBoneIndex], mesh.BoundingBox);
                modelMesh.IsVisible = mesh.IsVisible;
                m_meshes.Add(modelMesh);
                foreach (ModelMeshPartData meshPart in mesh.MeshParts) {
                    ModelMeshPart part = modelMesh.NewMeshPart(
                        array[meshPart.BuffersDataIndex],
                        array2[meshPart.BuffersDataIndex],
                        meshPart.StartIndex,
                        meshPart.IndicesCount,
                        meshPart.BoundingBox,
                        meshPart.MaterialIndex,
                        meshPart.PrimitiveType,
                        meshPart.InstanceMatrices,
                        meshPart.InstanceCount
                    );
                    if (meshPart.MorphTargetTexture != null) {
                        part.MorphTargetTexture = meshPart.MorphTargetTexture;
                        part.MorphTargetCount = meshPart.MorphTargetCount;
                        part.MorphWeights = meshPart.MorphWeights;
                    }
                }
            }
            // 转换灯光数据
            foreach (ModelLightData ld in modelData.Lights) {
                Lights.Add(new ModelLight {
                    Type = ld.Type,
                    Position = ld.Position,
                    Direction = ld.Direction,
                    Color = ld.Color,
                    Intensity = ld.Intensity,
                    Range = ld.Range,
                    InnerConeCos = ld.InnerConeCos,
                    OuterConeCos = ld.OuterConeCos,
                    BoneIndex = ld.BoneIndex,
                    IsVisible = ld.IsVisible
                });
            }
        }

        public void InternalDispose() {
            m_rootBone = null;
            m_bones.Clear();
            Utilities.DisposeCollection(m_meshes);
            // 清理缓存的纹理
            foreach (var texture in m_loadedTextures.Values) {
                texture?.Dispose();
            }
            m_loadedTextures.Clear();
        }
    }
}