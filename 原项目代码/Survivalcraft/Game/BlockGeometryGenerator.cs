using Engine;

namespace Game {
    public class BlockGeometryGenerator {
        public struct CornerLights {
            public float L000;

            public float L001;

            public float L010;

            public float L011;

            public float L100;

            public float L101;

            public float L110;

            public float L111;
        }

        public static Vector2[] m_textureCoordinates = [
            new(0.001f, 0.999f),
            new(0.999f, 0.999f),
            new(0.999f, 0.001f),
            new(0.001f, 0.001f),
            new(0.001f, 0.999f),
            new(0.999f, 0.999f),
            new(0.999f, 0.001f),
            new(0.001f, 0.001f)
        ];

        public Terrain Terrain;

        public SubsystemTerrain SubsystemTerrain;

        public SubsystemElectricity SubsystemElectricity;

        public SubsystemFurnitureBlockBehavior SubsystemFurnitureBlockBehavior;

        public SubsystemMetersBlockBehavior SubsystemMetersBlockBehavior;

        public SubsystemPalette SubsystemPalette;

        public DynamicArray<ElectricConnectionPath> m_tmpConnectionPaths = [];

        public Point3 m_cornerLightsPosition;

        public CornerLights[] m_cornerLightsByFace = new CornerLights[6];

        public bool[] m_visibleSides = new bool[6];

        public BlockGeometryGenerator() {}

        public BlockGeometryGenerator(Terrain terrain,
            SubsystemTerrain subsystemTerrain,
            SubsystemElectricity subsystemElectricity,
            SubsystemFurnitureBlockBehavior subsystemFurnitureBlockBehavior,
            SubsystemMetersBlockBehavior subsystemMetersBlockBehavior,
            SubsystemPalette subsystemPalette) {
            Terrain = terrain;
            SubsystemTerrain = subsystemTerrain;
            SubsystemElectricity = subsystemElectricity;
            SubsystemFurnitureBlockBehavior = subsystemFurnitureBlockBehavior;
            SubsystemMetersBlockBehavior = subsystemMetersBlockBehavior;
            SubsystemPalette = subsystemPalette;
            ResetCache();
        }

        public void ResetCache() {
            m_cornerLightsPosition = new Point3(2147483647);
        }

        public static void SetupCornerVertex(float x,
            float y,
            float z,
            Color color,
            int light,
            int face,
            int textureSlot,
            int textureSlotCount,
            int corner,
            ref TerrainVertex vertex) {
            float num = LightingManager.LightIntensityByLightValueAndFace[light + 16 * face];
            Color color2 = new((byte)(color.R * num), (byte)(color.G * num), (byte)(color.B * num), color.A);
            float tx = (m_textureCoordinates[corner].X + textureSlot % textureSlotCount) / textureSlotCount;
            float ty = (m_textureCoordinates[corner].Y + textureSlot / textureSlotCount) / textureSlotCount;
            SetupVertex(
                x,
                y,
                z,
                color2,
                tx,
                ty,
                ref vertex
            );
        }

        public static void SetupLitCornerVertex(float x,
            float y,
            float z,
            Color color,
            int textureSlot,
            int corner,
            ref TerrainVertex vertex) {
            SetupLitCornerVertex(
                x,
                y,
                z,
                color,
                textureSlot,
                16,
                corner,
                ref vertex
            );
        }

        public static void SetupLitCornerVertex(float x,
            float y,
            float z,
            Color color,
            int textureSlot,
            int textureSlotCount,
            int corner,
            ref TerrainVertex vertex) {
            float tx = (m_textureCoordinates[corner].X + textureSlot % textureSlotCount) / textureSlotCount;
            float ty = (m_textureCoordinates[corner].Y + textureSlot / textureSlotCount) / textureSlotCount;
            SetupVertex(
                x,
                y,
                z,
                color,
                tx,
                ty,
                ref vertex
            );
        }

        public static void SetupVertex(float x,
            float y,
            float z,
            Color color,
            float tx,
            float ty,
            ref TerrainVertex vertex) {
            vertex.X = x;
            vertex.Y = y;
            vertex.Z = z;
            vertex.Tx = (short)(tx * 32767f);
            vertex.Ty = (short)(ty * 32767f);
            vertex.Color = color;
        }

        public virtual void GenerateCrossfaceVertices(Block block,
            int value,
            int x,
            int y,
            int z,
            Color color,
            int textureSlot,
            TerrainGeometrySubset subset) {
            DynamicArray<TerrainVertex> vertices = subset.Vertices;
            TerrainGeometryDynamicArray<int> indices = subset.Indices;
            int num = Terrain.ExtractLight(value);
            float num2 = LightingManager.LightIntensityByLightValueAndFace[num + 64];
            Color color2 = new((byte)(color.R * num2), (byte)(color.G * num2), (byte)(color.B * num2), color.A);
            int count = vertices.Count;
            vertices.Count += 8;
            int textureSlotCount = block.GetTextureSlotCount(value);
            if ((x & 1) == 0) {
                SetupLitCornerVertex(
                    x,
                    y,
                    z,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    0,
                    ref vertices.Array[count]
                );
                SetupLitCornerVertex(
                    x + 1,
                    y,
                    z + 1,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    1,
                    ref vertices.Array[count + 1]
                );
                SetupLitCornerVertex(
                    x + 1,
                    y + 1,
                    z + 1,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    2,
                    ref vertices.Array[count + 2]
                );
                SetupLitCornerVertex(
                    x,
                    y + 1,
                    z,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    3,
                    ref vertices.Array[count + 3]
                );
            }
            else {
                SetupLitCornerVertex(
                    x,
                    y,
                    z,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    1,
                    ref vertices.Array[count]
                );
                SetupLitCornerVertex(
                    x + 1,
                    y,
                    z + 1,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    0,
                    ref vertices.Array[count + 1]
                );
                SetupLitCornerVertex(
                    x + 1,
                    y + 1,
                    z + 1,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    3,
                    ref vertices.Array[count + 2]
                );
                SetupLitCornerVertex(
                    x,
                    y + 1,
                    z,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    2,
                    ref vertices.Array[count + 3]
                );
            }
            if ((z & 1) == 0) {
                SetupLitCornerVertex(
                    x,
                    y,
                    z + 1,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    0,
                    ref vertices.Array[count + 4]
                );
                SetupLitCornerVertex(
                    x + 1,
                    y,
                    z,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    1,
                    ref vertices.Array[count + 5]
                );
                SetupLitCornerVertex(
                    x + 1,
                    y + 1,
                    z,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    2,
                    ref vertices.Array[count + 6]
                );
                SetupLitCornerVertex(
                    x,
                    y + 1,
                    z + 1,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    3,
                    ref vertices.Array[count + 7]
                );
            }
            else {
                SetupLitCornerVertex(
                    x,
                    y,
                    z + 1,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    1,
                    ref vertices.Array[count + 4]
                );
                SetupLitCornerVertex(
                    x + 1,
                    y,
                    z,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    0,
                    ref vertices.Array[count + 5]
                );
                SetupLitCornerVertex(
                    x + 1,
                    y + 1,
                    z,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    3,
                    ref vertices.Array[count + 6]
                );
                SetupLitCornerVertex(
                    x,
                    y + 1,
                    z + 1,
                    color2,
                    textureSlot,
                    textureSlotCount,
                    2,
                    ref vertices.Array[count + 7]
                );
            }
            int count2 = indices.Count;
            indices.Count += 24;
            indices.Array[count2] = count;
            indices.Array[count2 + 1] = count + 1;
            indices.Array[count2 + 2] = count + 2;
            indices.Array[count2 + 3] = count + 2;
            indices.Array[count2 + 4] = count + 1;
            indices.Array[count2 + 5] = count;
            indices.Array[count2 + 6] = count + 2;
            indices.Array[count2 + 7] = count + 3;
            indices.Array[count2 + 8] = count;
            indices.Array[count2 + 9] = count;
            indices.Array[count2 + 10] = count + 3;
            indices.Array[count2 + 11] = count + 2;
            indices.Array[count2 + 12] = count + 4;
            indices.Array[count2 + 13] = count + 5;
            indices.Array[count2 + 14] = count + 6;
            indices.Array[count2 + 15] = count + 6;
            indices.Array[count2 + 16] = count + 5;
            indices.Array[count2 + 17] = count + 4;
            indices.Array[count2 + 18] = count + 6;
            indices.Array[count2 + 19] = count + 7;
            indices.Array[count2 + 20] = count + 4;
            indices.Array[count2 + 21] = count + 4;
            indices.Array[count2 + 22] = count + 7;
            indices.Array[count2 + 23] = count + 6;
        }

