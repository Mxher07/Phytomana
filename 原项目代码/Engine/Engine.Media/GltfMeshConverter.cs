using System.Numerics;
using Engine.Graphics;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;
using GltfPrimitiveType = SharpGLTF.Schema2.PrimitiveType;
using PrimitiveType = Engine.Graphics.PrimitiveType;
using GltfTexture = SharpGLTF.Schema2.Texture;
using GltfMaterial = SharpGLTF.Schema2.Material;

namespace Engine.Media {
    public static class GltfMeshConverter {
        public static void ConvertMeshes(ModelRoot modelRoot,
            ModelData modelData,
            List<Node> allNodes,
            Dictionary<Node, int> nodeToIndex,
            Dictionary<GltfTexture, int> textureToIndex,
            Dictionary<GltfMaterial, int> materialToIndex) {
            int bufferIndex = 0;

            // 获取默认场景或使用所有根节点
            Scene scene = modelRoot.DefaultScene ?? modelRoot.LogicalScenes.FirstOrDefault();
            IEnumerable<Node> nodesToProcess;
            if (scene != null) {
                nodesToProcess = scene.VisualChildren;
            }
            else {
                nodesToProcess = modelRoot.LogicalNodes.Where(n => n.VisualParent == null);
            }
            foreach (Node node in nodesToProcess) {
                ProcessNodeForMesh(node, modelData, allNodes, nodeToIndex, ref bufferIndex, materialToIndex);
            }
        }

        public static void ProcessNodeForMesh(Node node,
            ModelData modelData,
            List<Node> allNodes,
            Dictionary<Node, int> nodeToIndex,
            ref int bufferIndex,
            Dictionary<GltfMaterial, int> materialToIndex,
            bool parentVisible = true) {
            // 解析当前节点的 visibility 状态
            bool nodeVisible = parentVisible;
            if (node.TryGetVisibility(out bool vis)) {
                nodeVisible = vis && parentVisible;
            }
            if (node.Mesh != null) {
                int boneIndex = nodeToIndex.TryGetValue(node, out int idx) ? idx : 0;

                // 每个 primitive 创建独立 ModelMeshData，避免同一 mesh 内不同材质的 parts 被错误地一起绘制
                // EXT_mesh_gpu_instancing：读取实例变换矩阵
                MeshGpuInstancing gpuInstancing = node.GetGpuInstancing();
                Matrix4x4[] instanceMatrices = null;
                int instanceCount = 0;
                if (gpuInstancing != null
                    && gpuInstancing.Count > 0) {
                    instanceCount = gpuInstancing.Count;
                    instanceMatrices = new Matrix4x4[instanceCount];
                    for (int i = 0; i < instanceCount; i++) {
                        instanceMatrices[i] = gpuInstancing.GetLocalMatrix(i);
                    }
                }
                foreach (MeshPrimitive primitive in node.Mesh.Primitives) {
                    ModelMeshPartData meshPart = ProcessPrimitive(primitive, modelData, ref bufferIndex, materialToIndex);
                    if (meshPart == null) {
                        continue;
                    }

                    // 设置实例化数据（同节点所有 primitive 共享同一数组引用，请勿修改数组内容）
                    meshPart.InstanceCount = instanceCount;
                    meshPart.InstanceMatrices = instanceMatrices;
                    ModelMeshData meshData = new() {
                        Name = node.Mesh.Name ?? $"Mesh{node.Mesh.LogicalIndex}", ParentBoneIndex = boneIndex, IsVisible = nodeVisible
                    };
                    meshData.MeshParts.Add(meshPart);
                    CalculateMeshBoundingBox(meshData, meshData.MeshParts);
                    modelData.Meshes.Add(meshData);
                }

                // 记录 node → mesh 索引映射（KHR_node_visibility 动画用）
                // 存储该 node 第一个 mesh 的索引，CreateNodeVisibilityTarget 通过 ParentBoneIndex 查找所有同级 mesh
                for (int mi = modelData.Meshes.Count - 1; mi >= 0; mi--) {
                    if (modelData.Meshes[mi].ParentBoneIndex == boneIndex) {
                        modelData.GltfNodeToMeshIndex[node.LogicalIndex] = mi;
                        break;
                    }
                }
            }

            // KHR_lights_punctual
            if (node.PunctualLight != null
                && modelData.Lights.Count < ModelLight.MaxPunctualLights) {
                PunctualLight pl = node.PunctualLight;
                Matrix4x4 wm = node.WorldMatrix;
                ModelLightData ld = new() {
                    Color = new Vector3(pl.Color.X, pl.Color.Y, pl.Color.Z),
                    Intensity = pl.Intensity,
                    Range = pl.Range,
                    Position = new Vector3(wm.M41, wm.M42, wm.M43),
                    Direction = Vector3.Normalize(new Vector3(-wm.M31, -wm.M32, -wm.M33)),
                    IsVisible = nodeVisible,
                    BoneIndex = nodeToIndex.TryGetValue(node, out int bIdx) ? bIdx : -1
                };
                switch (pl.LightType) {
                    case PunctualLightType.Directional: ld.Type = ModelLightType.Directional; break;
                    case PunctualLightType.Point: ld.Type = ModelLightType.Point; break;
                    case PunctualLightType.Spot:
                        ld.Type = ModelLightType.Spot;
                        ld.InnerConeCos = MathF.Cos(pl.InnerConeAngle);
                        ld.OuterConeCos = MathF.Cos(pl.OuterConeAngle);
                        break;
                }
                modelData.Lights.Add(ld);
                // 记录 node → light 索引映射（visibility 动画用）
                modelData.GltfNodeToLightIndex[node.LogicalIndex] = modelData.Lights.Count - 1;
            }
            foreach (Node child in node.VisualChildren) {
                ProcessNodeForMesh(
                    child,
                    modelData,
                    allNodes,
                    nodeToIndex,
                    ref bufferIndex,
                    materialToIndex,
                    nodeVisible
                );
            }
        }

