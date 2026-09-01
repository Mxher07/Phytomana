using Engine;
using Engine.Graphics;

namespace Game {
    public class TerrainRenderer : IDisposable {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemSky m_subsystemSky;

        public SubsystemAnimatedTextures m_subsystemAnimatedTextures;

        public static Shader m_opaqueShader;

        public static Shader m_alphaTestedShader;

        public static Shader m_transparentShader;

        public SamplerState m_samplerState = new() {
            AddressModeU = TextureAddressMode.Clamp, AddressModeV = TextureAddressMode.Clamp, FilterMode = TextureFilterMode.Point, MaxLod = 0f
        };

        public SamplerState m_samplerStateMips = new() {
            AddressModeU = TextureAddressMode.Clamp,
            AddressModeV = TextureAddressMode.Clamp,
            FilterMode = TextureFilterMode.PointMipLinear,
            MaxLod = 4f
        };

        public DynamicArray<TerrainChunk> m_chunksToDraw = [];

        public static DynamicArray<int> m_tmpIndices = [];
        public static DynamicArray<TerrainVertex> m_tmpVertices = [];

        public static bool DrawChunksMap;

        public static int ChunksDrawn;

        public static int ChunkDrawCalls;

        public static int ChunkTrianglesDrawn;

        public virtual string ChunksGpuMemoryUsage {
            get {
                long num = 0L;
                TerrainChunk[] allocatedChunks = m_subsystemTerrain.Terrain.AllocatedChunks;
                foreach (TerrainChunk terrainChunk in allocatedChunks) {
                    if (terrainChunk.Geometry != null) {
                        foreach (TerrainChunkGeometry.Buffer buffer in terrainChunk.Buffers) {
                            num += buffer.VertexBuffer?.GetGpuMemoryUsage() ?? 0;
                            num += buffer.IndexBuffer?.GetGpuMemoryUsage() ?? 0;
                        }
                    }
                }
                return $"{num / 1024 / 1024:0.0}MB";
            }
        }

        public TerrainRenderer() { }

        public TerrainRenderer(SubsystemTerrain subsystemTerrain) {
            m_subsystemTerrain = subsystemTerrain;
            m_subsystemSky = subsystemTerrain.Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemAnimatedTextures = subsystemTerrain.SubsystemAnimatedTextures;
            m_opaqueShader ??= new Shader(
                ShaderCodeManager.GetFast("Shaders/Opaque.vsh"),
                ShaderCodeManager.GetFast("Shaders/Opaque.psh"),
                new ShaderMacro("Opaque")
            );
            m_alphaTestedShader ??= new Shader(
                ShaderCodeManager.GetFast("Shaders/AlphaTested.vsh"),
                ShaderCodeManager.GetFast("Shaders/AlphaTested.psh"),
                new ShaderMacro("ALPHATESTED")
            );
            m_transparentShader ??= new Shader(
                ShaderCodeManager.GetFast("Shaders/Transparent.vsh"),
                ShaderCodeManager.GetFast("Shaders/Transparent.psh"),
                new ShaderMacro("Transparent")
            );
            Display.DeviceReset += Display_DeviceReset;
        }

