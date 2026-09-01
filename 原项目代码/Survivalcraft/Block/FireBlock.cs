using Engine;
using Engine.Graphics;

namespace Game {
    public class FireBlock : Block {
        public static int Index = 104;

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            TerrainGeometrySubset[] alphaTestSubsetsByFace = texture == null ? geometry.AlphaTestSubsetsByFace : geometry.GetGeometry(texture).AlphaTestSubsetsByFace;
            int data = Terrain.ExtractData(value);
            int value2 = y + 1 < TerrainChunk.Height ? generator.Terrain.GetCellValueFast(x, y + 1, z) : 0;
            int num = Terrain.ExtractContents(value2);
            int data2 = Terrain.ExtractData(value2);
            int value3 = y + 2 < TerrainChunk.Height ? generator.Terrain.GetCellValueFast(x, y + 2, z) : 0;
            int num2 = Terrain.ExtractContents(value3);
            int data3 = Terrain.ExtractData(value3);
            if (HasFireOnFace(data, 0)) {
                int value4 = y + 1 < TerrainChunk.Height ? generator.Terrain.GetCellValueFast(x, y + 1, z + 1) : 0;
                int num3 = Terrain.ExtractContents(value4);
                int data4 = Terrain.ExtractData(value4);
                int value5 = y + 2 < TerrainChunk.Height ? generator.Terrain.GetCellValueFast(x, y + 2, z + 1) : 0;
                int num4 = Terrain.ExtractContents(value5);
                int data5 = Terrain.ExtractData(value5);
                int num5 = DefaultTextureSlot;
                if ((num == 104 && HasFireOnFace(data2, 0))
                    || (num3 == 104 && HasFireOnFace(data4, 2))) {
                    num5 += 16;
                    if ((num2 == 104 && HasFireOnFace(data3, 0))
                        || (num4 == 104 && HasFireOnFace(data5, 2))) {
                        num5 += 16;
                    }
                }
                DynamicArray<TerrainVertex> vertices = alphaTestSubsetsByFace[0].Vertices;
                TerrainGeometryDynamicArray<int> indices = alphaTestSubsetsByFace[0].Indices;
                int count = vertices.Count;
                vertices.Count += 4;
                BlockGeometryGenerator.SetupLitCornerVertex(
                    x,
                    y,
                    z + 1,
                    Color.White,
                    num5,
                    0,
                    ref vertices.Array[count]
                );
                BlockGeometryGenerator.SetupLitCornerVertex(
                    x + 1,
                    y,
                    z + 1,
                    Color.White,
                    num5,
                    1,
                    ref vertices.Array[count + 1]
                );
                BlockGeometryGenerator.SetupLitCornerVertex(
                    x + 1,
                    y + 1,
                    z + 1,
                    Color.White,
                    num5,
                    2,
                    ref vertices.Array[count + 2]
                );
                BlockGeometryGenerator.SetupLitCornerVertex(
                    x,
                    y + 1,
                    z + 1,
                    Color.White,
                    num5,
                    3,
                    ref vertices.Array[count + 3]
                );
                indices.Add(count);
                indices.Add(count + 1);
                indices.Add(count + 2);
                indices.Add(count + 2);
                indices.Add(count + 1);
                indices.Add(count);
                indices.Add(count + 2);
                indices.Add(count + 3);
                indices.Add(count);
                indices.Add(count);
                indices.Add(count + 3);
                indices.Add(count + 2);
            }
            if (HasFireOnFace(data, 1)) {
                int value6 = y + 1 < TerrainChunk.Height ? generator.Terrain.GetCellValueFast(x + 1, y + 1, z) : 0;
                int num6 = Terrain.ExtractContents(value6);
                int data6 = Terrain.ExtractData(value6);
                int value7 = y + 2 < TerrainChunk.Height ? generator.Terrain.GetCellValueFast(x + 1, y + 2, z) : 0;
                int num7 = Terrain.ExtractContents(value7);
                int data7 = Terrain.ExtractData(value7);
                int num8 = DefaultTextureSlot;
                if ((num == 104 && HasFireOnFace(data2, 1))
                    || (num6 == 104 && HasFireOnFace(data6, 3))) {
                    num8 += 16;
                    if ((num2 == 104 && HasFireOnFace(data3, 1))
                        || (num7 == 104 && HasFireOnFace(data7, 3))) {
                        num8 += 16;
                    }
                }
                DynamicArray<TerrainVertex> vertices2 = alphaTestSubsetsByFace[1].Vertices;
                TerrainGeometryDynamicArray<int> indices2 = alphaTestSubsetsByFace[1].Indices;
                int count2 = vertices2.Count;
                vertices2.Count += 4;
                BlockGeometryGenerator.SetupLitCornerVertex(
                    x + 1,
                    y,
                    z,
                    Color.White,
                    num8,
                    0,
                    ref vertices2.Array[count2]
                );
                BlockGeometryGenerator.SetupLitCornerVertex(
                    x + 1,
                    y + 1,
                    z,
                    Color.White,
                    num8,
                    3,
                    ref vertices2.Array[count2 + 1]
                );
                BlockGeometryGenerator.SetupLitCornerVertex(
                    x + 1,
                    y + 1,
                    z + 1,
                    Color.White,
                    num8,
                    2,
                    ref vertices2.Array[count2 + 2]
                );
                BlockGeometryGenerator.SetupLitCornerVertex(
                    x + 1,
                    y,
                    z + 1,
                    Color.White,
                    num8,
                    1,
                    ref vertices2.Array[count2 + 3]
                );
                indices2.Add(count2);
                indices2.Add(count2 + 1);
                indices2.Add(count2 + 2);
                indices2.Add(count2 + 2);
                indices2.Add(count2 + 1);
                indices2.Add(count2);
                indices2.Add(count2 + 2);
                indices2.Add(count2 + 3);
                indices2.Add(count2);
                indices2.Add(count2);
                indices2.Add(count2 + 3);
                indices2.Add(count2 + 2);
            }
            if (HasFireOnFace(data, 2)) {
                int value8 = y + 1 < TerrainChunk.Height ? generator.Terrain.GetCellValueFast(x, y + 1, z - 1) : 0;
                int num9 = Terrain.ExtractContents(value8);
                int data8 = Terrain.ExtractData(value8);
                int value9 = y + 2 < TerrainChunk.Height ? generator.Terrain.GetCellValueFast(x, y + 2, z - 1) : 0;
                int num10 = Terrain.ExtractContents(value9);
                int data9 = Terrain.ExtractData(value9);
                int num11 = DefaultTextureSlot;
                if ((num == 104 && HasFireOnFace(data2, 2))
                    || (num9 == 104 && HasFireOnFace(data8, 0))) {
                    num11 += 16;
                    if ((num2 == 104 && HasFireOnFace(data3, 2))
                        || (num10 == 104 && HasFireOnFace(data9, 0))) {
                        num11 += 16;
                    }
                }
                DynamicArray<TerrainVertex> vertices3 = alphaTestSubsetsByFace[2].Vertices;
                TerrainGeometryDynamicArray<int> indices3 = alphaTestSubsetsByFace[2].Indices;
                int count3 = vertices3.Count;
                vertices3.Count += 4;
                BlockGeometryGenerator.SetupLitCornerVertex(
                    x,
                    y,
                    z,
                    Color.White,
                    num11,
                    0,
                    ref vertices3.Array[count3]
                );
                BlockGeometryGenerator.SetupLitCornerVertex(
                    x + 1,
                    y,
                    z,
                    Color.White,
                    num11,
                    1,
                    ref vertices3.Array[count3 + 1]
                );
                BlockGeometryGenerator.SetupLitCornerVertex(
                    x + 1,
                    y + 1,
                    z,
                    Color.White,
                    num11,
                    2,
                    ref vertices3.Array[count3 + 2]
                );
                BlockGeometryGenerator.SetupLitCornerVertex(
                    x,
                    y + 1,
                    z,
                    Color.White,
                    num11,
                    3,
                    ref vertices3.Array[count3 + 3]
                );
                indices3.Add(count3);
                indices3.Add(count3 + 2);
                indices3.Add(count3 + 1);
                indices3.Add(count3 + 1);
                indices3.Add(count3 + 2);
                indices3.Add(count3);
                indices3.Add(count3 + 2);
                indices3.Add(count3);
                indices3.Add(count3 + 3);
                indices3.Add(count3 + 3);
                indices3.Add(count3);
                indices3.Add(count3 + 2);
            }
            if (!HasFireOnFace(data, 3)) {
                return;
            }
            int value10 = y + 1 < TerrainChunk.Height ? generator.Terrain.GetCellValueFast(x - 1, y + 1, z) : 0;
            int num12 = Terrain.ExtractContents(value10);
            int data10 = Terrain.ExtractData(value10);
            int value11 = y + 2 < TerrainChunk.Height ? generator.Terrain.GetCellValueFast(x - 1, y + 2, z) : 0;
            int num13 = Terrain.ExtractContents(value11);
            int data11 = Terrain.ExtractData(value11);
            int num14 = DefaultTextureSlot;
            if ((num == 104 && HasFireOnFace(data2, 3))
                || (num12 == 104 && HasFireOnFace(data10, 1))) {
                num14 += 16;
                if ((num2 == 104 && HasFireOnFace(data3, 3))
                    || (num13 == 104 && HasFireOnFace(data11, 1))) {
                    num14 += 16;
                }
            }
            DynamicArray<TerrainVertex> vertices4 = alphaTestSubsetsByFace[3].Vertices;
            TerrainGeometryDynamicArray<int> indices4 = alphaTestSubsetsByFace[3].Indices;
            int count4 = vertices4.Count;
            vertices4.Count += 4;
            BlockGeometryGenerator.SetupLitCornerVertex(
                x,
                y,
                z,
                Color.White,
                num14,
                0,
                ref vertices4.Array[count4]
            );
            BlockGeometryGenerator.SetupLitCornerVertex(
                x,
                y + 1,
                z,
                Color.White,
                num14,
                3,
                ref vertices4.Array[count4 + 1]
            );
            BlockGeometryGenerator.SetupLitCornerVertex(
                x,
                y + 1,
                z + 1,
                Color.White,
                num14,
                2,
                ref vertices4.Array[count4 + 2]
            );
            BlockGeometryGenerator.SetupLitCornerVertex(
                x,
                y,
                z + 1,
                Color.White,
                num14,
                1,
                ref vertices4.Array[count4 + 3]
            );
            indices4.Add(count4);
            indices4.Add(count4 + 2);
            indices4.Add(count4 + 1);
            indices4.Add(count4 + 1);
            indices4.Add(count4 + 2);
            indices4.Add(count4);
            indices4.Add(count4 + 2);
            indices4.Add(count4);
            indices4.Add(count4 + 3);
            indices4.Add(count4 + 3);
            indices4.Add(count4);
            indices4.Add(count4 + 2);
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) { }

        public override bool ShouldAvoid(int value) => true;

        public static bool HasFireOnFace(int data, int face) {
            if (data == 0) {
                return true;
            }
            if ((data & (1 << face)) != 0) {
                return true;
            }
            return false;
        }
    }
}