        public static ModelMeshPartData ProcessPrimitive(MeshPrimitive primitive,
            ModelData modelData,
            ref int bufferIndex,
            Dictionary<GltfMaterial, int> materialToIndex) {
            // 获取顶点数据
            Accessor posAccessor = primitive.GetVertexAccessor("POSITION");
            if (posAccessor == null) {
                return null;
            }
            IAccessorArray<System.Numerics.Vector3> positions = posAccessor.AsVector3Array();
            IAccessorArray<System.Numerics.Vector3> normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
            IAccessorArray<System.Numerics.Vector2> uv0 = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
            IAccessorArray<System.Numerics.Vector2> uv1 = primitive.GetVertexAccessor("TEXCOORD_1")?.AsVector2Array();
            IAccessorArray<System.Numerics.Vector4> tangents = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();
            IAccessorArray<System.Numerics.Vector4> colors = primitive.GetVertexAccessor("COLOR_0")?.AsVector4Array();
            IAccessorArray<System.Numerics.Vector4> joints = primitive.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
            IAccessorArray<System.Numerics.Vector4> weights = primitive.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();

            // Morph Target 数据收集
            int morphTargetCount = primitive.MorphTargetsCount;
            System.Numerics.Vector3[][] morphPositions = null;
            System.Numerics.Vector3[][] morphNormals = null;
            System.Numerics.Vector4[][] morphTangentsArr = null;
            if (morphTargetCount > 0) {
                morphPositions = new System.Numerics.Vector3[morphTargetCount][];
                morphNormals = new System.Numerics.Vector3[morphTargetCount][];
                morphTangentsArr = new System.Numerics.Vector4[morphTargetCount][];
                for (int t = 0; t < morphTargetCount; t++) {
                    IReadOnlyDictionary<string, Accessor> ta = primitive.GetMorphTargetAccessors(t);
                    morphPositions[t] = ta.TryGetValue("POSITION", out Accessor mp) ? mp.AsVector3Array().ToArray() : null;
                    morphNormals[t] = ta.TryGetValue("NORMAL", out Accessor mn) ? mn.AsVector3Array().ToArray() : null;
                    morphTangentsArr[t] = ta.TryGetValue("TANGENT", out Accessor mt) ? mt.AsVector4Array().ToArray() : null;
                }
            }

            // 获取索引数据
            uint[] indices = primitive.GetIndices()?.ToArray();
            if (indices == null
                || indices.Length == 0) {
                // 如果没有索引，创建顺序索引（避免 LINQ 分配）
                indices = new uint[positions.Count];
                for (int i = 0; i < positions.Count; i++) {
                    indices[i] = (uint)i;
                }
            }

            // 无预计算切线时：先 unweld 再生成切线（仅 TRIANGLES，STRIP/FAN 索引不是三元组）
            // 共享顶点在 UV 缝合线处有冲突的切线方向，unweld 后每个三角形有独立顶点
            bool isTriangle = primitive.DrawPrimitiveType is GltfPrimitiveType.TRIANGLES
                or GltfPrimitiveType.TRIANGLE_STRIP
                or GltfPrimitiveType.TRIANGLE_FAN;
            bool isTrianglesOnly = primitive.DrawPrimitiveType == GltfPrimitiveType.TRIANGLES;
            System.Numerics.Vector4[] generatedTangents = null;
            System.Numerics.Vector3[] uwPos = null, uwNrm = null;
            System.Numerics.Vector2[] uwUv0 = null, uwUv1 = null;
            System.Numerics.Vector4[] uwJoints = null, uwWeights = null, uwColors = null;
            if (isTrianglesOnly
                && tangents == null
                && normals != null
                && uv0 != null
                && indices != null) {
                int idxCount = indices.Length;
                uwPos = new System.Numerics.Vector3[idxCount];
                uwNrm = new System.Numerics.Vector3[idxCount];
                uwUv0 = new System.Numerics.Vector2[idxCount];
                if (uv1 != null) {
                    uwUv1 = new System.Numerics.Vector2[idxCount];
                }
                if (colors != null) {
                    uwColors = new System.Numerics.Vector4[idxCount];
                }
                if (joints != null) {
                    uwJoints = new System.Numerics.Vector4[idxCount];
                }
                if (weights != null) {
                    uwWeights = new System.Numerics.Vector4[idxCount];
                }
                for (int i = 0; i < idxCount; i++) {
                    int idx = (int)indices[i];
                    uwPos[i] = positions[idx];
                    uwNrm[i] = normals[idx];
                    uwUv0[i] = uv0[idx];
                    if (uwUv1 != null) {
                        uwUv1[i] = uv1[idx];
                    }
                    if (uwColors != null) {
                        uwColors[i] = colors[idx];
                    }
                    if (uwJoints != null) {
                        uwJoints[i] = joints[idx];
                    }
                    if (uwWeights != null) {
                        uwWeights[i] = weights[idx];
                    }
                }

                // Unweld Morph Targets
                if (morphTargetCount > 0) {
                    for (int t = 0; t < morphTargetCount; t++) {
                        if (morphPositions[t] != null) {
                            System.Numerics.Vector3[] uw = new System.Numerics.Vector3[idxCount];
                            for (int i = 0; i < idxCount; i++) {
                                uw[i] = morphPositions[t][(int)indices[i]];
                            }
                            morphPositions[t] = uw;
                        }
                        if (morphNormals[t] != null) {
                            System.Numerics.Vector3[] uw = new System.Numerics.Vector3[idxCount];
                            for (int i = 0; i < idxCount; i++) {
                                uw[i] = morphNormals[t][(int)indices[i]];
                            }
                            morphNormals[t] = uw;
                        }
                        if (morphTangentsArr[t] != null) {
                            System.Numerics.Vector4[] uw = new System.Numerics.Vector4[idxCount];
                            for (int i = 0; i < idxCount; i++) {
                                uw[i] = morphTangentsArr[t][(int)indices[i]];
                            }
                            morphTangentsArr[t] = uw;
                        }
                    }
                }
                indices = new uint[idxCount];
                for (int i = 0; i < idxCount; i++) {
                    indices[i] = (uint)i;
                }
                generatedTangents = GenerateTangents(uwPos, uwNrm, uwUv0, indices);
            }

            // 构建顶点声明
            // 注意：着色器期望 Position, Normal, TexCoord 都是必需的
            List<VertexElement> elements = new();
            int offset = 0;

            // Position (Vector3)
            elements.Add(new VertexElement(offset, VertexElementFormat.Vector3, VertexElementSemantic.Position));
            offset += 12;

            // Normal (Vector3) - 始终添加，着色器期望有 NORMAL
            // 如果模型没有法线数据，使用默认值 (0, 1, 0)
            bool hasNormals = normals != null;
            elements.Add(new VertexElement(offset, VertexElementFormat.Vector3, VertexElementSemantic.Normal));
            offset += 12;

            // UV0 (Vector2) - 始终添加，着色器期望有 TEXCOORD
            // 如果模型没有 UV 数据，使用默认值 (0, 0)
            bool hasUV0 = uv0 != null;
            elements.Add(new VertexElement(offset, VertexElementFormat.Vector2, VertexElementSemantic.TextureCoordinate));
            offset += 8;

            // UV1 (Vector2) - 仅在有 TEXCOORD_1 数据时添加
            bool hasUV1 = uv1 != null;
            if (hasUV1) {
                elements.Add(new VertexElement(offset, VertexElementFormat.Vector2, VertexElementSemantic.TextureCoordinate1));
                offset += 8;
            }

            // Tangent (Vector4) - 仅在有 TANGENT 数据时添加
            bool hasTangents = tangents != null || generatedTangents != null;
            if (hasTangents) {
                elements.Add(new VertexElement(offset, VertexElementFormat.Vector4, VertexElementSemantic.Tangent));
                offset += 16;
            }

            // Color (Vector4) - 顶点颜色
            bool hasColors = colors != null;
            if (hasColors) {
                elements.Add(new VertexElement(offset, VertexElementFormat.Vector4, VertexElementSemantic.Color));
                offset += 16;
            }

            // BlendIndices (Vector4 - 作为 4 个 float 存储)
            // BlendWeights (Vector4)
            bool hasSkinning = joints != null && weights != null;
            if (hasSkinning) {
                elements.Add(new VertexElement(offset, VertexElementFormat.Vector4, VertexElementSemantic.BlendIndices));
                offset += 16;
                elements.Add(new VertexElement(offset, VertexElementFormat.Vector4, VertexElementSemantic.BlendWeights));
                offset += 16;
            }
            VertexDeclaration vertexDecl = new(elements.ToArray());

            // 构建顶点缓冲
            int vertexCount = uwPos != null ? uwPos.Length : positions.Count;
            byte[] vertexBuffer = new byte[vertexCount * offset];
            int vertexStride = offset;
            for (int i = 0; i < vertexCount; i++) {
                int baseOffset = i * vertexStride;
                int currentOffset = 0;

                // Position
                System.Numerics.Vector3 pos = uwPos != null ? uwPos[i] : positions[i];
                WriteVector3(vertexBuffer, baseOffset + currentOffset, pos.X, pos.Y, pos.Z);
                currentOffset += 12;

                // Normal - 始终写入，没有数据时使用默认向上法线
                if (hasNormals) {
                    System.Numerics.Vector3 normal = uwNrm != null ? uwNrm[i] : normals[i];
                    WriteVector3(vertexBuffer, baseOffset + currentOffset, normal.X, normal.Y, normal.Z);
                }
                else {
                    WriteVector3(vertexBuffer, baseOffset + currentOffset, 0f, 1f, 0f);
                }
                currentOffset += 12;

                // UV0 - 始终写入，没有数据时使用默认值
                if (hasUV0) {
                    System.Numerics.Vector2 uv = uwUv0 != null ? uwUv0[i] : uv0[i];
                    WriteVector2(vertexBuffer, baseOffset + currentOffset, uv.X, uv.Y);
                }
                else {
                    WriteVector2(vertexBuffer, baseOffset + currentOffset, 0f, 0f);
                }
                currentOffset += 8;

                // UV1 - 仅在有 TEXCOORD_1 数据时写入
                if (hasUV1) {
                    System.Numerics.Vector2 uv = uwUv1 != null ? uwUv1[i] : uv1[i];
                    WriteVector2(vertexBuffer, baseOffset + currentOffset, uv.X, uv.Y);
                    currentOffset += 8;
                }

                // Tangent
                if (hasTangents) {
                    if (tangents != null) {
                        System.Numerics.Vector4 t = tangents[i];
                        WriteVector4(vertexBuffer, baseOffset + currentOffset, t.X, t.Y, t.Z, t.W);
                    }
                    else {
                        System.Numerics.Vector4 t = generatedTangents[i];
                        WriteVector4(vertexBuffer, baseOffset + currentOffset, t.X, t.Y, t.Z, t.W);
                    }
                    currentOffset += 16;
                }

                // Color - 顶点颜色
                if (hasColors) {
                    System.Numerics.Vector4 c = uwColors != null ? uwColors[i] : colors[i];
                    WriteVector4(vertexBuffer, baseOffset + currentOffset, c.X, c.Y, c.Z, c.W);
                    currentOffset += 16;
                }

                // BlendIndices 和 BlendWeights
                if (hasSkinning) {
                    System.Numerics.Vector4 joint = uwJoints != null ? uwJoints[i] : joints[i];
                    System.Numerics.Vector4 weight = uwWeights != null ? uwWeights[i] : weights[i];

                    // BlendIndices (存储为 float)
                    WriteVector4(vertexBuffer, baseOffset + currentOffset, joint.X, joint.Y, joint.Z, joint.W);
                    currentOffset += 16;

                    // BlendWeights
                    WriteVector4(vertexBuffer, baseOffset + currentOffset, weight.X, weight.Y, weight.Z, weight.W);
                    currentOffset += 16;
                }
            }

            // 构建索引缓冲（统一使用 32 位索引，与 Collada 加载器保持一致）
            byte[] indexBuffer = new byte[indices.Length * 4];
            if (isTrianglesOnly) {
                // glTF 使用逆时针绕序 (CCW)，引擎使用 CullCounterClockwise，需要翻转绕序
                // 仅 TRIANGLES 图元的索引是三元组，STRIP/FAN 索引结构不同
                for (int triangle = 0; triangle < indices.Length / 3; triangle++) {
                    int baseIdx = triangle * 3;
                    // 翻转绕序：交换 v1 和 v2 (0,1,2 -> 0,2,1)
                    uint idx0 = indices[baseIdx];
                    uint idx1 = indices[baseIdx + 2]; // 交换
                    uint idx2 = indices[baseIdx + 1]; // 交换
                    WriteIndex32(indexBuffer, baseIdx, idx0);
                    WriteIndex32(indexBuffer, baseIdx + 1, idx1);
                    WriteIndex32(indexBuffer, baseIdx + 2, idx2);
                }
            }
            else {
                // 非三角形图元直接写入索引，不需要翻转绕序
                for (int i = 0; i < indices.Length; i++) {
                    WriteIndex32(indexBuffer, i, indices[i]);
                }
            }

            // 创建缓冲数据
            ModelBuffersData buffersData = new() { VertexDeclaration = vertexDecl, Vertices = vertexBuffer, Indices = indexBuffer };
            modelData.Buffers.Add(buffersData);

            // 计算包围盒
            BoundingBox bbox;
            if (uwPos != null) {
                Vector3 min = new(float.MaxValue);
                Vector3 max = new(float.MinValue);
                for (int i = 0; i < uwPos.Length; i++) {
                    min.X = Math.Min(min.X, uwPos[i].X);
                    min.Y = Math.Min(min.Y, uwPos[i].Y);
                    min.Z = Math.Min(min.Z, uwPos[i].Z);
                    max.X = Math.Max(max.X, uwPos[i].X);
                    max.Y = Math.Max(max.Y, uwPos[i].Y);
                    max.Z = Math.Max(max.Z, uwPos[i].Z);
                }
                bbox = new BoundingBox(min, max);
            }
            else {
                bbox = CalculateBoundingBoxFromPositions(positions, indices);
            }
            ModelMeshPartData meshPart = new() {
                BuffersDataIndex = bufferIndex++,
                StartIndex = 0,
                IndicesCount = indices.Length,
                BoundingBox = bbox,
                PrimitiveType = MapPrimitiveType(primitive.DrawPrimitiveType)
            };

            // 设置材质索引
            if (primitive.Material != null
                && materialToIndex.TryGetValue(primitive.Material, out int matIndex)) {
                meshPart.MaterialIndex = matIndex;
            }

            // Morph Target 纹理化
            if (morphTargetCount > 0) {
                HashSet<string> morphAttributes = new();
                for (int t = 0; t < morphTargetCount; t++) {
                    foreach (string attr in primitive.GetMorphTargetAccessors(t).Keys) {
                        morphAttributes.Add(attr);
                    }
                }
                int finalVertexCount = uwPos != null ? uwPos.Length : positions.Count;
                MorphTargetTexture morphTex = new(finalVertexCount, morphTargetCount, morphAttributes);
                // 转为数组列表用于 UploadData
                IReadOnlyList<Vector3>[] mpList = ToEngineVector3List(morphPositions);
                IReadOnlyList<Vector3>[] mnList = ToEngineVector3List(morphNormals);
                IReadOnlyList<Vector4>[] mtList = ToEngineVector4List(morphTangentsArr);
                morphTex.UploadData(mpList, mnList, mtList, null, null, null);
                meshPart.MorphTargetTexture = morphTex;
                meshPart.MorphTargetCount = morphTargetCount;
                IReadOnlyList<float> meshWeights = primitive.LogicalParent.MorphWeights;
                meshPart.MorphWeights = new float[morphTargetCount];
                if (meshWeights != null) {
                    for (int i = 0; i < Math.Min(meshWeights.Count, morphTargetCount); i++) {
                        meshPart.MorphWeights[i] = meshWeights[i];
                    }
                }
            }
            return meshPart;
        }