        public virtual void PrepareForDrawing(Camera camera) {
            Vector2 xZ = camera.ViewPosition.XZ;
            float num = MathUtils.Sqr(m_subsystemSky.VisibilityRange);
            BoundingFrustum viewFrustum = camera.ViewFrustum;
            int gameWidgetIndex = camera.GameWidget.GameWidgetIndex;
            m_chunksToDraw.Clear();
            TerrainChunk[] allocatedChunks = m_subsystemTerrain.Terrain.AllocatedChunks;
            foreach (TerrainChunk terrainChunk in allocatedChunks) {
                if (terrainChunk.NewGeometryData) {
                    lock (terrainChunk.Geometry) {
                        if (terrainChunk.NewGeometryData) {
                            terrainChunk.NewGeometryData = false;
                            SetupTerrainChunkGeometryVertexIndexBuffers(terrainChunk);
                        }
                    }
                }
                if (terrainChunk.Buffers.Count > 0
                    && Vector2.DistanceSquared(xZ, terrainChunk.Center) <= (double)num) {
                    if (viewFrustum.Intersection(terrainChunk.BoundingBox)) {
                        m_chunksToDraw.Add(terrainChunk);
                    }
                    if (terrainChunk.State != TerrainChunkState.Valid) {
                        continue;
                    }
                    float num2 = terrainChunk.HazeEnds[gameWidgetIndex];
                    if (num2 != 3.40282347E+38f) {
                        if (num2 == 0f) {
                            StartChunkFadeIn(camera, terrainChunk);
                        }
                        else {
                            RunChunkFadeIn(camera, terrainChunk);
                        }
                    }
                }
                else {
                    terrainChunk.HazeEnds[gameWidgetIndex] = 0f;
                }
            }
            ChunksDrawn = 0;
            ChunkDrawCalls = 0;
            ChunkTrianglesDrawn = 0;
        }

        public virtual void DrawOpaque(Camera camera) {
            int gameWidgetIndex = camera.GameWidget.GameWidgetIndex;
            Vector3 viewPosition = camera.InvertedViewMatrix.Translation;
            Vector3 v = new(MathF.Floor(viewPosition.X), 0f, MathF.Floor(viewPosition.Z));
            Matrix value = Matrix.CreateTranslation(v - viewPosition) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
            Display.BlendState = BlendState.Opaque;
            Display.DepthStencilState = DepthStencilState.Default;
            Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
            m_opaqueShader.GetParameter("u_origin", true).SetValue(v.XZ);
            m_opaqueShader.GetParameter("u_viewProjectionMatrix", true).SetValue(value);
            m_opaqueShader.GetParameter("u_viewPosition", true).SetValue(viewPosition);
            m_opaqueShader.GetParameter("u_samplerState", true).SetValue(SettingsManager.TerrainMipmapsEnabled ? m_samplerStateMips : m_samplerState);
            m_opaqueShader.GetParameter("u_fogYMultiplier", true).SetValue(m_subsystemSky.VisibilityRangeYMultiplier);
            m_opaqueShader.GetParameter("u_fogColor", true).SetValue(new Vector3(m_subsystemSky.ViewFogColor));
            m_opaqueShader.GetParameter("u_fogBottomTopDensity")
                .SetValue(new Vector3(m_subsystemSky.ViewFogBottom, m_subsystemSky.ViewFogTop, m_subsystemSky.ViewFogDensity));
            ShaderParameter parameter = m_opaqueShader.GetParameter("u_hazeStartDensity");
            ModsManager.HookAction(
                "SetShaderParameter",
                modLoader => {
                    modLoader.SetShaderParameter(m_opaqueShader, camera);
                    return true;
                }
            );
            //Point2 point = Terrain.ToChunk(camera.ViewPosition.XZ);
            //TerrainChunk chunk = m_subsystemTerrain.Terrain.GetChunkAtCoords(point.X, point.Y);
            for (int i = 0; i < m_chunksToDraw.Count; i++) {
                TerrainChunk terrainChunk = m_chunksToDraw[i];
                float num = MathUtils.Min(terrainChunk.HazeEnds[gameWidgetIndex], m_subsystemSky.ViewHazeStart + 1f / m_subsystemSky.ViewHazeDensity);
                float num2 = MathUtils.Min(m_subsystemSky.ViewHazeStart, num - 1f);
                parameter.SetValue(new Vector2(num2, 1f / (num - num2)));
                int num3 = 16;
                if (viewPosition.Z > terrainChunk.BoundingBox.Min.Z) {
                    num3 |= 1;
                }
                if (viewPosition.X > terrainChunk.BoundingBox.Min.X) {
                    num3 |= 2;
                }
                if (viewPosition.Z < terrainChunk.BoundingBox.Max.Z) {
                    num3 |= 4;
                }
                if (viewPosition.X < terrainChunk.BoundingBox.Max.X) {
                    num3 |= 8;
                }
                DrawTerrainChunkGeometrySubsets(m_opaqueShader, terrainChunk, num3);
                ChunksDrawn++;
            }
        }

