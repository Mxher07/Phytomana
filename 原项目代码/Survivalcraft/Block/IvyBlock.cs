using Engine;
using Engine.Graphics;

namespace Game {
    public class IvyBlock : Block {
        public static int Index = 197;

        public BoundingBox[][] m_boundingBoxes =
            new BoundingBox[]
                [] { [
                        new BoundingBox(new Vector3(0f, 0f, 0.9375f), new Vector3(1f, 1f, 1f))
                    ], [new BoundingBox(new Vector3(0.9375f, 0f, 0f), new Vector3(1f, 1f, 1f))], [
                        new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 0.0625f))
                    ], [new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(0.0625f, 1f, 1f))]
                };

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) {
            int face = GetFace(Terrain.ExtractData(value));
            if (face >= 0
                && face < 4) {
                return m_boundingBoxes[face];
            }
            return base.GetCustomCollisionBoxes(terrain, value);
        }

        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            BlockPlacementData result = default;
            if (raycastResult.CellFace.Face < 4) {
                result.CellFace = raycastResult.CellFace;
                result.Value = Terrain.MakeBlockValue(197, 0, SetFace(0, CellFace.OppositeFace(raycastResult.CellFace.Face)));
            }
            return result;
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            TerrainGeometrySubset subsetAlphaTest = texture == null ? geometry.SubsetAlphaTest : geometry.GetGeometry(texture).SubsetAlphaTest;
            DynamicArray<TerrainVertex> vertices = subsetAlphaTest.Vertices;
            TerrainGeometryDynamicArray<int> indices = subsetAlphaTest.Indices;
            int count = vertices.Count;
            int data = Terrain.ExtractData(value);
            int num = Terrain.ExtractLight(value);
            int face = GetFace(data);
            float s = LightingManager.LightIntensityByLightValueAndFace[num + 16 * CellFace.OppositeFace(face)];
            Color color = BlockColorsMap.Ivy.Lookup(generator.Terrain, x, y, z) * s;
            color.A = byte.MaxValue;
            switch (face) {
                case 0:
                    vertices.Count += 4;
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x,
                        y,
                        z + 1,
                        color,
                        DefaultTextureSlot,
                        0,
                        ref vertices.Array[count]
                    );
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x + 1,
                        y,
                        z + 1,
                        color,
                        DefaultTextureSlot,
                        1,
                        ref vertices.Array[count + 1]
                    );
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x + 1,
                        y + 1,
                        z + 1,
                        color,
                        DefaultTextureSlot,
                        2,
                        ref vertices.Array[count + 2]
                    );
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x,
                        y + 1,
                        z + 1,
                        color,
                        DefaultTextureSlot,
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
                    break;
                case 1:
                    vertices.Count += 4;
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x + 1,
                        y,
                        z,
                        color,
                        DefaultTextureSlot,
                        0,
                        ref vertices.Array[count]
                    );
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x + 1,
                        y + 1,
                        z,
                        color,
                        DefaultTextureSlot,
                        3,
                        ref vertices.Array[count + 1]
                    );
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x + 1,
                        y + 1,
                        z + 1,
                        color,
                        DefaultTextureSlot,
                        2,
                        ref vertices.Array[count + 2]
                    );
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x + 1,
                        y,
                        z + 1,
                        color,
                        DefaultTextureSlot,
                        1,
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
                    break;
                case 2:
                    vertices.Count += 4;
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x,
                        y,
                        z,
                        color,
                        DefaultTextureSlot,
                        0,
                        ref vertices.Array[count]
                    );
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x + 1,
                        y,
                        z,
                        color,
                        DefaultTextureSlot,
                        1,
                        ref vertices.Array[count + 1]
                    );
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x + 1,
                        y + 1,
                        z,
                        color,
                        DefaultTextureSlot,
                        2,
                        ref vertices.Array[count + 2]
                    );
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x,
                        y + 1,
                        z,
                        color,
                        DefaultTextureSlot,
                        3,
                        ref vertices.Array[count + 3]
                    );
                    indices.Add(count);
                    indices.Add(count + 2);
                    indices.Add(count + 1);
                    indices.Add(count + 1);
                    indices.Add(count + 2);
                    indices.Add(count);
                    indices.Add(count + 2);
                    indices.Add(count);
                    indices.Add(count + 3);
                    indices.Add(count + 3);
                    indices.Add(count);
                    indices.Add(count + 2);
                    break;
                case 3:
                    vertices.Count += 4;
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x,
                        y,
                        z,
                        color,
                        DefaultTextureSlot,
                        0,
                        ref vertices.Array[count]
                    );
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x,
                        y + 1,
                        z,
                        color,
                        DefaultTextureSlot,
                        3,
                        ref vertices.Array[count + 1]
                    );
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x,
                        y + 1,
                        z + 1,
                        color,
                        DefaultTextureSlot,
                        2,
                        ref vertices.Array[count + 2]
                    );
                    BlockGeometryGenerator.SetupLitCornerVertex(
                        x,
                        y,
                        z + 1,
                        color,
                        DefaultTextureSlot,
                        1,
                        ref vertices.Array[count + 3]
                    );
                    indices.Add(count);
                    indices.Add(count + 2);
                    indices.Add(count + 1);
                    indices.Add(count + 1);
                    indices.Add(count + 2);
                    indices.Add(count);
                    indices.Add(count + 2);
                    indices.Add(count);
                    indices.Add(count + 3);
                    indices.Add(count + 3);
                    indices.Add(count);
                    indices.Add(count + 2);
                    break;
            }
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            color *= BlockColorsMap.Ivy.Lookup(environmentData.Temperature, environmentData.Humidity);
            BlocksManager.DrawFlatOrImageExtrusionBlock(
                primitivesRenderer,
                value,
                size,
                ref matrix,
                GetDefaultTexture(value),
                color,
                false,
                environmentData
            );
        }

        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) {
            Color color = BlockColorsMap.Ivy.Lookup(
                subsystemTerrain.Terrain,
                Terrain.ToCell(position.X),
                Terrain.ToCell(position.Y),
                Terrain.ToCell(position.Z)
            );
            return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color, DefaultTextureSlot);
        }

        public static int GetFace(int data) => data & 3;

        public static int SetFace(int data, int face) => (data & -4) | (face & 3);

        public static bool IsGrowthStopCell(int x, int y, int z) => MathUtils.Hash((uint)(x + y * 451 + z * 77437)) % 5u == 0;
    }
}