        public static IReadOnlyList<Vector3>[] ToEngineVector3List(System.Numerics.Vector3[][] arrays) {
            if (arrays == null) {
                return null;
            }
            IReadOnlyList<Vector3>[] result = new IReadOnlyList<Vector3>[arrays.Length];
            for (int t = 0; t < arrays.Length; t++) {
                if (arrays[t] == null) {
                    continue;
                }
                Vector3[] converted = new Vector3[arrays[t].Length];
                for (int i = 0; i < arrays[t].Length; i++) {
                    System.Numerics.Vector3 v = arrays[t][i];
                    converted[i] = new Vector3(v.X, v.Y, v.Z);
                }
                result[t] = converted;
            }
            return result;
        }

        public static IReadOnlyList<Vector4>[] ToEngineVector4List(System.Numerics.Vector4[][] arrays) {
            if (arrays == null) {
                return null;
            }
            IReadOnlyList<Vector4>[] result = new IReadOnlyList<Vector4>[arrays.Length];
            for (int t = 0; t < arrays.Length; t++) {
                if (arrays[t] == null) {
                    continue;
                }
                Vector4[] converted = new Vector4[arrays[t].Length];
                for (int i = 0; i < arrays[t].Length; i++) {
                    System.Numerics.Vector4 v = arrays[t][i];
                    converted[i] = new Vector4(v.X, v.Y, v.Z, v.W);
                }
                result[t] = converted;
            }
            return result;
        }

