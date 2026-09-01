using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using BYTES = System.ArraySegment<byte>;

namespace Engine.Media {
    /// <summary>
    /// glTF 模型加载器
    /// 通过回调机制加载外部资源（.bin 文件和纹理）
    /// </summary>
    public static class GltfLoader {
        /// <summary>
        /// 外置文件加载回调，一般情况下和 Storage.LoadContentStreamCallback 相同
        /// </summary>
        public static Func<string, Stream> LoadExternalStreamCallback { get; set; }

        /// <summary>
        /// 检查是否为 glTF 文件
        /// </summary>
        public static bool IsGltfFile(string filePath) {
            string ext = Storage.GetExtension(filePath).ToLowerInvariant();
            return ext is ".gltf" or ".glb";
        }

        /// <summary>
        /// 从文件路径加载 glTF 模型（便利方法，仅用于测试，将持久修改 LoadExternalStreamCallback）
        /// </summary>
        /// <param name="filePath">模型文件路径（.gltf 或 .glb）</param>
        /// <returns>ModelData 实例</returns>
        public static ModelData LoadFromFile(string filePath) {
            ArgumentNullException.ThrowIfNull(filePath);
            string basePath = Path.GetDirectoryName(filePath);
            LoadExternalStreamCallback = relativePath => {
                string fullPath = Path.Combine(basePath, relativePath);
                return File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
            };
            using FileStream stream = File.OpenRead(filePath);
            return Load(stream, basePath);
        }

        /// <summary>
        /// 从流加载 glTF 模型
        /// </summary>
        /// <param name="stream">模型数据流（GLB 或 glTF JSON）</param>
        /// <param name="basePath">模型数据流的基路径（用于加载外部资源）</param>
        /// <returns>ModelData 实例</returns>
        public static ModelData Load(Stream stream, string basePath = null) {
            ArgumentNullException.ThrowIfNull(stream);

            BYTES FileReaderCallback(string assetName) {
                string path = basePath == null ? assetName : Storage.CombinePaths(basePath.Replace('\\', '/'), assetName);
                Stream resourceStream = LoadExternalStreamCallback(path);
                if (resourceStream == null) {
                    return null;
                }
                byte[] bytes = new byte[resourceStream.Length];
                int totalRead = 0;
                while (totalRead < bytes.Length) {
                    int read = resourceStream.Read(bytes, totalRead, bytes.Length - totalRead);
                    if (read == 0) {
                        throw new Exception($"Failed to read {path}");
                    }
                    totalRead += read;
                }
                return new BYTES(bytes);
            }

            ReadContext context = ReadContext.Create(FileReaderCallback);
            context.Validation = ValidationMode.Skip;
            ModelRoot modelRoot = context.ReadSchema2(stream);
            return ConvertToModelData(modelRoot);
        }

        static ModelData ConvertToModelData(ModelRoot modelRoot) {
            ModelData modelData = new();

            // 构建节点名称到索引的映射
            Dictionary<Node, int> nodeToIndex = new();
            List<Node> allNodes = new();

            // 首先收集所有节点
            foreach (Node node in modelRoot.LogicalNodes) {
                nodeToIndex[node] = allNodes.Count;
                allNodes.Add(node);
            }

            // 加载纹理和材质（在网格之前，因为需要建立索引映射）
            Dictionary<Texture, int> textureToIndex = new();
            Dictionary<Material, int> materialToIndex = new();
            GltfMaterialConverter.ConvertTexturesAndMaterials(modelRoot, modelData, textureToIndex, materialToIndex);

            // 转换骨骼/节点数据
            GltfBoneConverter.ConvertBones(modelRoot, modelData, allNodes, nodeToIndex);

            // 转换网格数据
            GltfMeshConverter.ConvertMeshes(modelRoot, modelData, allNodes, nodeToIndex, textureToIndex, materialToIndex);

            // 转换蒙皮数据
            GltfBoneConverter.ConvertSkins(modelRoot, modelData, nodeToIndex);

            // 转换动画数据
            GltfAnimationConverter.ConvertAnimations(modelRoot, modelData);
            return modelData;
        }
    }
}