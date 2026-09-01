using Engine.Animation;
using Engine.Graphics;

namespace Engine.Media {
    public class ModelData {
        public List<ModelBoneData> Bones = [];

        public List<ModelMeshData> Meshes = [];

        public List<ModelBuffersData> Buffers = [];

        /// <summary>
        /// 纹理信息列表（glTF 加载时填充，支持延迟加载）
        /// </summary>
        public List<ModelTextureInfo> Textures = [];

        /// <summary>
        /// 材质数据列表（glTF 加载时填充）
        /// </summary>
        public List<ModelMaterial> Materials = [];

        /// <summary>
        /// 蒙皮数据（glTF 加载时填充）
        /// </summary>
        public ModelSkin Skin { get; set; }

        /// <summary>
        /// 动画数据列表（glTF 加载时填充）
        /// </summary>
        public List<ModelAnimation> Animations { get; set; } = [];

        /// <summary>
        /// 灯光数据列表（KHR_lights_punctual，glTF 加载时填充）
        /// </summary>
        public List<ModelLightData> Lights = [];

        /// <summary>
        /// glTF 纹理 LogicalIndex 到 Textures 列表索引的映射
        /// 仅在加载过程中有效
        /// </summary>
        internal Dictionary<int, int> GltfTextureToModelIndex = new();

        /// <summary>
        /// glTF node LogicalIndex 到 Meshes 列表索引的映射（KHR_node_visibility 用）
        /// 仅在加载过程中有效
        /// </summary>
        internal Dictionary<int, int> GltfNodeToMeshIndex = new();

        /// <summary>
        /// glTF node LogicalIndex 到 Lights 列表索引的映射（KHR_node_visibility 用）
        /// 仅在加载过程中有效
        /// </summary>
        internal Dictionary<int, int> GltfNodeToLightIndex = new();

        /// <summary>
        /// 获取纹理在 Textures 列表中的索引
        /// </summary>
        /// <param name="gltfLogicalIndex">glTF 纹理的 LogicalIndex</param>
        /// <returns>Textures 列表中的索引，不存在返回 -1</returns>
        public int GetTextureIndex(int gltfLogicalIndex) {
            return GltfTextureToModelIndex.TryGetValue(gltfLogicalIndex, out int index) ? index : -1;
        }

        public static ModelFileFormat DetermineFileFormat(Stream stream) {
            long position = stream.Position;
            stream.Position = 0;
            byte[] header = new byte[4];
            if (stream.Read(header, 0, 4) >= 4) {
                // GLB 文件以 "glTF" 魔数开头 (0x67 0x6C 0x54 0x46)
                if (header[0] == 0x67 && header[1] == 0x6C && header[2] == 0x54 && header[3] == 0x46) {
                    stream.Position = position;
                    return ModelFileFormat.Glb;
                }
                // glTF 文本格式以 '{' 开头 (JSON 格式)
                if (header[0] == 0x7B) {
                    stream.Position = position;
                    return ModelFileFormat.Gltf;
                }
            }
            stream.Position = position;
            if (Collada.IsColladaStream(stream)) {
                return ModelFileFormat.Collada;
            }
            throw new InvalidOperationException("Unsupported model file format.");
        }

        public static ModelFileFormat DetermineFileFormat(string extension) {
            if (extension.Equals(".dae", StringComparison.OrdinalIgnoreCase)) {
                return ModelFileFormat.Collada;
            }
            if (extension.Equals(".gltf", StringComparison.OrdinalIgnoreCase)) {
                return ModelFileFormat.Gltf;
            }
            if (extension.Equals(".glb", StringComparison.OrdinalIgnoreCase)) {
                return ModelFileFormat.Glb;
            }
            throw new InvalidOperationException("Unsupported model file format.");
        }

        public static ModelData Load(Stream stream, ModelFileFormat format) {
            return format switch {
                ModelFileFormat.Collada => Collada.Load(stream),
                ModelFileFormat.Gltf or ModelFileFormat.Glb => GltfLoader.Load(stream),
                _ => throw new InvalidOperationException("Unsupported model file format.")
            };
        }

        public static ModelData Load(string fileName, ModelFileFormat format) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                return Load(stream, format);
            }
        }

        public static ModelData Load(Stream stream) {
            PeekStream peekStream = new(stream, 256);
            ModelFileFormat format = DetermineFileFormat(peekStream.GetInitialBytesStream());
            return Load(peekStream, format);
        }

        public static ModelData Load(string fileName) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                return Load(stream);
            }
        }

        public static void Save(ModelData modelData, Stream stream, ModelFileFormat format) {
            if (format == ModelFileFormat.Collada) {
                Collada.Save(modelData, stream);
                return;
            }
            throw new InvalidOperationException("Unsupported model file format.");
        }

        public static void Save(ModelData modelData, string fileName, ModelFileFormat format) {
            using Stream stream = Storage.OpenFile(fileName, OpenFileMode.Create);
            Save(modelData, stream, format);
        }
    }
}