        public static PrimitiveType MapPrimitiveType(GltfPrimitiveType type) => type switch {
            GltfPrimitiveType.POINTS => PrimitiveType.Points,
            GltfPrimitiveType.LINES => PrimitiveType.LineList,
            GltfPrimitiveType.LINE_LOOP => PrimitiveType.LineLoop,
            GltfPrimitiveType.LINE_STRIP => PrimitiveType.LineStrip,
            GltfPrimitiveType.TRIANGLES => PrimitiveType.TriangleList,
            GltfPrimitiveType.TRIANGLE_STRIP => PrimitiveType.TriangleStrip,
            GltfPrimitiveType.TRIANGLE_FAN => PrimitiveType.TriangleFan,
            _ => PrimitiveType.TriangleList
        };

        public static System.Numerics.Vector4[] GenerateTangents(System.Numerics.Vector3[] positions,
            System.Numerics.Vector3[] normals,
            System.Numerics.Vector2[] uvs,
            uint[] indices) {
            if (positions == null
                || normals == null
                || uvs == null
                || indices == null) {
                return null;
            }
            int vertexCount = positions.Length;
            if (vertexCount == 0
                || normals.Length < vertexCount
                || uvs.Length < vertexCount) {
                return null;
            }
            System.Numerics.Vector3[] tan1 = new System.Numerics.Vector3[vertexCount];
            System.Numerics.Vector3[] tan2 = new System.Numerics.Vector3[vertexCount];
            for (int i = 0; i + 2 < indices.Length; i += 3) {
                int i0 = (int)indices[i], i1 = (int)indices[i + 1], i2 = (int)indices[i + 2];
                if ((uint)i0 >= vertexCount
                    || (uint)i1 >= vertexCount
                    || (uint)i2 >= vertexCount) {
                    continue;
                }
                System.Numerics.Vector3 p0 = positions[i0];
                System.Numerics.Vector3 p1 = positions[i1];
                System.Numerics.Vector3 p2 = positions[i2];
                System.Numerics.Vector2 uv0 = uvs[i0];
                System.Numerics.Vector2 uv1 = uvs[i1];
                System.Numerics.Vector2 uv2 = uvs[i2];
                System.Numerics.Vector3 edge1 = p1 - p0;
                System.Numerics.Vector3 edge2 = p2 - p0;
                System.Numerics.Vector2 duv1 = uv1 - uv0;
                System.Numerics.Vector2 duv2 = uv2 - uv0;
                float denom = duv1.X * duv2.Y - duv2.X * duv1.Y;
                if (MathF.Abs(denom) < 1e-8f) {
                    continue;
                }
                float inv = 1f / denom;
                System.Numerics.Vector3 sdir = (edge1 * duv2.Y - edge2 * duv1.Y) * inv;
                System.Numerics.Vector3 tdir = (edge2 * duv1.X - edge1 * duv2.X) * inv;
                tan1[i0] += sdir;
                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan2[i0] += tdir;
                tan2[i1] += tdir;
                tan2[i2] += tdir;
            }
            System.Numerics.Vector4[] tangents = new System.Numerics.Vector4[vertexCount];
            for (int i = 0; i < vertexCount; i++) {
                System.Numerics.Vector3 n = normals[i];
                if (n.LengthSquared() < float.Epsilon) {
                    tangents[i] = new System.Numerics.Vector4(1f, 0f, 0f, 1f);
                    continue;
                }
                n = System.Numerics.Vector3.Normalize(n);
                System.Numerics.Vector3 t = tan1[i];
                if (t.LengthSquared() < 1e-12f) {
                    // 零切线退化：选一个垂直于法线的方向
                    System.Numerics.Vector3 axis = MathF.Abs(n.Y) < 0.999f ? System.Numerics.Vector3.UnitY : System.Numerics.Vector3.UnitX;
                    t = System.Numerics.Vector3.Cross(axis, n);
                    if (t.LengthSquared() < 1e-12f) {
                        t = System.Numerics.Vector3.UnitX;
                    }
                    tangents[i] = new System.Numerics.Vector4(System.Numerics.Vector3.Normalize(t), 1f);
                    continue;
                }
                t = System.Numerics.Vector3.Normalize(t - n * System.Numerics.Vector3.Dot(n, t));
                System.Numerics.Vector3 b = System.Numerics.Vector3.Cross(n, t);
                // glTF 约定：bitangent = cross(N, T) * w，与 Lengyel 标准公式方向相反
                float w = System.Numerics.Vector3.Dot(b, tan2[i]) < 0f ? 1f : -1f;
                tangents[i] = new System.Numerics.Vector4(t, w);
            }
            return tangents;
        }

