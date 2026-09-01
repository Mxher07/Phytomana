using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game {
    public static class InstancedModelsManager {
        public struct SourceModelVertex {
            public float X;
            public float Y;
            public float Z;
            public float Nx;
            public float Ny;
            public float Nz;
            public float Tx;
            public float Ty;
        }

        public struct SourceModelVertexWithTangent {
            public float X, Y, Z;
            public float Nx, Ny, Nz;
            public float Tx, Ty;
            public float Bx, By, Bz, Bw;
        }

        /// <summary>
        /// 从任意 stride 的顶点缓冲中提取 Position+Normal+UV 到 InstancedVertex
        /// </summary>
        static void ExtractVertices(byte[] rawData, int stride, int positionOffset, int normalOffset, int uvOffset,
            DynamicArray<InstancedVertex> vertices, DynamicArray<int> indices, int[] indexData,
            int startIndex, int indicesCount, int instanceIndex) {
            int vertexCount = rawData.Length / stride;
            Dictionary<int, int> vertexRemap = new();

            for (int j = startIndex; j < startIndex + indicesCount; j++) {
                if (j >= indexData.Length) continue;
                int originalIndex = indexData[j];
                if (originalIndex < 0 || originalIndex >= vertexCount) continue;

                if (!vertexRemap.TryGetValue(originalIndex, out int newIndex)) {
                    int baseOff = originalIndex * stride;
                    // Bounds check: ensure all reads stay within rawData
                    int maxReadEnd = baseOff + Math.Max(
                        positionOffset + 12,
                        Math.Max(normalOffset + 12, uvOffset + 8));
                    if (maxReadEnd > rawData.Length) {
                        continue;
                    }
                    newIndex = vertices.Count;
                    vertexRemap[originalIndex] = newIndex;
                    InstancedVertex v = default;
                    v.X = BitConverter.ToSingle(rawData, baseOff + positionOffset);
                    v.Y = BitConverter.ToSingle(rawData, baseOff + positionOffset + 4);
                    v.Z = BitConverter.ToSingle(rawData, baseOff + positionOffset + 8);
                    v.Nx = BitConverter.ToSingle(rawData, baseOff + normalOffset);
                    v.Ny = BitConverter.ToSingle(rawData, baseOff + normalOffset + 4);
                    v.Nz = BitConverter.ToSingle(rawData, baseOff + normalOffset + 8);
                    v.Tx = BitConverter.ToSingle(rawData, baseOff + uvOffset);
                    v.Ty = BitConverter.ToSingle(rawData, baseOff + uvOffset + 4);
                    v.Instance = instanceIndex;
                    vertices.Add(v);
                }
                indices.Add(newIndex);
            }
        }

        public struct InstancedVertex {
            public float X;
            public float Y;
            public float Z;
            public float Nx;
            public float Ny;
            public float Nz;
            public float Tx;
            public float Ty;
            public float Instance;
        }

        /// <summary>
        /// 缓存键：包含 Model 引用和 meshDrawOrders 哈希
        /// </summary>
        struct CacheKey : System.IEquatable<CacheKey> {
            public readonly Model Model;
            public readonly int MeshDrawOrdersHash;

            public CacheKey(Model model, int[] meshDrawOrders) {
                Model = model;
                MeshDrawOrdersHash = ComputeMeshDrawOrdersHash(meshDrawOrders);
            }

            static int ComputeMeshDrawOrdersHash(int[] meshDrawOrders) {
                if (meshDrawOrders == null || meshDrawOrders.Length == 0) {
                    return 0;
                }
                int hash = 17;
                foreach (int order in meshDrawOrders) {
                    hash = hash * 31 + order;
                }
                return hash;
            }

            public bool Equals(CacheKey other) {
                return Model == other.Model && MeshDrawOrdersHash == other.MeshDrawOrdersHash;
            }

            public override int GetHashCode() {
                return System.HashCode.Combine(Model, MeshDrawOrdersHash);
            }

            public override bool Equals(object obj) {
                return obj is CacheKey other && Equals(other);
            }
        }

        /// <summary>
        /// 缓存：CacheKey -> (MaterialIndex -> InstancedModelData)
        /// MaterialIndex = -1 表示无材质
        /// </summary>
        static Dictionary<CacheKey, Dictionary<int, InstancedModelData>> m_cache = new();

        /// <summary>
        /// 每个缓存条目构建时的可见性签名。
        /// 任一 mesh 的 IsVisible 翻转（如 KHR_node_visibility 动画）→ 签名变化 → 该条目重建。
        /// </summary>
        static Dictionary<CacheKey, int> m_visibilitySignatures = new();

        static InstancedModelsManager() {
            Display.DeviceReset += delegate {
                foreach (Dictionary<int, InstancedModelData> dict in m_cache.Values) {
                    foreach (InstancedModelData value in dict.Values) {
                        value.VertexBuffer?.Dispose();
                        value.IndexBuffer?.Dispose();
                    }
                }
                m_cache.Clear();
                m_visibilitySignatures.Clear();
            };
        }

        /// <summary>
        /// 计算 meshDrawOrders 中各 mesh 的 IsVisible 组合签名。IsVisible 任一翻转 → 签名变化。
        /// </summary>
        static int ComputeVisibilitySignature(Model model, int[] meshDrawOrders) {
            int hash = 17;
            for (int i = 0; i < meshDrawOrders.Length; i++) {
                int meshIndex = meshDrawOrders[i];
                bool visible = meshIndex >= 0
                    && meshIndex < model.Meshes.Count
                    && model.Meshes[meshIndex].IsVisible;
                hash = hash * 31 + (visible ? 1 : 0);
            }
            return hash;
        }

        static void DisposeDataByMaterial(Dictionary<int, InstancedModelData> dataByMaterial) {
            foreach (InstancedModelData value in dataByMaterial.Values) {
                value.VertexBuffer?.Dispose();
                value.IndexBuffer?.Dispose();
            }
        }

        /// <summary>
        /// 获取模型的所有实例化数据（按材质分组）
        /// </summary>
        public static Dictionary<int, InstancedModelData> GetInstancedModelDataByMaterial(Model model, int[] meshDrawOrders) {
            CacheKey key = new CacheKey(model, meshDrawOrders);
            int signature = ComputeVisibilitySignature(model, meshDrawOrders);
            if (m_cache.TryGetValue(key, out Dictionary<int, InstancedModelData> dataByMaterial)) {
                // 可见性变化（KHR_node_visibility 动画等）→ 丢弃旧缓冲，按当前可见性重建
                if (!m_visibilitySignatures.TryGetValue(key, out int builtSignature)
                    || builtSignature != signature) {
                    DisposeDataByMaterial(dataByMaterial);
                    dataByMaterial = CreateInstancedModelDataByMaterial(model, meshDrawOrders);
                    m_cache[key] = dataByMaterial;
                    m_visibilitySignatures[key] = signature;
                }
            }
            else {
                dataByMaterial = CreateInstancedModelDataByMaterial(model, meshDrawOrders);
                m_cache[key] = dataByMaterial;
                m_visibilitySignatures[key] = signature;
            }
            return dataByMaterial;
        }

        /// <summary>
        /// 兼容旧接口：获取合并的实例化数据（仅用于单材质模型）
        /// 注意：对于多材质模型，返回的材质是不确定的
        /// </summary>
        public static InstancedModelData GetInstancedModelData(Model model, int[] meshDrawOrders) {
            Dictionary<int, InstancedModelData> dataByMaterial = GetInstancedModelDataByMaterial(model, meshDrawOrders);
            // 返回第一个材质的数据（兼容旧代码）
            foreach (var kvp in dataByMaterial) {
                return kvp.Value;
            }
            return null;
        }

        /// <summary>
        /// 按材质分组创建实例化数据
        /// </summary>
        public static Dictionary<int, InstancedModelData> CreateInstancedModelDataByMaterial(Model model, int[] meshDrawOrders) {
            Dictionary<int, InstancedModelData> result = new();

            // 按 MaterialIndex 分组收集顶点和索引数据
            Dictionary<int, List<(int meshIndex, ModelMeshPart part)>> partsByMaterial = new();

            for (int i = 0; i < meshDrawOrders.Length; i++) {
                ModelMesh modelMesh = model.Meshes[meshDrawOrders[i]];
                if (!modelMesh.IsVisible) {
                    continue;
                }
                foreach (ModelMeshPart meshPart in modelMesh.MeshParts) {
                    int materialIndex = meshPart.MaterialIndex;
                    if (!partsByMaterial.TryGetValue(materialIndex, out List<(int, ModelMeshPart)> list)) {
                        list = new List<(int, ModelMeshPart)>();
                        partsByMaterial[materialIndex] = list;
                    }
                    list.Add((meshDrawOrders[i], meshPart));
                }
            }

            // 为每个材质创建实例化数据
            foreach (var kvp in partsByMaterial) {
                int materialIndex = kvp.Key;
                var parts = kvp.Value;

                InstancedModelData data = CreateInstancedModelDataForParts(model, parts);
                if (data != null) {
                    result[materialIndex] = data;
                }
            }

            return result;
        }

        /// <summary>
        /// 为指定的 mesh parts 创建实例化数据
        /// </summary>
        static InstancedModelData CreateInstancedModelDataForParts(Model model, List<(int meshIndex, ModelMeshPart part)> parts) {
            DynamicArray<InstancedVertex> vertices = new();
            DynamicArray<int> indices = new();

            foreach (var (meshIndex, meshPart) in parts) {
                ModelMesh modelMesh = model.Meshes[meshIndex];
                VertexBuffer vertexBuffer = meshPart.VertexBuffer;
                IndexBuffer indexBuffer = meshPart.IndexBuffer;

                if (vertexBuffer == null || indexBuffer == null) {
                    continue;
                }

                ReadOnlyList<VertexElement> vertexElements = vertexBuffer.VertexDeclaration.VertexElements;

                // 查找 Position、Normal、TexCoord 的偏移量
                int positionOffset = -1, normalOffset = -1, uvOffset = -1;
                string texCoordSemantic = VertexElementSemantic.TextureCoordinate.GetSemanticString();
                foreach (VertexElement elem in vertexElements) {
                    if (elem.Semantic == VertexElementSemantic.Position.GetSemanticString()) positionOffset = elem.Offset;
                    else if (elem.Semantic == VertexElementSemantic.Normal.GetSemanticString()) normalOffset = elem.Offset;
                    else if (uvOffset < 0 && elem.SemanticName == texCoordSemantic) uvOffset = elem.Offset;
                }

                if (positionOffset < 0 || normalOffset < 0 || uvOffset < 0) {
                    continue;
                }

                int[] indexData = BlockMesh.GetIndexData<int>(indexBuffer);
                if (indexData == null) continue;

                // 从 Tag 读取原始字节数据，按实际 stride 提取
                if (vertexBuffer.Tag is not byte[] rawData) {
                    continue;
                }

                int stride = vertexBuffer.VertexDeclaration.VertexStride;
                ExtractVertices(rawData, stride, positionOffset, normalOffset, uvOffset,
                    vertices, indices, indexData,
                    meshPart.StartIndex, meshPart.IndicesCount, modelMesh.ParentBone.Index);
            }

            // 如果没有有效的顶点数据，返回 null
            if (vertices.Count == 0 || indices.Count == 0) {
                return null;
            }

            InstancedModelData data = new() {
                VertexBuffer = new VertexBuffer(InstancedModelData.VertexDeclaration, vertices.Count),
                IndexBuffer = new IndexBuffer(IndexFormat.ThirtyTwoBits, indices.Count)
            };
            data.VertexBuffer.SetData(vertices.Array, 0, vertices.Count);
            data.IndexBuffer.SetData(indices.Array, 0, indices.Count);

            return data;
        }

        /// <summary>
        /// 旧方法：创建合并的实例化数据（保持向后兼容）
        /// 仅支持固定 3 元素 (Position+Normal+UV) 顶点格式，不支持 glTF 等带额外属性的模型。
        /// 推荐使用 <see cref="GetInstancedModelDataByMaterial"/> 或 <see cref="GetInstancedModelData"/>。
        /// </summary>
        [System.Obsolete("Use GetInstancedModelData or GetInstancedModelDataByMaterial instead.")]
        public static InstancedModelData CreateInstancedModelData(Model model, int[] meshDrawOrders) {
            DynamicArray<InstancedVertex> dynamicArray = new();
            DynamicArray<int> dynamicArray2 = new();
            for (int i = 0; i < meshDrawOrders.Length; i++) {
                ModelMesh modelMesh = model.Meshes[meshDrawOrders[i]];
                if (!modelMesh.IsVisible) {
                    continue;
                }
                foreach (ModelMeshPart meshPart in modelMesh.MeshParts) {
                    _ = dynamicArray.Count;
                    VertexBuffer vertexBuffer = meshPart.VertexBuffer;
                    IndexBuffer indexBuffer = meshPart.IndexBuffer;
                    ReadOnlyList<VertexElement> vertexElements = vertexBuffer.VertexDeclaration.VertexElements;
                    int[] indexData = BlockMesh.GetIndexData<int>(indexBuffer);
                    Dictionary<int, int> dictionary = new();
                    if (vertexElements.Count != 3
                        || vertexElements[0].Offset != 0
                        || !(vertexElements[0].Semantic == VertexElementSemantic.Position.GetSemanticString())
                        || vertexElements[1].Offset != 12
                        || !(vertexElements[1].Semantic == VertexElementSemantic.Normal.GetSemanticString())
                        || vertexElements[2].Offset != 24
                        || !(vertexElements[2].Semantic == VertexElementSemantic.TextureCoordinate.GetSemanticString())) {
                        throw new InvalidOperationException("Unsupported vertex format.");
                    }
                    SourceModelVertex[] vertexData = BlockMesh.GetVertexData<SourceModelVertex>(vertexBuffer);
                    for (int j = meshPart.StartIndex; j < meshPart.StartIndex + meshPart.IndicesCount; j++) {
                        int num = indexData[j];
                        if (!dictionary.ContainsKey(num)) {
                            dictionary.Add(num, dynamicArray.Count);
                            InstancedVertex item = default;
                            SourceModelVertex sourceModelVertex = vertexData[num];
                            item.X = sourceModelVertex.X;
                            item.Y = sourceModelVertex.Y;
                            item.Z = sourceModelVertex.Z;
                            item.Nx = sourceModelVertex.Nx;
                            item.Ny = sourceModelVertex.Ny;
                            item.Nz = sourceModelVertex.Nz;
                            item.Tx = sourceModelVertex.Tx;
                            item.Ty = sourceModelVertex.Ty;
                            item.Instance = modelMesh.ParentBone.Index;
                            dynamicArray.Add(item);
                        }
                    }
                    for (int k = 0; k < meshPart.IndicesCount / 3; k++) {
                        dynamicArray2.Add(dictionary[indexData[meshPart.StartIndex + 3 * k]]);
                        dynamicArray2.Add(dictionary[indexData[meshPart.StartIndex + 3 * k + 1]]);
                        dynamicArray2.Add(dictionary[indexData[meshPart.StartIndex + 3 * k + 2]]);
                    }
                }
            }
            InstancedModelData instancedModelData = new() {
                VertexBuffer = new VertexBuffer(InstancedModelData.VertexDeclaration, dynamicArray.Count),
                IndexBuffer = new IndexBuffer(IndexFormat.ThirtyTwoBits, dynamicArray2.Count)
            };
            instancedModelData.VertexBuffer.SetData(dynamicArray.Array, 0, dynamicArray.Count);
            instancedModelData.IndexBuffer.SetData(dynamicArray2.Array, 0, dynamicArray2.Count);
            return instancedModelData;
        }
    }
}