        public virtual void DrawAlphaTested(Camera camera) {
            int gameWidgetIndex = camera.GameWidget.GameWidgetIndex;
            Vector3 viewPosition = camera.InvertedViewMatrix.Translation;
            Vector3 v = new(MathF.Floor(viewPosition.X), 0f, MathF.Floor(viewPosition.Z));
            Matrix value = Matrix.CreateTranslation(v - viewPosition) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
            Display.BlendState = BlendState.Opaque;
            Display.DepthStencilState = DepthStencilState.Default;
            Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
            m_alphaTestedShader.GetParameter("u_origin", true).SetValue(v.XZ);
            m_alphaTestedShader.GetParameter("u_viewProjectionMatrix", true).SetValue(value);
            m_alphaTestedShader.GetParameter("u_viewPosition", true).SetValue(viewPosition);
            m_alphaTestedShader.GetParameter("u_samplerState", true)
                .SetValue(SettingsManager.TerrainMipmapsEnabled ? m_samplerStateMips : m_samplerState);
            m_alphaTestedShader.GetParameter("u_fogYMultiplier", true).SetValue(m_subsystemSky.VisibilityRangeYMultiplier);
            m_alphaTestedShader.GetParameter("u_fogColor", true).SetValue(new Vector3(m_subsystemSky.ViewFogColor));
            m_alphaTestedShader.GetParameter("u_fogBottomTopDensity")
                .SetValue(new Vector3(m_subsystemSky.ViewFogBottom, m_subsystemSky.ViewFogTop, m_subsystemSky.ViewFogDensity));
            m_alphaTestedShader.GetParameter("u_alphaThreshold").SetValue(0.5f);
            ShaderParameter parameter = m_alphaTestedShader.GetParameter("u_hazeStartDensity");
            ModsManager.HookAction(
                "SetShaderParameter",
                modLoader => {
                    modLoader.SetShaderParameter(m_alphaTestedShader, camera);
                    return true;
                }
            );
            for (int i = 0; i < m_chunksToDraw.Count; i++) {
                TerrainChunk terrainChunk = m_chunksToDraw[i];
                float num = MathUtils.Min(terrainChunk.HazeEnds[gameWidgetIndex], m_subsystemSky.ViewHazeStart + 1f / m_subsystemSky.ViewHazeDensity);
                float num2 = MathUtils.Min(m_subsystemSky.ViewHazeStart, num - 1f);
                parameter.SetValue(new Vector2(num2, 1f / (num - num2)));
                int subsetsMask = 32;
                DrawTerrainChunkGeometrySubsets(m_alphaTestedShader, terrainChunk, subsetsMask);
            }
        }