        public static unsafe void WriteFloat(byte[] buffer, int offset, float value) {
            // 使用指针直接写入，避免 BitConverter.GetBytes 的数组分配
            fixed (byte* ptr = &buffer[offset]) {
                *(float*)ptr = value;
            }
        }

        public static void WriteVector2(byte[] buffer, int offset, float x, float y) {
            WriteFloat(buffer, offset, x);
            WriteFloat(buffer, offset + 4, y);
        }

        public static void WriteVector3(byte[] buffer, int offset, float x, float y, float z) {
            WriteFloat(buffer, offset, x);
            WriteFloat(buffer, offset + 4, y);
            WriteFloat(buffer, offset + 8, z);
        }

        public static void WriteVector4(byte[] buffer, int offset, float x, float y, float z, float w) {
            WriteFloat(buffer, offset, x);
            WriteFloat(buffer, offset + 4, y);
            WriteFloat(buffer, offset + 8, z);
            WriteFloat(buffer, offset + 12, w);
        }

        public static void WriteIndex32(byte[] buffer, int elementIndex, uint value) {
            int offset = elementIndex * 4;
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        public static BoundingBox CalculateBoundingBoxFromPositions(IAccessorArray<System.Numerics.Vector3> positions, uint[] indices) {
            if (positions == null
                || positions.Count == 0) {
                return new BoundingBox(Vector3.Zero, Vector3.Zero);
            }
            Vector3 min = new(float.MaxValue);
            Vector3 max = new(float.MinValue);
            if (indices != null
                && indices.Length > 0) {
                foreach (uint idx in indices) {
                    if (idx < positions.Count) {
                        System.Numerics.Vector3 pos = positions[(int)idx];
                        min.X = Math.Min(min.X, pos.X);
                        min.Y = Math.Min(min.Y, pos.Y);
                        min.Z = Math.Min(min.Z, pos.Z);
                        max.X = Math.Max(max.X, pos.X);
                        max.Y = Math.Max(max.Y, pos.Y);
                        max.Z = Math.Max(max.Z, pos.Z);
                    }
                }
            }
            else {
                for (int i = 0; i < positions.Count; i++) {
                    System.Numerics.Vector3 pos = positions[i];
                    min.X = Math.Min(min.X, pos.X);
                    min.Y = Math.Min(min.Y, pos.Y);
                    min.Z = Math.Min(min.Z, pos.Z);
                    max.X = Math.Max(max.X, pos.X);
                    max.Y = Math.Max(max.Y, pos.Y);
                    max.Z = Math.Max(max.Z, pos.Z);
                }
            }
            return new BoundingBox(min, max);
        }

        public static void CalculateMeshBoundingBox(ModelMeshData meshData, List<ModelMeshPartData> parts) {
            if (parts.Count == 0) {
                meshData.BoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
                return;
            }
            Vector3 min = new(float.MaxValue);
            Vector3 max = new(float.MinValue);
            foreach (ModelMeshPartData part in parts) {
                BoundingBox bbox = part.BoundingBox;
                min.X = Math.Min(min.X, bbox.Min.X);
                min.Y = Math.Min(min.Y, bbox.Min.Y);
                min.Z = Math.Min(min.Z, bbox.Min.Z);
                max.X = Math.Max(max.X, bbox.Max.X);
                max.Y = Math.Max(max.Y, bbox.Max.Y);
                max.Z = Math.Max(max.Z, bbox.Max.Z);
            }
            meshData.BoundingBox = new BoundingBox(min, max);
        }
    }
}