        public virtual void GenerateCubeVertices(Block block,
            int value,
            int x,
            int y,
            int z,
            Color color,
            TerrainGeometrySubset[] subsetsByFace) {
            int blockIndex = block.BlockIndex;
            TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
            TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z + 1);
            TerrainChunk chunkAtCell3 = Terrain.GetChunkAtCell(x + 1, z);
            TerrainChunk chunkAtCell4 = Terrain.GetChunkAtCell(x, z - 1);
            TerrainChunk chunkAtCell5 = Terrain.GetChunkAtCell(x - 1, z);
            int cellValueFast = chunkAtCell2.GetCellValueFast(x & 0xF, y, (z + 1) & 0xF);
            int textureSlotCount = block.GetTextureSlotCount(value);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    0,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices = subsetsByFace[0].Vertices;
                TerrainGeometryDynamicArray<int> indices = subsetsByFace[0].Indices;
                int faceTextureSlot = block.GetFaceTextureSlot(0, value);
                int count = vertices.Count;
                vertices.Count += 4;
                SetupCubeVertexFace0(
                    x,
                    y,
                    z + 1,
                    1f,
                    0,
                    faceTextureSlot,
                    textureSlotCount,
                    color,
                    ref vertices.Array[count]
                ); //A
                SetupCubeVertexFace0(
                    x + 1,
                    y,
                    z + 1,
                    1f,
                    1,
                    faceTextureSlot,
                    textureSlotCount,
                    color,
                    ref vertices.Array[count + 1]
                ); //B
                SetupCubeVertexFace0(
                    x + 1,
                    y + 1,
                    z + 1,
                    1f,
                    2,
                    faceTextureSlot,
                    textureSlotCount,
                    color,
                    ref vertices.Array[count + 2]
                ); //C
                SetupCubeVertexFace0(
                    x,
                    y + 1,
                    z + 1,
                    1f,
                    3,
                    faceTextureSlot,
                    textureSlotCount,
                    color,
                    ref vertices.Array[count + 3]
                ); //D
                int count2 = indices.Count;
                indices.Count += 6;
                indices.Array[count2] = count; //A
                indices.Array[count2 + 1] = count + 2; //C
                indices.Array[count2 + 2] = count + 1; //B
                indices.Array[count2 + 3] = count + 2; //C
                indices.Array[count2 + 4] = count; //A
                indices.Array[count2 + 5] = count + 3; //D
            }
            cellValueFast = chunkAtCell3.GetCellValueFast((x + 1) & 0xF, y, z & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    1,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices2 = subsetsByFace[1].Vertices;
                TerrainGeometryDynamicArray<int> indices2 = subsetsByFace[1].Indices;
                int faceTextureSlot2 = block.GetFaceTextureSlot(1, value);
                int count3 = vertices2.Count;
                vertices2.Count += 4;
                SetupCubeVertexFace1(
                    x + 1,
                    y,
                    z,
                    1f,
                    1,
                    faceTextureSlot2,
                    textureSlotCount,
                    color,
                    ref vertices2.Array[count3]
                );
                SetupCubeVertexFace1(
                    x + 1,
                    y + 1,
                    z,
                    1f,
                    2,
                    faceTextureSlot2,
                    textureSlotCount,
                    color,
                    ref vertices2.Array[count3 + 1]
                );
                SetupCubeVertexFace1(
                    x + 1,
                    y + 1,
                    z + 1,
                    1f,
                    3,
                    faceTextureSlot2,
                    textureSlotCount,
                    color,
                    ref vertices2.Array[count3 + 2]
                );
                SetupCubeVertexFace1(
                    x + 1,
                    y,
                    z + 1,
                    1f,
                    0,
                    faceTextureSlot2,
                    textureSlotCount,
                    color,
                    ref vertices2.Array[count3 + 3]
                );
                int count4 = indices2.Count;
                indices2.Count += 6;
                indices2.Array[count4] = count3;
                indices2.Array[count4 + 1] = count3 + 2;
                indices2.Array[count4 + 2] = count3 + 1;
                indices2.Array[count4 + 3] = count3 + 2;
                indices2.Array[count4 + 4] = count3;
                indices2.Array[count4 + 5] = count3 + 3;
            }
            cellValueFast = chunkAtCell4.GetCellValueFast(x & 0xF, y, (z - 1) & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    2,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices3 = subsetsByFace[2].Vertices;
                TerrainGeometryDynamicArray<int> indices3 = subsetsByFace[2].Indices;
                int faceTextureSlot3 = block.GetFaceTextureSlot(2, value);
                int count5 = vertices3.Count;
                vertices3.Count += 4;
                SetupCubeVertexFace2(
                    x,
                    y,
                    z,
                    1f,
                    1,
                    faceTextureSlot3,
                    textureSlotCount,
                    color,
                    ref vertices3.Array[count5]
                );
                SetupCubeVertexFace2(
                    x + 1,
                    y,
                    z,
                    1f,
                    0,
                    faceTextureSlot3,
                    textureSlotCount,
                    color,
                    ref vertices3.Array[count5 + 1]
                );
                SetupCubeVertexFace2(
                    x + 1,
                    y + 1,
                    z,
                    1f,
                    3,
                    faceTextureSlot3,
                    textureSlotCount,
                    color,
                    ref vertices3.Array[count5 + 2]
                );
                SetupCubeVertexFace2(
                    x,
                    y + 1,
                    z,
                    1f,
                    2,
                    faceTextureSlot3,
                    textureSlotCount,
                    color,
                    ref vertices3.Array[count5 + 3]
                );
                int count6 = indices3.Count;
                indices3.Count += 6;
                indices3.Array[count6] = count5;
                indices3.Array[count6 + 1] = count5 + 1;
                indices3.Array[count6 + 2] = count5 + 2;
                indices3.Array[count6 + 3] = count5 + 2;
                indices3.Array[count6 + 4] = count5 + 3;
                indices3.Array[count6 + 5] = count5;
            }
            cellValueFast = chunkAtCell5.GetCellValueFast((x - 1) & 0xF, y, z & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    3,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices4 = subsetsByFace[3].Vertices;
                TerrainGeometryDynamicArray<int> indices4 = subsetsByFace[3].Indices;
                int faceTextureSlot4 = block.GetFaceTextureSlot(3, value);
                int count7 = vertices4.Count;
                vertices4.Count += 4;
                SetupCubeVertexFace3(
                    x,
                    y,
                    z,
                    1f,
                    0,
                    faceTextureSlot4,
                    textureSlotCount,
                    color,
                    ref vertices4.Array[count7]
                );
                SetupCubeVertexFace3(
                    x,
                    y + 1,
                    z,
                    1f,
                    3,
                    faceTextureSlot4,
                    textureSlotCount,
                    color,
                    ref vertices4.Array[count7 + 1]
                );
                SetupCubeVertexFace3(
                    x,
                    y + 1,
                    z + 1,
                    1f,
                    2,
                    faceTextureSlot4,
                    textureSlotCount,
                    color,
                    ref vertices4.Array[count7 + 2]
                );
                SetupCubeVertexFace3(
                    x,
                    y,
                    z + 1,
                    1f,
                    1,
                    faceTextureSlot4,
                    textureSlotCount,
                    color,
                    ref vertices4.Array[count7 + 3]
                );
                int count8 = indices4.Count;
                indices4.Count += 6;
                indices4.Array[count8] = count7;
                indices4.Array[count8 + 1] = count7 + 1;
                indices4.Array[count8 + 2] = count7 + 2;
                indices4.Array[count8 + 3] = count7 + 2;
                indices4.Array[count8 + 4] = count7 + 3;
                indices4.Array[count8 + 5] = count7;
            }
            cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y + 1, z & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    4,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices5 = subsetsByFace[4].Vertices;
                TerrainGeometryDynamicArray<int> indices5 = subsetsByFace[4].Indices;
                int faceTextureSlot5 = block.GetFaceTextureSlot(4, value);
                int count9 = vertices5.Count;
                vertices5.Count += 4;
                SetupCubeVertexFace4(
                    x,
                    y + 1,
                    z,
                    1f,
                    3,
                    faceTextureSlot5,
                    textureSlotCount,
                    color,
                    ref vertices5.Array[count9]
                );
                SetupCubeVertexFace4(
                    x + 1,
                    y + 1,
                    z,
                    1f,
                    2,
                    faceTextureSlot5,
                    textureSlotCount,
                    color,
                    ref vertices5.Array[count9 + 1]
                );
                SetupCubeVertexFace4(
                    x + 1,
                    y + 1,
                    z + 1,
                    1f,
                    1,
                    faceTextureSlot5,
                    textureSlotCount,
                    color,
                    ref vertices5.Array[count9 + 2]
                );
                SetupCubeVertexFace4(
                    x,
                    y + 1,
                    z + 1,
                    1f,
                    0,
                    faceTextureSlot5,
                    textureSlotCount,
                    color,
                    ref vertices5.Array[count9 + 3]
                );
                int count10 = indices5.Count;
                indices5.Count += 6;
                indices5.Array[count10] = count9;
                indices5.Array[count10 + 1] = count9 + 1;
                indices5.Array[count10 + 2] = count9 + 2;
                indices5.Array[count10 + 3] = count9 + 2;
                indices5.Array[count10 + 4] = count9 + 3;
                indices5.Array[count10 + 5] = count9;
            }
            cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y - 1, z & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    5,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices6 = subsetsByFace[5].Vertices;
                TerrainGeometryDynamicArray<int> indices6 = subsetsByFace[5].Indices;
                int faceTextureSlot6 = block.GetFaceTextureSlot(5, value);
                int count11 = vertices6.Count;
                vertices6.Count += 4;
                SetupCubeVertexFace5(
                    x,
                    y,
                    z,
                    1f,
                    0,
                    faceTextureSlot6,
                    textureSlotCount,
                    color,
                    ref vertices6.Array[count11]
                );
                SetupCubeVertexFace5(
                    x + 1,
                    y,
                    z,
                    1f,
                    1,
                    faceTextureSlot6,
                    textureSlotCount,
                    color,
                    ref vertices6.Array[count11 + 1]
                );
                SetupCubeVertexFace5(
                    x + 1,
                    y,
                    z + 1,
                    1f,
                    2,
                    faceTextureSlot6,
                    textureSlotCount,
                    color,
                    ref vertices6.Array[count11 + 2]
                );
                SetupCubeVertexFace5(
                    x,
                    y,
                    z + 1,
                    1f,
                    3,
                    faceTextureSlot6,
                    textureSlotCount,
                    color,
                    ref vertices6.Array[count11 + 3]
                );
                int count12 = indices6.Count;
                indices6.Count += 6;
                indices6.Array[count12] = count11;
                indices6.Array[count12 + 1] = count11 + 2;
                indices6.Array[count12 + 2] = count11 + 1;
                indices6.Array[count12 + 3] = count11 + 2;
                indices6.Array[count12 + 4] = count11;
                indices6.Array[count12 + 5] = count11 + 3;
            }
        }

        public virtual void GenerateCubeVertices(Block block,
            int value,
            int x,
            int y,
            int z,
            float height11,
            float height21,
            float height22,
            float height12,
            Color sideColor,
            Color topColor11,
            Color topColor21,
            Color topColor22,
            Color topColor12,
            int overrideTopTextureSlot,
            TerrainGeometrySubset[] subsetsByFace) {
            int blockIndex = block.BlockIndex;
            TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
            TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z + 1);
            TerrainChunk chunkAtCell3 = Terrain.GetChunkAtCell(x + 1, z);
            TerrainChunk chunkAtCell4 = Terrain.GetChunkAtCell(x, z - 1);
            TerrainChunk chunkAtCell5 = Terrain.GetChunkAtCell(x - 1, z);
            int cellValueFast = chunkAtCell2.GetCellValueFast(x & 0xF, y, (z + 1) & 0xF);
            int textureSlotCount = block.GetTextureSlotCount(value);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    0,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices = subsetsByFace[0].Vertices;
                TerrainGeometryDynamicArray<int> indices = subsetsByFace[0].Indices;
                int faceTextureSlot = block.GetFaceTextureSlot(0, value);
                int count = vertices.Count;
                vertices.Count += 4;
                SetupCubeVertexFace0(
                    x,
                    y,
                    z + 1,
                    1f,
                    0,
                    faceTextureSlot,
                    textureSlotCount,
                    sideColor,
                    ref vertices.Array[count]
                );
                SetupCubeVertexFace0(
                    x + 1,
                    y,
                    z + 1,
                    1f,
                    1,
                    faceTextureSlot,
                    textureSlotCount,
                    sideColor,
                    ref vertices.Array[count + 1]
                );
                SetupCubeVertexFace0(
                    x + 1,
                    y + 1,
                    z + 1,
                    height22,
                    2,
                    faceTextureSlot,
                    textureSlotCount,
                    sideColor,
                    ref vertices.Array[count + 2]
                );
                SetupCubeVertexFace0(
                    x,
                    y + 1,
                    z + 1,
                    height12,
                    3,
                    faceTextureSlot,
                    textureSlotCount,
                    sideColor,
                    ref vertices.Array[count + 3]
                );
                int count2 = indices.Count;
                indices.Count += 6;
                indices.Array[count2] = count;
                indices.Array[count2 + 1] = count + 2;
                indices.Array[count2 + 2] = count + 1;
                indices.Array[count2 + 3] = count + 2;
                indices.Array[count2 + 4] = count;
                indices.Array[count2 + 5] = count + 3;
            }
            cellValueFast = chunkAtCell3.GetCellValueFast((x + 1) & 0xF, y, z & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    1,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices2 = subsetsByFace[1].Vertices;
                TerrainGeometryDynamicArray<int> indices2 = subsetsByFace[1].Indices;
                int faceTextureSlot2 = block.GetFaceTextureSlot(1, value);
                int count3 = vertices2.Count;
                vertices2.Count += 4;
                SetupCubeVertexFace1(
                    x + 1,
                    y,
                    z,
                    1f,
                    1,
                    faceTextureSlot2,
                    textureSlotCount,
                    sideColor,
                    ref vertices2.Array[count3]
                );
                SetupCubeVertexFace1(
                    x + 1,
                    y + 1,
                    z,
                    height21,
                    2,
                    faceTextureSlot2,
                    textureSlotCount,
                    sideColor,
                    ref vertices2.Array[count3 + 1]
                );
                SetupCubeVertexFace1(
                    x + 1,
                    y + 1,
                    z + 1,
                    height22,
                    3,
                    faceTextureSlot2,
                    textureSlotCount,
                    sideColor,
                    ref vertices2.Array[count3 + 2]
                );
                SetupCubeVertexFace1(
                    x + 1,
                    y,
                    z + 1,
                    1f,
                    0,
                    faceTextureSlot2,
                    textureSlotCount,
                    sideColor,
                    ref vertices2.Array[count3 + 3]
                );
                int count4 = indices2.Count;
                indices2.Count += 6;
                indices2.Array[count4] = count3;
                indices2.Array[count4 + 1] = count3 + 2;
                indices2.Array[count4 + 2] = count3 + 1;
                indices2.Array[count4 + 3] = count3 + 2;
                indices2.Array[count4 + 4] = count3;
                indices2.Array[count4 + 5] = count3 + 3;
            }
            cellValueFast = chunkAtCell4.GetCellValueFast(x & 0xF, y, (z - 1) & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    2,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices3 = subsetsByFace[2].Vertices;
                TerrainGeometryDynamicArray<int> indices3 = subsetsByFace[2].Indices;
                int faceTextureSlot3 = block.GetFaceTextureSlot(2, value);
                int count5 = vertices3.Count;
                vertices3.Count += 4;
                SetupCubeVertexFace2(
                    x,
                    y,
                    z,
                    1f,
                    1,
                    faceTextureSlot3,
                    textureSlotCount,
                    sideColor,
                    ref vertices3.Array[count5]
                );
                SetupCubeVertexFace2(
                    x + 1,
                    y,
                    z,
                    1f,
                    0,
                    faceTextureSlot3,
                    textureSlotCount,
                    sideColor,
                    ref vertices3.Array[count5 + 1]
                );
                SetupCubeVertexFace2(
                    x + 1,
                    y + 1,
                    z,
                    height21,
                    3,
                    faceTextureSlot3,
                    textureSlotCount,
                    sideColor,
                    ref vertices3.Array[count5 + 2]
                );
                SetupCubeVertexFace2(
                    x,
                    y + 1,
                    z,
                    height11,
                    2,
                    faceTextureSlot3,
                    textureSlotCount,
                    sideColor,
                    ref vertices3.Array[count5 + 3]
                );
                int count6 = indices3.Count;
                indices3.Count += 6;
                indices3.Array[count6] = count5;
                indices3.Array[count6 + 1] = count5 + 1;
                indices3.Array[count6 + 2] = count5 + 2;
                indices3.Array[count6 + 3] = count5 + 2;
                indices3.Array[count6 + 4] = count5 + 3;
                indices3.Array[count6 + 5] = count5;
            }
            cellValueFast = chunkAtCell5.GetCellValueFast((x - 1) & 0xF, y, z & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    3,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices4 = subsetsByFace[3].Vertices;
                TerrainGeometryDynamicArray<int> indices4 = subsetsByFace[3].Indices;
                int faceTextureSlot4 = block.GetFaceTextureSlot(3, value);
                int count7 = vertices4.Count;
                vertices4.Count += 4;
                SetupCubeVertexFace3(
                    x,
                    y,
                    z,
                    1f,
                    0,
                    faceTextureSlot4,
                    textureSlotCount,
                    sideColor,
                    ref vertices4.Array[count7]
                );
                SetupCubeVertexFace3(
                    x,
                    y + 1,
                    z,
                    height11,
                    3,
                    faceTextureSlot4,
                    textureSlotCount,
                    sideColor,
                    ref vertices4.Array[count7 + 1]
                );
                SetupCubeVertexFace3(
                    x,
                    y + 1,
                    z + 1,
                    height12,
                    2,
                    faceTextureSlot4,
                    textureSlotCount,
                    sideColor,
                    ref vertices4.Array[count7 + 2]
                );
                SetupCubeVertexFace3(
                    x,
                    y,
                    z + 1,
                    1f,
                    1,
                    faceTextureSlot4,
                    textureSlotCount,
                    sideColor,
                    ref vertices4.Array[count7 + 3]
                );
                int count8 = indices4.Count;
                indices4.Count += 6;
                indices4.Array[count8] = count7;
                indices4.Array[count8 + 1] = count7 + 1;
                indices4.Array[count8 + 2] = count7 + 2;
                indices4.Array[count8 + 3] = count7 + 2;
                indices4.Array[count8 + 4] = count7 + 3;
                indices4.Array[count8 + 5] = count7;
            }
            cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y + 1, z & 0xF);
            if (((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                    && block.ShouldGenerateFace(
                        SubsystemTerrain,
                        4,
                        value,
                        cellValueFast,
                        x,
                        y,
                        z
                    ))
                || height11 < 1f
                || height12 < 1f
                || height21 < 1f
                || height22 < 1f) {
                DynamicArray<TerrainVertex> vertices5 = subsetsByFace[4].Vertices;
                TerrainGeometryDynamicArray<int> indices5 = subsetsByFace[4].Indices;
                int textureSlot = overrideTopTextureSlot >= 0 ? overrideTopTextureSlot : block.GetFaceTextureSlot(4, value);
                int count9 = vertices5.Count;
                vertices5.Count += 4;
                SetupCubeVertexFace4(
                    x,
                    y + 1,
                    z,
                    height11,
                    3,
                    textureSlot,
                    textureSlotCount,
                    topColor11,
                    ref vertices5.Array[count9]
                );
                SetupCubeVertexFace4(
                    x + 1,
                    y + 1,
                    z,
                    height21,
                    2,
                    textureSlot,
                    textureSlotCount,
                    topColor21,
                    ref vertices5.Array[count9 + 1]
                );
                SetupCubeVertexFace4(
                    x + 1,
                    y + 1,
                    z + 1,
                    height22,
                    1,
                    textureSlot,
                    textureSlotCount,
                    topColor22,
                    ref vertices5.Array[count9 + 2]
                );
                SetupCubeVertexFace4(
                    x,
                    y + 1,
                    z + 1,
                    height12,
                    0,
                    textureSlot,
                    textureSlotCount,
                    topColor12,
                    ref vertices5.Array[count9 + 3]
                );
                int count10 = indices5.Count;
                indices5.Count += 6;
                indices5.Array[count10] = count9;
                indices5.Array[count10 + 1] = count9 + 1;
                indices5.Array[count10 + 2] = count9 + 2;
                indices5.Array[count10 + 3] = count9 + 2;
                indices5.Array[count10 + 4] = count9 + 3;
                indices5.Array[count10 + 5] = count9;
            }
            cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y - 1, z & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    5,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices6 = subsetsByFace[5].Vertices;
                TerrainGeometryDynamicArray<int> indices6 = subsetsByFace[5].Indices;
                int faceTextureSlot5 = block.GetFaceTextureSlot(5, value);
                int count11 = vertices6.Count;
                vertices6.Count += 4;
                SetupCubeVertexFace5(
                    x,
                    y,
                    z,
                    1f,
                    0,
                    faceTextureSlot5,
                    textureSlotCount,
                    sideColor,
                    ref vertices6.Array[count11]
                );
                SetupCubeVertexFace5(
                    x + 1,
                    y,
                    z,
                    1f,
                    1,
                    faceTextureSlot5,
                    textureSlotCount,
                    sideColor,
                    ref vertices6.Array[count11 + 1]
                );
                SetupCubeVertexFace5(
                    x + 1,
                    y,
                    z + 1,
                    1f,
                    2,
                    faceTextureSlot5,
                    textureSlotCount,
                    sideColor,
                    ref vertices6.Array[count11 + 2]
                );
                SetupCubeVertexFace5(
                    x,
                    y,
                    z + 1,
                    1f,
                    3,
                    faceTextureSlot5,
                    textureSlotCount,
                    sideColor,
                    ref vertices6.Array[count11 + 3]
                );
                int count12 = indices6.Count;
                indices6.Count += 6;
                indices6.Array[count12] = count11;
                indices6.Array[count12 + 1] = count11 + 2;
                indices6.Array[count12 + 2] = count11 + 1;
                indices6.Array[count12 + 3] = count11 + 2;
                indices6.Array[count12 + 4] = count11;
                indices6.Array[count12 + 5] = count11 + 3;
            }
        }

        public virtual void GenerateCubeVertices(Block block,
            int value,
            int x,
            int y,
            int z,
            int rotationX,
            int rotationY,
            int rotationZ,
            Color color,
            TerrainGeometrySubset[] subsetsByFace) {
            int blockIndex = block.BlockIndex;
            TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
            TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z + 1);
            TerrainChunk chunkAtCell3 = Terrain.GetChunkAtCell(x + 1, z);
            TerrainChunk chunkAtCell4 = Terrain.GetChunkAtCell(x, z - 1);
            TerrainChunk chunkAtCell5 = Terrain.GetChunkAtCell(x - 1, z);
            int cellValueFast = chunkAtCell2.GetCellValueFast(x & 0xF, y, (z + 1) & 0xF);
            int textureSlotCount = block.GetTextureSlotCount(value);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    0,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices = subsetsByFace[0].Vertices;
                TerrainGeometryDynamicArray<int> indices = subsetsByFace[0].Indices;
                int faceTextureSlot = block.GetFaceTextureSlot(0, value);
                int count = vertices.Count;
                vertices.Count += 4;
                SetupCubeVertexFace0(
                    x,
                    y,
                    z + 1,
                    1f,
                    rotationZ,
                    faceTextureSlot,
                    textureSlotCount,
                    color,
                    ref vertices.Array[count]
                );
                SetupCubeVertexFace0(
                    x + 1,
                    y,
                    z + 1,
                    1f,
                    1 + rotationZ,
                    faceTextureSlot,
                    textureSlotCount,
                    color,
                    ref vertices.Array[count + 1]
                );
                SetupCubeVertexFace0(
                    x + 1,
                    y + 1,
                    z + 1,
                    1f,
                    2 + rotationZ,
                    faceTextureSlot,
                    textureSlotCount,
                    color,
                    ref vertices.Array[count + 2]
                );
                SetupCubeVertexFace0(
                    x,
                    y + 1,
                    z + 1,
                    1f,
                    3 + rotationZ,
                    faceTextureSlot,
                    textureSlotCount,
                    color,
                    ref vertices.Array[count + 3]
                );
                int count2 = indices.Count;
                indices.Count += 6;
                indices.Array[count2] = count;
                indices.Array[count2 + 1] = count + 2;
                indices.Array[count2 + 2] = count + 1;
                indices.Array[count2 + 3] = count + 2;
                indices.Array[count2 + 4] = count;
                indices.Array[count2 + 5] = count + 3;
            }
            cellValueFast = chunkAtCell3.GetCellValueFast((x + 1) & 0xF, y, z & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    1,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices2 = subsetsByFace[1].Vertices;
                TerrainGeometryDynamicArray<int> indices2 = subsetsByFace[1].Indices;
                int faceTextureSlot2 = block.GetFaceTextureSlot(1, value);
                int count3 = vertices2.Count;
                vertices2.Count += 4;
                SetupCubeVertexFace1(
                    x + 1,
                    y,
                    z,
                    1f,
                    1 + rotationX,
                    faceTextureSlot2,
                    textureSlotCount,
                    color,
                    ref vertices2.Array[count3]
                );
                SetupCubeVertexFace1(
                    x + 1,
                    y + 1,
                    z,
                    1f,
                    2 + rotationX,
                    faceTextureSlot2,
                    textureSlotCount,
                    color,
                    ref vertices2.Array[count3 + 1]
                );
                SetupCubeVertexFace1(
                    x + 1,
                    y + 1,
                    z + 1,
                    1f,
                    3 + rotationX,
                    faceTextureSlot2,
                    textureSlotCount,
                    color,
                    ref vertices2.Array[count3 + 2]
                );
                SetupCubeVertexFace1(
                    x + 1,
                    y,
                    z + 1,
                    1f,
                    rotationX,
                    faceTextureSlot2,
                    textureSlotCount,
                    color,
                    ref vertices2.Array[count3 + 3]
                );
                int count4 = indices2.Count;
                indices2.Count += 6;
                indices2.Array[count4] = count3;
                indices2.Array[count4 + 1] = count3 + 2;
                indices2.Array[count4 + 2] = count3 + 1;
                indices2.Array[count4 + 3] = count3 + 2;
                indices2.Array[count4 + 4] = count3;
                indices2.Array[count4 + 5] = count3 + 3;
            }
            cellValueFast = chunkAtCell4.GetCellValueFast(x & 0xF, y, (z - 1) & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    2,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices3 = subsetsByFace[2].Vertices;
                TerrainGeometryDynamicArray<int> indices3 = subsetsByFace[2].Indices;
                int faceTextureSlot3 = block.GetFaceTextureSlot(2, value);
                int count5 = vertices3.Count;
                vertices3.Count += 4;
                SetupCubeVertexFace2(
                    x,
                    y,
                    z,
                    1f,
                    1 + rotationZ,
                    faceTextureSlot3,
                    textureSlotCount,
                    color,
                    ref vertices3.Array[count5]
                );
                SetupCubeVertexFace2(
                    x + 1,
                    y,
                    z,
                    1f,
                    rotationZ,
                    faceTextureSlot3,
                    textureSlotCount,
                    color,
                    ref vertices3.Array[count5 + 1]
                );
                SetupCubeVertexFace2(
                    x + 1,
                    y + 1,
                    z,
                    1f,
                    3 + rotationZ,
                    faceTextureSlot3,
                    textureSlotCount,
                    color,
                    ref vertices3.Array[count5 + 2]
                );
                SetupCubeVertexFace2(
                    x,
                    y + 1,
                    z,
                    1f,
                    2 + rotationZ,
                    faceTextureSlot3,
                    textureSlotCount,
                    color,
                    ref vertices3.Array[count5 + 3]
                );
                int count6 = indices3.Count;
                indices3.Count += 6;
                indices3.Array[count6] = count5;
                indices3.Array[count6 + 1] = count5 + 1;
                indices3.Array[count6 + 2] = count5 + 2;
                indices3.Array[count6 + 3] = count5 + 2;
                indices3.Array[count6 + 4] = count5 + 3;
                indices3.Array[count6 + 5] = count5;
            }
            cellValueFast = chunkAtCell5.GetCellValueFast((x - 1) & 0xF, y, z & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    3,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices4 = subsetsByFace[3].Vertices;
                TerrainGeometryDynamicArray<int> indices4 = subsetsByFace[3].Indices;
                int faceTextureSlot4 = block.GetFaceTextureSlot(3, value);
                int count7 = vertices4.Count;
                vertices4.Count += 4;
                SetupCubeVertexFace3(
                    x,
                    y,
                    z,
                    1f,
                    rotationX,
                    faceTextureSlot4,
                    textureSlotCount,
                    color,
                    ref vertices4.Array[count7]
                );
                SetupCubeVertexFace3(
                    x,
                    y + 1,
                    z,
                    1f,
                    3 + rotationX,
                    faceTextureSlot4,
                    textureSlotCount,
                    color,
                    ref vertices4.Array[count7 + 1]
                );
                SetupCubeVertexFace3(
                    x,
                    y + 1,
                    z + 1,
                    1f,
                    2 + rotationX,
                    faceTextureSlot4,
                    textureSlotCount,
                    color,
                    ref vertices4.Array[count7 + 2]
                );
                SetupCubeVertexFace3(
                    x,
                    y,
                    z + 1,
                    1f,
                    1 + rotationX,
                    faceTextureSlot4,
                    textureSlotCount,
                    color,
                    ref vertices4.Array[count7 + 3]
                );
                int count8 = indices4.Count;
                indices4.Count += 6;
                indices4.Array[count8] = count7;
                indices4.Array[count8 + 1] = count7 + 1;
                indices4.Array[count8 + 2] = count7 + 2;
                indices4.Array[count8 + 3] = count7 + 2;
                indices4.Array[count8 + 4] = count7 + 3;
                indices4.Array[count8 + 5] = count7;
            }
            cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y + 1, z & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    4,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices5 = subsetsByFace[4].Vertices;
                TerrainGeometryDynamicArray<int> indices5 = subsetsByFace[4].Indices;
                int faceTextureSlot5 = block.GetFaceTextureSlot(4, value);
                int count9 = vertices5.Count;
                vertices5.Count += 4;
                SetupCubeVertexFace4(
                    x,
                    y + 1,
                    z,
                    1f,
                    3 + rotationY,
                    faceTextureSlot5,
                    textureSlotCount,
                    color,
                    ref vertices5.Array[count9]
                );
                SetupCubeVertexFace4(
                    x + 1,
                    y + 1,
                    z,
                    1f,
                    2 + rotationY,
                    faceTextureSlot5,
                    textureSlotCount,
                    color,
                    ref vertices5.Array[count9 + 1]
                );
                SetupCubeVertexFace4(
                    x + 1,
                    y + 1,
                    z + 1,
                    1f,
                    1 + rotationY,
                    faceTextureSlot5,
                    textureSlotCount,
                    color,
                    ref vertices5.Array[count9 + 2]
                );
                SetupCubeVertexFace4(
                    x,
                    y + 1,
                    z + 1,
                    1f,
                    rotationY,
                    faceTextureSlot5,
                    textureSlotCount,
                    color,
                    ref vertices5.Array[count9 + 3]
                );
                int count10 = indices5.Count;
                indices5.Count += 6;
                indices5.Array[count10] = count9;
                indices5.Array[count10 + 1] = count9 + 1;
                indices5.Array[count10 + 2] = count9 + 2;
                indices5.Array[count10 + 3] = count9 + 2;
                indices5.Array[count10 + 4] = count9 + 3;
                indices5.Array[count10 + 5] = count9;
            }
            cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y - 1, z & 0xF);
            if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex)
                && block.ShouldGenerateFace(
                    SubsystemTerrain,
                    5,
                    value,
                    cellValueFast,
                    x,
                    y,
                    z
                )) {
                DynamicArray<TerrainVertex> vertices6 = subsetsByFace[5].Vertices;
                TerrainGeometryDynamicArray<int> indices6 = subsetsByFace[5].Indices;
                int faceTextureSlot6 = block.GetFaceTextureSlot(5, value);
                int count11 = vertices6.Count;
                vertices6.Count += 4;
                SetupCubeVertexFace5(
                    x,
                    y,
                    z,
                    1f,
                    rotationY,
                    faceTextureSlot6,
                    textureSlotCount,
                    color,
                    ref vertices6.Array[count11]
                );
                SetupCubeVertexFace5(
                    x + 1,
                    y,
                    z,
                    1f,
                    1 + rotationY,
                    faceTextureSlot6,
                    textureSlotCount,
                    color,
                    ref vertices6.Array[count11 + 1]
                );
                SetupCubeVertexFace5(
                    x + 1,
                    y,
                    z + 1,
                    1f,
                    2 + rotationY,
                    faceTextureSlot6,
                    textureSlotCount,
                    color,
                    ref vertices6.Array[count11 + 2]
                );
                SetupCubeVertexFace5(
                    x,
                    y,
                    z + 1,
                    1f,
                    3 + rotationY,
                    faceTextureSlot6,
                    textureSlotCount,
                    color,
                    ref vertices6.Array[count11 + 3]
                );
                int count12 = indices6.Count;
                indices6.Count += 6;
                indices6.Array[count12] = count11;
                indices6.Array[count12 + 1] = count11 + 2;
                indices6.Array[count12 + 2] = count11 + 1;
                indices6.Array[count12 + 3] = count11 + 2;
                indices6.Array[count12 + 4] = count11;
                indices6.Array[count12 + 5] = count11 + 3;
            }
        }

        public virtual void GenerateFlatVertices(Block block,
            int value,
            int x,
            int y,
            int z,
            int rotation,
            Color color,
            TerrainGeometrySubset[] subsetsByFace) {
            DynamicArray<TerrainVertex> vertices = subsetsByFace[4].Vertices;
            TerrainGeometryDynamicArray<int> indices = subsetsByFace[4].Indices;
            int faceTextureSlot = block.GetFaceTextureSlot(4, value);
            int textureSlotCount = block.GetTextureSlotCount(value);
            int count = vertices.Count;
            vertices.Count += 4;
            SetupCubeVertexFace4(
                x,
                y + 1,
                z,
                0f,
                3 + rotation,
                faceTextureSlot,
                textureSlotCount,
                color,
                ref vertices.Array[count]
            );
            SetupCubeVertexFace4(
                x + 1,
                y + 1,
                z,
                0f,
                2 + rotation,
                faceTextureSlot,
                textureSlotCount,
                color,
                ref vertices.Array[count + 1]
            );
            SetupCubeVertexFace4(
                x + 1,
                y + 1,
                z + 1,
                0f,
                1 + rotation,
                faceTextureSlot,
                textureSlotCount,
                color,
                ref vertices.Array[count + 2]
            );
            SetupCubeVertexFace4(
                x,
                y + 1,
                z + 1,
                0f,
                rotation,
                faceTextureSlot,
                textureSlotCount,
                color,
                ref vertices.Array[count + 3]
            );
            int count2 = indices.Count;
            indices.Count += 6;
            indices.Array[count2] = count;
            indices.Array[count2 + 1] = count + 1;
            indices.Array[count2 + 2] = count + 2;
            indices.Array[count2 + 3] = count + 2;
            indices.Array[count2 + 4] = count + 3;
            indices.Array[count2 + 5] = count;
        }

        public virtual void GenerateMeshVertices(Block block,
            int x,
            int y,
            int z,
            BlockMesh blockMesh,
            Color color,
            Matrix? matrix,
            TerrainGeometrySubset subset) {
            DynamicArray<TerrainVertex> vertices = subset.Vertices;
            TerrainGeometryDynamicArray<int> indices = subset.Indices;
            int count = vertices.Count;
            int cellLightFast = Terrain.GetCellLightFast(x, y, z);
            float num = LightingManager.LightIntensityByLightValue[cellLightFast];
            vertices.Count += blockMesh.Vertices.Count;
            for (int i = 0; i < blockMesh.Vertices.Count; i++) {
                BlockMeshVertex blockMeshVertex = blockMesh.Vertices.Array[i];
                Vector3 vector = blockMeshVertex.Position;
                if (matrix.HasValue) {
                    vector = Vector3.Transform(blockMeshVertex.Position, matrix.Value);
                }
                Color color2;
                if (blockMeshVertex.IsEmissive) {
                    color2 = new Color(
                        (byte)(color.R * blockMeshVertex.Color.R / 255),
                        (byte)(color.G * blockMeshVertex.Color.G / 255),
                        (byte)(color.B * blockMeshVertex.Color.B / 255)
                    );
                }
                else {
                    float num2 = num / 255f;
                    color2 = new Color(
                        (byte)(color.R * blockMeshVertex.Color.R * num2),
                        (byte)(color.G * blockMeshVertex.Color.G * num2),
                        (byte)(color.B * blockMeshVertex.Color.B * num2)
                    );
                }
                SetupVertex(
                    x + vector.X,
                    y + vector.Y,
                    z + vector.Z,
                    color2,
                    blockMeshVertex.TextureCoordinates.X,
                    blockMeshVertex.TextureCoordinates.Y,
                    ref vertices.Array[count + i]
                );
            }
            if (blockMesh.Sides != null) {
                for (int j = 0; j < 6; j++) {
                    Point3 point = CellFace.FaceToPoint3(j);
                    int cellValueFastChunkExists = Terrain.GetCellValueFastChunkExists(x + point.X, y + point.Y, z + point.Z);
                    m_visibleSides[j] = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFastChunkExists)]
                        .IsFaceTransparent(SubsystemTerrain, CellFace.OppositeFace(j), cellValueFastChunkExists);
                }
                for (int k = 0; k < blockMesh.Indices.Count / 3; k++) {
                    int num3 = blockMesh.Sides == null ? -1 : blockMesh.Sides.Array[k];
                    if (num3 < 0
                        || m_visibleSides[num3]) {
                        indices.Add(blockMesh.Indices.Array[3 * k] + count);
                        indices.Add(blockMesh.Indices.Array[3 * k + 1] + count);
                        indices.Add(blockMesh.Indices.Array[3 * k + 2] + count);
                    }
                }
            }
            else {
                for (int l = 0; l < blockMesh.Indices.Count; l++) {
                    indices.Add(blockMesh.Indices.Array[l] + count);
                }
            }
        }

        public virtual void GenerateShadedMeshVertices(Block block,
            int x,
            int y,
            int z,
            BlockMesh blockMesh,
            Color color,
            Matrix? matrix,
            int[] facesMap,
            TerrainGeometrySubset subset) {
            CalculateCornerLights(x, y, z);
            DynamicArray<TerrainVertex> vertices = subset.Vertices;
            TerrainGeometryDynamicArray<int> indices = subset.Indices;
            int count = vertices.Count;
            vertices.Count += blockMesh.Vertices.Count;
            for (int i = 0; i < blockMesh.Vertices.Count; i++) {
                BlockMeshVertex blockMeshVertex = blockMesh.Vertices.Array[i];
                Vector3 vector = blockMeshVertex.Position;
                if (matrix.HasValue) {
                    vector = Vector3.Transform(vector, matrix.Value);
                }
                Color color2;
                if (blockMeshVertex.IsEmissive) {
                    color2 = new Color(
                        (byte)(color.R * blockMeshVertex.Color.R / 255),
                        (byte)(color.G * blockMeshVertex.Color.G / 255),
                        (byte)(color.B * blockMeshVertex.Color.B / 255)
                    );
                }
                else {
                    int face = facesMap != null ? facesMap[blockMeshVertex.Face] : blockMeshVertex.Face;
                    float num = InterpolateCornerLights(face, vector) / 255f;
                    color2 = new Color(
                        (byte)(color.R * blockMeshVertex.Color.R * num),
                        (byte)(color.G * blockMeshVertex.Color.G * num),
                        (byte)(color.B * blockMeshVertex.Color.B * num)
                    );
                }
                SetupVertex(
                    x + vector.X,
                    y + vector.Y,
                    z + vector.Z,
                    color2,
                    blockMeshVertex.TextureCoordinates.X,
                    blockMeshVertex.TextureCoordinates.Y,
                    ref vertices.Array[count + i]
                );
            }
            if (blockMesh.Sides != null) {
                for (int j = 0; j < 6; j++) {
                    Point3 point = CellFace.FaceToPoint3(j);
                    int cellValueFastChunkExists = Terrain.GetCellValueFastChunkExists(x + point.X, y + point.Y, z + point.Z);
                    m_visibleSides[j] = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFastChunkExists)]
                        .IsFaceTransparent(SubsystemTerrain, CellFace.OppositeFace(j), cellValueFastChunkExists);
                }
                for (int k = 0; k < blockMesh.Indices.Count / 3; k++) {
                    int num2 = blockMesh.Sides == null ? -1 : blockMesh.Sides.Array[k];
                    if (num2 < 0
                        || m_visibleSides[facesMap != null ? facesMap[num2] : num2]) {
                        indices.Add(blockMesh.Indices.Array[3 * k] + count);
                        indices.Add(blockMesh.Indices.Array[3 * k + 1] + count);
                        indices.Add(blockMesh.Indices.Array[3 * k + 2] + count);
                    }
                }
            }
            else {
                for (int l = 0; l < blockMesh.Indices.Count; l++) {
                    indices.Add(blockMesh.Indices.Array[l] + count);
                }
            }
        }

        public virtual void GenerateWireVertices(int value,
            int x,
            int y,
            int z,
            int mountingFace,
            float centerBoxSize,
            Vector2 centerOffset,
            TerrainGeometrySubset subset) {
            if (SubsystemElectricity == null) {
                return;
            }
            Color color = WireBlock.WireColor;
            int num = Terrain.ExtractContents(value);
            if (num == 133) {
                int? color2 = WireBlock.GetColor(Terrain.ExtractData(value));
                if (color2.HasValue) {
                    color = SubsystemPalette.GetColor(this, color2);
                }
            }
            int num2 = Terrain.ExtractLight(value);
            float num3 = LightingManager.LightIntensityByLightValue[num2];
            Vector3 v = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) - 0.5f * CellFace.FaceToVector3(mountingFace);
            Vector3 vector = CellFace.FaceToVector3(mountingFace);
            Vector2 v2 = new(0.9376f, 0.0001f);
            Vector2 v3 = new(0.03125f, 0.00550781237f);
            Point3 point = CellFace.FaceToPoint3(mountingFace);
            int cellContents = Terrain.GetCellContents(x - point.X, y - point.Y, z - point.Z);
            bool flag = cellContents == 2 || cellContents == 7 || cellContents == 8 || cellContents == 6 || cellContents == 62 || cellContents == 72;
            Vector3 v4 = CellFace.FaceToVector3(SubsystemElectricity.GetConnectorFace(mountingFace, ElectricConnectorDirection.Top));
            Vector3 vector2 = CellFace.FaceToVector3(SubsystemElectricity.GetConnectorFace(mountingFace, ElectricConnectorDirection.Left))
                * centerOffset.X
                + v4 * centerOffset.Y;
            int num4 = 0;
            m_tmpConnectionPaths.Clear();
            SubsystemElectricity.GetAllConnectedNeighbors(x, y, z, mountingFace, m_tmpConnectionPaths);
            foreach (ElectricConnectionPath tmpConnectionPath in m_tmpConnectionPaths) {
                if ((num4 & (1 << tmpConnectionPath.ConnectorFace)) == 0) {
                    ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(
                        mountingFace,
                        0,
                        tmpConnectionPath.ConnectorFace
                    );
                    if (!(centerOffset == Vector2.Zero)
                        || connectorDirection != ElectricConnectorDirection.In) {
                        num4 |= 1 << tmpConnectionPath.ConnectorFace;
                        Color color3 = color;
                        if (num != 133) {
                            int cellValue = Terrain.GetCellValue(
                                x + tmpConnectionPath.NeighborOffsetX,
                                y + tmpConnectionPath.NeighborOffsetY,
                                z + tmpConnectionPath.NeighborOffsetZ
                            );
                            if (Terrain.ExtractContents(cellValue) == 133) {
                                int? color4 = WireBlock.GetColor(Terrain.ExtractData(cellValue));
                                if (color4.HasValue) {
                                    color3 = SubsystemPalette.GetColor(this, color4);
                                }
                            }
                        }
                        Vector3 vector3 = connectorDirection != ElectricConnectorDirection.In
                            ? CellFace.FaceToVector3(tmpConnectionPath.ConnectorFace)
                            : -Vector3.Normalize(vector2);
                        Vector3 vector4 = Vector3.Cross(vector, vector3);
                        float s = centerBoxSize >= 0f ? MathUtils.Max(0.03125f, centerBoxSize / 2f) : centerBoxSize / 2f;
                        float num5 = connectorDirection == ElectricConnectorDirection.In ? 0.03125f : 0.5f;
                        float num6 = connectorDirection == ElectricConnectorDirection.In ? 0f :
                            tmpConnectionPath.ConnectorFace == tmpConnectionPath.NeighborFace ? num5 + 0.03125f :
                            tmpConnectionPath.ConnectorFace != CellFace.OppositeFace(tmpConnectionPath.NeighborFace) ? num5 : num5 - 0.03125f;
                        Vector3 v5 = v - vector4 * 0.03125f + vector3 * s + vector2;
                        Vector3 vector5 = v - vector4 * 0.03125f + vector3 * num5;
                        Vector3 vector6 = v + vector4 * 0.03125f + vector3 * num5;
                        Vector3 v6 = v + vector4 * 0.03125f + vector3 * s + vector2;
                        Vector3 vector7 = v + vector * 0.03125f + vector3 * (centerBoxSize / 2f) + vector2;
                        Vector3 vector8 = v + vector * 0.03125f + vector3 * num6;
                        if (flag && centerBoxSize == 0f) {
                            Vector3 vector9 = 0.25f * GetRandomWireOffset(0.5f * (v5 + v6), vector);
                            v5 += vector9;
                            v6 += vector9;
                            vector7 += vector9;
                        }
                        Vector2 vector10 = v2 + v3 * new Vector2(MathUtils.Max(0.0625f, centerBoxSize), 0f);
                        Vector2 vector11 = v2 + v3 * new Vector2(num5 * 2f, 0f);
                        Vector2 vector12 = v2 + v3 * new Vector2(num5 * 2f, 1f);
                        Vector2 vector13 = v2 + v3 * new Vector2(MathUtils.Max(0.0625f, centerBoxSize), 1f);
                        Vector2 vector14 = v2 + v3 * new Vector2(centerBoxSize, 0.5f);
                        Vector2 vector15 = v2 + v3 * new Vector2(num6 * 2f, 0.5f);
                        int num7 = Terrain.ExtractLight(
                            Terrain.GetCellValue(
                                x + tmpConnectionPath.NeighborOffsetX,
                                y + tmpConnectionPath.NeighborOffsetY,
                                z + tmpConnectionPath.NeighborOffsetZ
                            )
                        );
                        float num8 = LightingManager.LightIntensityByLightValue[num7];
                        float num9 = 0.5f * (num3 + num8);
                        float num10 = LightingManager.CalculateLighting(-vector4);
                        float num11 = LightingManager.CalculateLighting(vector4);
                        float num12 = LightingManager.CalculateLighting(vector);
                        float num13 = num10 * num3;
                        float num14 = num10 * num9;
                        float num15 = num11 * num9;
                        float num16 = num11 * num3;
                        float num17 = num12 * num3;
                        float num18 = num12 * num9;
                        Color color5 = new((byte)(color3.R * num13), (byte)(color3.G * num13), (byte)(color3.B * num13));
                        Color color6 = new((byte)(color3.R * num14), (byte)(color3.G * num14), (byte)(color3.B * num14));
                        Color color7 = new((byte)(color3.R * num15), (byte)(color3.G * num15), (byte)(color3.B * num15));
                        Color color8 = new((byte)(color3.R * num16), (byte)(color3.G * num16), (byte)(color3.B * num16));
                        Color color9 = new((byte)(color3.R * num17), (byte)(color3.G * num17), (byte)(color3.B * num17));
                        Color color10 = new((byte)(color3.R * num18), (byte)(color3.G * num18), (byte)(color3.B * num18));
                        int count = subset.Vertices.Count;
                        subset.Vertices.Count += 6;
                        TerrainVertex[] array = subset.Vertices.Array;
                        SetupVertex(
                            v5.X,
                            v5.Y,
                            v5.Z,
                            color5,
                            vector10.X,
                            vector10.Y,
                            ref array[count]
                        );
                        SetupVertex(
                            vector5.X,
                            vector5.Y,
                            vector5.Z,
                            color6,
                            vector11.X,
                            vector11.Y,
                            ref array[count + 1]
                        );
                        SetupVertex(
                            vector6.X,
                            vector6.Y,
                            vector6.Z,
                            color7,
                            vector12.X,
                            vector12.Y,
                            ref array[count + 2]
                        );
                        SetupVertex(
                            v6.X,
                            v6.Y,
                            v6.Z,
                            color8,
                            vector13.X,
                            vector13.Y,
                            ref array[count + 3]
                        );
                        SetupVertex(
                            vector7.X,
                            vector7.Y,
                            vector7.Z,
                            color9,
                            vector14.X,
                            vector14.Y,
                            ref array[count + 4]
                        );
                        SetupVertex(
                            vector8.X,
                            vector8.Y,
                            vector8.Z,
                            color10,
                            vector15.X,
                            vector15.Y,
                            ref array[count + 5]
                        );
                        int count2 = subset.Indices.Count;
                        subset.Indices.Count += connectorDirection == ElectricConnectorDirection.In ? 15 : 12;
                        int[] array2 = subset.Indices.Array;
                        array2[count2] = count;
                        array2[count2 + 1] = count + 5;
                        array2[count2 + 2] = count + 1;
                        array2[count2 + 3] = count + 5;
                        array2[count2 + 4] = count;
                        array2[count2 + 5] = count + 4;
                        array2[count2 + 6] = count + 4;
                        array2[count2 + 7] = count + 2;
                        array2[count2 + 8] = count + 5;
                        array2[count2 + 9] = count + 2;
                        array2[count2 + 10] = count + 4;
                        array2[count2 + 11] = count + 3;
                        if (connectorDirection == ElectricConnectorDirection.In) {
                            array2[count2 + 12] = count + 2;
                            array2[count2 + 13] = count + 1;
                            array2[count2 + 14] = count + 5;
                        }
                    }
                }
            }
            if (centerBoxSize != 0f
                || (num4 == 0 && num != 133)) {
                return;
            }
            for (int i = 0; i < 6; i++) {
                if (i != mountingFace
                    && i != CellFace.OppositeFace(mountingFace)
                    && (num4 & (1 << i)) == 0) {
                    Vector3 vector16 = CellFace.FaceToVector3(i);
                    Vector3 v7 = Vector3.Cross(vector, vector16);
                    Vector3 v8 = v - v7 * 0.03125f + vector16 * 0.03125f;
                    Vector3 v9 = v + v7 * 0.03125f + vector16 * 0.03125f;
                    Vector3 vector17 = v + vector * 0.03125f;
                    if (flag) {
                        Vector3 vector18 = 0.25f * GetRandomWireOffset(0.5f * (v8 + v9), vector);
                        v8 += vector18;
                        v9 += vector18;
                        vector17 += vector18;
                    }
                    Vector2 vector19 = v2 + v3 * new Vector2(0.0625f, 0f);
                    Vector2 vector20 = v2 + v3 * new Vector2(0.0625f, 1f);
                    Vector2 vector21 = v2 + v3 * new Vector2(0f, 0.5f);
                    float num19 = LightingManager.CalculateLighting(vector16) * num3;
                    float num20 = LightingManager.CalculateLighting(vector) * num3;
                    Color color11 = new((byte)(color.R * num19), (byte)(color.G * num19), (byte)(color.B * num19));
                    Color color12 = new((byte)(color.R * num20), (byte)(color.G * num20), (byte)(color.B * num20));
                    int count3 = subset.Vertices.Count;
                    subset.Vertices.Count += 3;
                    TerrainVertex[] array3 = subset.Vertices.Array;
                    SetupVertex(
                        v8.X,
                        v8.Y,
                        v8.Z,
                        color11,
                        vector19.X,
                        vector19.Y,
                        ref array3[count3]
                    );
                    SetupVertex(
                        v9.X,
                        v9.Y,
                        v9.Z,
                        color11,
                        vector20.X,
                        vector20.Y,
                        ref array3[count3 + 1]
                    );
                    SetupVertex(
                        vector17.X,
                        vector17.Y,
                        vector17.Z,
                        color12,
                        vector21.X,
                        vector21.Y,
                        ref array3[count3 + 2]
                    );
                    int count4 = subset.Indices.Count;
                    subset.Indices.Count += 3;
                    int[] array4 = subset.Indices.Array;
                    array4[count4] = count3;
                    array4[count4 + 1] = count3 + 2;
                    array4[count4 + 2] = count3 + 1;
                }
            }
        }

        public static void CalculateCubeVertexLight(int value, ref int light, ref int shadow) {
            int num = Terrain.ExtractContents(value);
            if (num == 0) {
                light = Math.Max(light, Terrain.ExtractLight(value));
                return;
            }
            light = Math.Max(light, Terrain.ExtractLight(value));
            shadow += BlocksManager.Blocks[num].GetShadowStrength(value);
        }

        public static int CombineLightAndShadow(int light, int shadow) => MathUtils.Max(light - MathUtils.Max(shadow / 7, 0), 0);

        public virtual int CalculateVertexLightFace0(int x, int y, int z) {
            int light = 0;
            int shadow = 0;
            TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x - 1, z);
            int num = TerrainChunk.CalculateCellIndex((x - 1) & 0xF, y, z & 0xF);
            int cellValueFast = chunkAtCell.GetCellValueFast(num - 1);
            int cellValueFast2 = chunkAtCell.GetCellValueFast(num);
            CalculateCubeVertexLight(cellValueFast, ref light, ref shadow);
            CalculateCubeVertexLight(cellValueFast2, ref light, ref shadow);
            TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z);
            int num2 = TerrainChunk.CalculateCellIndex(x & 0xF, y, z & 0xF);
            int cellValueFast3 = chunkAtCell2.GetCellValueFast(num2 - 1);
            int cellValueFast4 = chunkAtCell2.GetCellValueFast(num2);
            CalculateCubeVertexLight(cellValueFast3, ref light, ref shadow);
            CalculateCubeVertexLight(cellValueFast4, ref light, ref shadow);
            return CombineLightAndShadow(light, shadow);
        }

        public virtual int CalculateVertexLightFace1(int x, int y, int z) {
            int light = 0;
            int shadow = 0;
            TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z - 1);
            int num = TerrainChunk.CalculateCellIndex(x & 0xF, y, (z - 1) & 0xF);
            int cellValueFast = chunkAtCell.GetCellValueFast(num - 1);
            int cellValueFast2 = chunkAtCell.GetCellValueFast(num);
            CalculateCubeVertexLight(cellValueFast, ref light, ref shadow);
            CalculateCubeVertexLight(cellValueFast2, ref light, ref shadow);
            TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z);
            int num2 = TerrainChunk.CalculateCellIndex(x & 0xF, y, z & 0xF);
            int cellValueFast3 = chunkAtCell2.GetCellValueFast(num2 - 1);
            int cellValueFast4 = chunkAtCell2.GetCellValueFast(num2);
            CalculateCubeVertexLight(cellValueFast3, ref light, ref shadow);
            CalculateCubeVertexLight(cellValueFast4, ref light, ref shadow);
            return CombineLightAndShadow(light, shadow);
        }

        public virtual int CalculateVertexLightFace2(int x, int y, int z) {
            int light = 0;
            int shadow = 0;
            TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x - 1, z - 1);
            int num = TerrainChunk.CalculateCellIndex((x - 1) & 0xF, y, (z - 1) & 0xF);
            int cellValueFast = chunkAtCell.GetCellValueFast(num - 1);
            int cellValueFast2 = chunkAtCell.GetCellValueFast(num);
            CalculateCubeVertexLight(cellValueFast, ref light, ref shadow);
            CalculateCubeVertexLight(cellValueFast2, ref light, ref shadow);
            TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z - 1);
            int num2 = TerrainChunk.CalculateCellIndex(x & 0xF, y, (z - 1) & 0xF);
            int cellValueFast3 = chunkAtCell2.GetCellValueFast(num2 - 1);
            int cellValueFast4 = chunkAtCell2.GetCellValueFast(num2);
            CalculateCubeVertexLight(cellValueFast3, ref light, ref shadow);
            CalculateCubeVertexLight(cellValueFast4, ref light, ref shadow);
            return CombineLightAndShadow(light, shadow);
        }

        public virtual int CalculateVertexLightFace3(int x, int y, int z) {
            int light = 0;
            int shadow = 0;
            TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x - 1, z - 1);
            int num = TerrainChunk.CalculateCellIndex((x - 1) & 0xF, y, (z - 1) & 0xF);
            int cellValueFast = chunkAtCell.GetCellValueFast(num - 1);
            int cellValueFast2 = chunkAtCell.GetCellValueFast(num);
            CalculateCubeVertexLight(cellValueFast, ref light, ref shadow);
            CalculateCubeVertexLight(cellValueFast2, ref light, ref shadow);
            TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x - 1, z);
            int num2 = TerrainChunk.CalculateCellIndex((x - 1) & 0xF, y, z & 0xF);
            int cellValueFast3 = chunkAtCell2.GetCellValueFast(num2 - 1);
            int cellValueFast4 = chunkAtCell2.GetCellValueFast(num2);
            CalculateCubeVertexLight(cellValueFast3, ref light, ref shadow);
            CalculateCubeVertexLight(cellValueFast4, ref light, ref shadow);
            return CombineLightAndShadow(light, shadow);
        }

        public virtual int CalculateVertexLightFace4(int x, int y, int z) {
            int light = 0;
            int shadow = 0;
            CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x - 1, y, z - 1), ref light, ref shadow);
            CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x, y, z - 1), ref light, ref shadow);
            CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x - 1, y, z), ref light, ref shadow);
            CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x, y, z), ref light, ref shadow);
            return CombineLightAndShadow(light, shadow);
        }

        public virtual int CalculateVertexLightFace5(int x, int y, int z) {
            int light = 0;
            int shadow = 0;
            CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x - 1, y - 1, z - 1), ref light, ref shadow);
            CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x, y - 1, z - 1), ref light, ref shadow);
            CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x - 1, y - 1, z), ref light, ref shadow);
            CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x, y - 1, z), ref light, ref shadow);
            return CombineLightAndShadow(light, shadow);
        }

        public virtual void SetupCubeVertexFace0(int x,
            int y,
            int z,
            float height,
            int corner,
            int textureSlot,
            int textureSlotCount,
            Color color,
            ref TerrainVertex vertex) {
            float y2 = y + height - 1f;
            int light = CalculateVertexLightFace0(x, y, z);
            SetupCornerVertex(
                x,
                y2,
                z,
                color,
                light,
                0,
                textureSlot,
                textureSlotCount,
                corner,
                ref vertex
            );
        }

        public virtual void SetupCubeVertexFace1(int x,
            int y,
            int z,
            float height,
            int corner,
            int textureSlot,
            int textureSlotCount,
            Color color,
            ref TerrainVertex vertex) {
            float y2 = y + height - 1f;
            int light = CalculateVertexLightFace1(x, y, z);
            SetupCornerVertex(
                x,
                y2,
                z,
                color,
                light,
                1,
                textureSlot,
                textureSlotCount,
                corner,
                ref vertex
            );
        }

        public virtual void SetupCubeVertexFace2(int x,
            int y,
            int z,
            float height,
            int corner,
            int textureSlot,
            int textureSlotCount,
            Color color,
            ref TerrainVertex vertex) {
            float y2 = y + height - 1f;
            int light = CalculateVertexLightFace2(x, y, z);
            SetupCornerVertex(
                x,
                y2,
                z,
                color,
                light,
                2,
                textureSlot,
                textureSlotCount,
                corner,
                ref vertex
            );
        }

        public virtual void SetupCubeVertexFace3(int x,
            int y,
            int z,
            float height,
            int corner,
            int textureSlot,
            int textureSlotCount,
            Color color,
            ref TerrainVertex vertex) {
            float y2 = y + height - 1f;
            int light = CalculateVertexLightFace3(x, y, z);
            SetupCornerVertex(
                x,
                y2,
                z,
                color,
                light,
                3,
                textureSlot,
                textureSlotCount,
                corner,
                ref vertex
            );
        }

        public virtual void SetupCubeVertexFace4(int x,
            int y,
            int z,
            float height,
            int corner,
            int textureSlot,
            int textureSlotCount,
            Color color,
            ref TerrainVertex vertex) {
            float y2 = y + height - 1f;
            int light = CalculateVertexLightFace4(x, y, z);
            SetupCornerVertex(
                x,
                y2,
                z,
                color,
                light,
                4,
                textureSlot,
                textureSlotCount,
                corner,
                ref vertex
            );
        }

        public virtual void SetupCubeVertexFace5(int x,
            int y,
            int z,
            float height,
            int corner,
            int textureSlot,
            int textureSlotCount,
            Color color,
            ref TerrainVertex vertex) {
            float y2 = y + height - 1f;
            int light = CalculateVertexLightFace5(x, y, z);
            SetupCornerVertex(
                x,
                y2,
                z,
                color,
                light,
                5,
                textureSlot,
                textureSlotCount,
                corner,
                ref vertex
            );
        }

        public static Vector3 GetRandomWireOffset(Vector3 position, Vector3 normal) {
            int hashCode = Vector3.Round(2f * position).GetHashCode();
            Vector3 result = default;
            result.X = normal.X == 0f ? (float)(double)(MathUtils.Hash((uint)hashCode) % 255u) / 255f - 0.5f : 0f;
            result.Y = normal.Y == 0f ? (float)(double)(MathUtils.Hash((uint)(hashCode + 1)) % 255u) / 255f - 0.5f : 0f;
            result.Z = normal.Z == 0f ? (float)(double)(MathUtils.Hash((uint)(hashCode + 2)) % 255u) / 255f - 0.5f : 0f;
            return result;
        }

        public virtual void CalculateCornerLights(int x, int y, int z) {
            if (!(m_cornerLightsPosition == new Point3(x, y, z))) {
                m_cornerLightsPosition = new Point3(x, y, z);
                m_cornerLightsByFace[0].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x, y, z)];
                m_cornerLightsByFace[0].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x, y, z + 1)];
                m_cornerLightsByFace[0].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x, y + 1, z)];
                m_cornerLightsByFace[0].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x, y + 1, z + 1)];
                m_cornerLightsByFace[0].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x + 1, y, z)];
                m_cornerLightsByFace[0].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x + 1, y, z + 1)];
                m_cornerLightsByFace[0].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x + 1, y + 1, z)];
                m_cornerLightsByFace[0].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x + 1, y + 1, z + 1)];
                m_cornerLightsByFace[1].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x, y, z)];
                m_cornerLightsByFace[1].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x, y, z + 1)];
                m_cornerLightsByFace[1].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x, y + 1, z)];
                m_cornerLightsByFace[1].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x, y + 1, z + 1)];
                m_cornerLightsByFace[1].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x + 1, y, z)];
                m_cornerLightsByFace[1].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x + 1, y, z + 1)];
                m_cornerLightsByFace[1].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x + 1, y + 1, z)];
                m_cornerLightsByFace[1].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x + 1, y + 1, z + 1)];
                m_cornerLightsByFace[2].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x, y, z)];
                m_cornerLightsByFace[2].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x, y, z + 1)];
                m_cornerLightsByFace[2].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x, y + 1, z)];
                m_cornerLightsByFace[2].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x, y + 1, z + 1)];
                m_cornerLightsByFace[2].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x + 1, y, z)];
                m_cornerLightsByFace[2].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x + 1, y, z + 1)];
                m_cornerLightsByFace[2].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x + 1, y + 1, z)];
                m_cornerLightsByFace[2].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x + 1, y + 1, z + 1)];
                m_cornerLightsByFace[3].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x, y, z)];
                m_cornerLightsByFace[3].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x, y, z + 1)];
                m_cornerLightsByFace[3].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x, y + 1, z)];
                m_cornerLightsByFace[3].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x, y + 1, z + 1)];
                m_cornerLightsByFace[3].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x + 1, y, z)];
                m_cornerLightsByFace[3].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x + 1, y, z + 1)];
                m_cornerLightsByFace[3].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x + 1, y + 1, z)];
                m_cornerLightsByFace[3].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x + 1, y + 1, z + 1)];
                m_cornerLightsByFace[4].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x, y, z)];
                m_cornerLightsByFace[4].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x, y, z + 1)];
                m_cornerLightsByFace[4].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x, y + 1, z)];
                m_cornerLightsByFace[4].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x, y + 1, z + 1)];
                m_cornerLightsByFace[4].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x + 1, y, z)];
                m_cornerLightsByFace[4].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x + 1, y, z + 1)];
                m_cornerLightsByFace[4].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x + 1, y + 1, z)];
                m_cornerLightsByFace[4].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x + 1, y + 1, z + 1)];
                m_cornerLightsByFace[5].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x, y, z)];
                m_cornerLightsByFace[5].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x, y, z + 1)];
                m_cornerLightsByFace[5].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x, y + 1, z)];
                m_cornerLightsByFace[5].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x, y + 1, z + 1)];
                m_cornerLightsByFace[5].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x + 1, y, z)];
                m_cornerLightsByFace[5].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x + 1, y, z + 1)];
                m_cornerLightsByFace[5].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x + 1, y + 1, z)];
                m_cornerLightsByFace[5].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x + 1, y + 1, z + 1)];
            }
        }

        public virtual float InterpolateCornerLights(int face, Vector3 position) {
            float x = position.X;
            float y = position.Y;
            float z = position.Z;
            float num = 1f - x;
            float num2 = 1f - y;
            float num3 = 1f - z;
            return m_cornerLightsByFace[face].L000 * num * num2 * num3
                + m_cornerLightsByFace[face].L001 * num * num2 * z
                + m_cornerLightsByFace[face].L010 * num * y * num3
                + m_cornerLightsByFace[face].L011 * num * y * z
                + m_cornerLightsByFace[face].L100 * x * num2 * num3
                + m_cornerLightsByFace[face].L101 * x * num2 * z
                + m_cornerLightsByFace[face].L110 * x * y * num3
                + m_cornerLightsByFace[face].L111 * x * y * z;
        }
    }
}