        public virtual void DrawTransparent(Camera camera) {
            int gameWidgetIndex = camera.GameWidget.GameWidgetIndex;
            Vector3 viewPosition = camera.InvertedViewMatrix.Translation;
            Vector3 v = new(MathF.Floor(viewPosition.X), 0f, MathF.Floor(viewPosition.Z));
            Matrix value = Matrix.CreateTranslation(v - viewPosition) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
            Display.BlendState = BlendState.AlphaBlend;
            Display.DepthStencilState = DepthStencilState.Default;
            Display.RasterizerState = m_subsystemSky.ViewUnderWaterDepth > 0f
                ? RasterizerState.CullClockwiseScissor
                : RasterizerState.CullCounterClockwiseScissor;
            m_transparentShader.GetParameter("u_origin", true).SetValue(v.XZ);
            m_transparentShader.GetParameter("u_viewProjectionMatrix", true).SetValue(value);
            m_transparentShader.GetParameter("u_viewPosition", true).SetValue(viewPosition);
            m_transparentShader.GetParameter("u_samplerState", true)
                .SetValue(SettingsManager.TerrainMipmapsEnabled ? m_samplerStateMips : m_samplerState);
            m_transparentShader.GetParameter("u_fogYMultiplier", true).SetValue(m_subsystemSky.VisibilityRangeYMultiplier);
            m_transparentShader.GetParameter("u_fogColor", true).SetValue(new Vector3(m_subsystemSky.ViewFogColor));
            m_transparentShader.GetParameter("u_fogBottomTopDensity")
                .SetValue(new Vector3(m_subsystemSky.ViewFogBottom, m_subsystemSky.ViewFogTop, m_subsystemSky.ViewFogDensity));
            ShaderParameter parameter = m_transparentShader.GetParameter("u_hazeStartDensity");
            ModsManager.HookAction(
                "SetShaderParameter",
                modLoader => {
                    modLoader.SetShaderParameter(m_transparentShader, camera);
                    return true;
                }
            );
            for (int i = 0; i < m_chunksToDraw.Count; i++) {
                TerrainChunk terrainChunk = m_chunksToDraw[i];
                float num = MathUtils.Min(terrainChunk.HazeEnds[gameWidgetIndex], m_subsystemSky.ViewHazeStart + 1f / m_subsystemSky.ViewHazeDensity);
                float num2 = MathUtils.Min(m_subsystemSky.ViewHazeStart, num - 1f);
                parameter.SetValue(new Vector2(num2, 1f / (num - num2)));
                int subsetsMask = 64;
                DrawTerrainChunkGeometrySubsets(m_transparentShader, terrainChunk, subsetsMask);
            }
        }

        public virtual void Dispose() {
            Display.DeviceReset -= Display_DeviceReset;
        }

        public virtual void Display_DeviceReset() {
            m_subsystemTerrain.TerrainUpdater.DowngradeAllChunksState(TerrainChunkState.InvalidVertices1, false);
            TerrainChunk[] allocatedChunks = m_subsystemTerrain.Terrain.AllocatedChunks;
            foreach (TerrainChunk terrainChunk in allocatedChunks) {
                terrainChunk.DisposeVertexIndexBuffers();
            }
        }

        public virtual void DisposeTerrainChunkGeometryVertexIndexBuffers(TerrainChunk chunk) {
            foreach (TerrainChunkGeometry.Buffer buffer in chunk.Buffers) {
                buffer.Dispose();
            }
            chunk.Buffers.Clear();
            chunk.InvalidateSliceContentsHashes();
        }

        public virtual void SetupTerrainChunkGeometryVertexIndexBuffers(TerrainChunk chunk) {
            DisposeTerrainChunkGeometryVertexIndexBuffers(chunk);
            CompileDrawSubsets(chunk.ChunkSliceGeometries, chunk.Buffers);
            chunk.CopySliceContentsHashes();
        }

        public class SubsetStat {
            public int[] subsetTotalIndexCount = new int[7];
            public int[] subsetTotalVertexCount = new int[7];
            public int[] subsetSettedIndexCount = new int[7];
            public int[] subsetSettedVertexCount = new int[7];
            public int totalIndexCount;
            public int totalVertextCount;
            public TerrainChunkGeometry.Buffer Buffer;
        }

        public static Dictionary<Texture2D, SubsetStat> stat = new();

        public static void CompileDrawSubsets(TerrainGeometry[] chunkSliceGeometries,
            DynamicArray<TerrainChunkGeometry.Buffer> buffers,
            Func<TerrainVertex, TerrainVertex> vertexTransform = null) {
            stat.Clear();
            //按贴图进行分组统计Subset的顶点数与索引数
            for (int k = 0; k < chunkSliceGeometries.Length; k++) {
                TerrainGeometry geometry = chunkSliceGeometries[k]; //第k个slice
                //统计每个subset的indexCount与VertexCount
                foreach (KeyValuePair<Texture2D, TerrainGeometry> drawItem in geometry.Draws) {
                    TerrainGeometry subGeometry = drawItem.Value;
                    for (int i = 0; i < subGeometry.Subsets.Length; i++) {
                        if (!stat.TryGetValue(drawItem.Key, out SubsetStat subsetStat)) {
                            subsetStat = new SubsetStat();
                            stat.Add(drawItem.Key, subsetStat);
                        }
                        int ic = subGeometry.Subsets[i].Indices.Count;
                        int vc = subGeometry.Subsets[i].Vertices.Count;
                        subsetStat.subsetTotalIndexCount[i] += ic;
                        subsetStat.subsetTotalVertexCount[i] += vc;
                        subsetStat.totalIndexCount += ic;
                        subsetStat.totalVertextCount += vc;
                    }
                }
            }
            //按贴图分组完成，生成buffer
            foreach (KeyValuePair<Texture2D, SubsetStat> statItem in stat) {
                if (statItem.Value.totalIndexCount == 0) {
                    continue;
                }
                TerrainChunkGeometry.Buffer buffer = new();
                buffer.IndexBuffer = new IndexBuffer(IndexFormat.ThirtyTwoBits, statItem.Value.totalIndexCount);
                buffer.VertexBuffer = new VertexBuffer(TerrainVertex.VertexDeclaration, statItem.Value.totalVertextCount);
                buffer.Texture = statItem.Key;
                statItem.Value.Buffer = buffer;
                buffers.Add(buffer);
                int subsetSettedIndexCount = 0;
                int subsetSettedVertexCount = 0;
                for (int i = 0; i < 7; i++) {
                    if (i == 0) {
                        buffer.SubsetIndexBufferStarts[i] = 0;
                        buffer.SubsetIndexBufferEnds[i] = statItem.Value.subsetTotalIndexCount[i];
                        buffer.SubsetVertexBufferStarts[i] = 0;
                        buffer.SubsetVertexBufferEnds[i] = statItem.Value.subsetTotalVertexCount[i];
                        subsetSettedIndexCount = statItem.Value.subsetTotalIndexCount[i];
                        subsetSettedVertexCount = statItem.Value.subsetTotalVertexCount[i];
                    }
                    else {
                        buffer.SubsetIndexBufferStarts[i] = subsetSettedIndexCount;
                        buffer.SubsetIndexBufferEnds[i] = statItem.Value.subsetTotalIndexCount[i] + buffer.SubsetIndexBufferStarts[i];
                        buffer.SubsetVertexBufferStarts[i] = subsetSettedVertexCount;
                        buffer.SubsetVertexBufferEnds[i] = statItem.Value.subsetTotalVertexCount[i] + buffer.SubsetVertexBufferStarts[i];
                        subsetSettedIndexCount += statItem.Value.subsetTotalIndexCount[i];
                        subsetSettedVertexCount += statItem.Value.subsetTotalVertexCount[i];
                    }
                }
            }
            //将顶点列表与索引列表写入buffer
            for (int k = 0; k < chunkSliceGeometries.Length; k++) {
                TerrainGeometry geometry = chunkSliceGeometries[k]; //第k个slice
                //统计每个subset的indexCount与VertexCount
                foreach (KeyValuePair<Texture2D, TerrainGeometry> drawItem in geometry.Draws) {
                    TerrainGeometry subGeometry = drawItem.Value;
                    for (int i = 0; i < subGeometry.Subsets.Length; i++) {
                        if (stat.TryGetValue(drawItem.Key, out SubsetStat subsetStat)) {
                            if (subsetStat.totalIndexCount == 0) {
                                continue;
                            }
                            TerrainGeometryDynamicArray<int> indices = subGeometry.Subsets[i].Indices;
                            TerrainGeometryDynamicArray<TerrainVertex> vertices = subGeometry.Subsets[i].Vertices;
                            if (indices.Count > 0) {
                                TerrainChunkGeometry.Buffer buffer = subsetStat.Buffer;
                                m_tmpIndices.Count = indices.Count;
                                ShiftIndices(
                                    indices.Array,
                                    m_tmpIndices.Array,
                                    buffer.SubsetVertexBufferStarts[i] + subsetStat.subsetSettedVertexCount[i],
                                    indices.Count
                                );
                                buffer.IndexBuffer.SetData(
                                    m_tmpIndices.Array,
                                    0,
                                    indices.Count,
                                    buffer.SubsetIndexBufferStarts[i] + subsetStat.subsetSettedIndexCount[i]
                                );
                                if (vertexTransform != null) {
                                    m_tmpVertices.Count = vertices.Count;
                                    for (int j = 0; j < vertices.Count; j++) {
                                        m_tmpVertices[j] = vertexTransform(vertices[j]);
                                    }
                                    buffer.VertexBuffer.SetData(
                                        m_tmpVertices.Array,
                                        0,
                                        vertices.Count,
                                        buffer.SubsetVertexBufferStarts[i] + subsetStat.subsetSettedVertexCount[i]
                                    );
                                }
                                else {
                                    buffer.VertexBuffer.SetData(
                                        vertices.Array,
                                        0,
                                        vertices.Count,
                                        buffer.SubsetVertexBufferStarts[i] + subsetStat.subsetSettedVertexCount[i]
                                    );
                                }
                                subsetStat.subsetSettedIndexCount[i] += indices.Count;
                                subsetStat.subsetSettedVertexCount[i] += vertices.Count;
                            }
                        }
                    }
                }
            }
        }

        public virtual void DrawTerrainChunkGeometrySubsets(Shader shader, TerrainChunk chunk, int subsetsMask, bool ApplyTexture = true) {
            foreach (TerrainChunkGeometry.Buffer buffer in chunk.Buffers) {
                int num = 2147483647;
                int num2 = 0;
                for (int i = 0; i < 8; i++) {
                    if (i < 7
                        && (subsetsMask & (1 << i)) != 0) {
                        if (buffer.SubsetIndexBufferEnds[i] > 0) {
                            if (num == 2147483647) {
                                num = buffer.SubsetIndexBufferStarts[i];
                            }
                            num2 = buffer.SubsetIndexBufferEnds[i];
                        }
                    }
                    else {
                        if (num2 > num) {
                            if (ApplyTexture) {
                                shader.GetParameter("u_texture", true).SetValue(buffer.Texture);
                            }
                            Display.DrawIndexed(PrimitiveType.TriangleList, shader, buffer.VertexBuffer, buffer.IndexBuffer, num, num2 - num);
                            ChunkTrianglesDrawn += (num2 - num) / 3;
                            ChunkDrawCalls++;
                        }
                        num = 2147483647;
                    }
                }
            }
        }

        public virtual void StartChunkFadeIn(Camera camera, TerrainChunk chunk) {
            Vector3 viewPosition = camera.ViewPosition;
            Vector2 v = new(chunk.Origin.X, chunk.Origin.Y);
            Vector2 v2 = new(chunk.Origin.X + 16, chunk.Origin.Y);
            Vector2 v3 = new(chunk.Origin.X, chunk.Origin.Y + 16);
            Vector2 v4 = new(chunk.Origin.X + 16, chunk.Origin.Y + 16);
            float x = Vector2.Distance(viewPosition.XZ, v);
            float x2 = Vector2.Distance(viewPosition.XZ, v2);
            float x3 = Vector2.Distance(viewPosition.XZ, v3);
            float x4 = Vector2.Distance(viewPosition.XZ, v4);
            chunk.HazeEnds[camera.GameWidget.GameWidgetIndex] = MathF.Max(Math.Min(Math.Min(Math.Min(x, x2), x3), x4), 0.001f);
        }

        public virtual void RunChunkFadeIn(Camera camera, TerrainChunk chunk) {
            chunk.HazeEnds[camera.GameWidget.GameWidgetIndex] += 32f * Time.FrameDuration;
            if (chunk.HazeEnds[camera.GameWidget.GameWidgetIndex] >= m_subsystemSky.VisibilityRange) {
                chunk.HazeEnds[camera.GameWidget.GameWidgetIndex] = 3.40282347E+38f;
            }
        }

        public static void ShiftIndices(int[] source, int[] destination, int shift, int count) {
            for (int i = 0; i < count; i++) {
                destination[i] = source[i] + shift;
            }
        }
    }
}