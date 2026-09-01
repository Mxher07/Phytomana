using Engine;
using Engine.Graphics;

namespace Game {
    public class RyeBlock : CrossBlock {
        public static int Index = 174;

        public override IEnumerable<int> GetCreativeValues() {
            yield return Terrain.MakeBlockValue(174, 0, SetIsWild(SetSize(0, 7), true));
            yield return Terrain.MakeBlockValue(174, 0, SetIsWild(SetSize(0, 1), false));
            yield return Terrain.MakeBlockValue(174, 0, SetIsWild(SetSize(0, 3), false));
            yield return Terrain.MakeBlockValue(174, 0, SetIsWild(SetSize(0, 5), false));
            yield return Terrain.MakeBlockValue(174, 0, SetIsWild(SetSize(0, 7), false));
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            if (GetIsWild(Terrain.ExtractData(value))) {
                color *= BlockColorsMap.Grass.Lookup(environmentData.Temperature, environmentData.Humidity);
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
            else {
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
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            TerrainGeometrySubset subsetAlphaTest = texture == null ? geometry.SubsetAlphaTest : geometry.GetGeometry(texture).SubsetAlphaTest;
            if (GetIsWild(Terrain.ExtractData(value))) {
                Color color = BlockColorsMap.Grass.Lookup(generator.Terrain, x, y, z);
                generator.GenerateCrossfaceVertices(
                    this,
                    value,
                    x,
                    y,
                    z,
                    color,
                    GetFaceTextureSlot(0, value),
                    subsetAlphaTest
                );
            }
            else {
                generator.GenerateCrossfaceVertices(
                    this,
                    value,
                    x,
                    y,
                    z,
                    Color.White,
                    GetFaceTextureSlot(0, value),
                    subsetAlphaTest
                );
            }
        }

        public override int GetShadowStrength(int value) => GetSize(Terrain.ExtractData(value)) + 1;

        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) {
            if (GetIsWild(Terrain.ExtractData(value))) {
                Color color = BlockColorsMap.Grass.Lookup(
                    subsystemTerrain.Terrain,
                    Terrain.ToCell(position.X),
                    Terrain.ToCell(position.Y),
                    Terrain.ToCell(position.Z)
                );
                return new BlockDebrisParticleSystem(
                    subsystemTerrain,
                    position,
                    strength,
                    DestructionDebrisScale,
                    color,
                    GetFaceTextureSlot(4, value)
                );
            }
            return new BlockDebrisParticleSystem(
                subsystemTerrain,
                position,
                strength,
                DestructionDebrisScale,
                Color.White,
                GetFaceTextureSlot(4, value)
            );
        }

        public override int GetFaceTextureSlot(int face, int value) {
            int data = Terrain.ExtractData(value);
            int size = GetSize(data);
            if (GetIsWild(data)) {
                if (size > 2) {
                    return 87;
                }
                return 86;
            }
            return 88 + size;
        }

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            int num = 0;
            int data = 0;
            int data2 = Terrain.ExtractData(oldValue);
            int size = GetSize(data2);
            bool isWild = GetIsWild(data2);
            if (isWild) {
                num = size > 2 && Random.Float(0f, 1f) < 0.33f ? 1 : 0;
                data = 4;
            }
            else {
                switch (size) {
                    case 5:
                        num = 1;
                        data = 4;
                        break;
                    case 6:
                        num = Random.Int(1, 2);
                        data = 4;
                        break;
                    case 7:
                        num = Random.Int(1, 3);
                        data = 5;
                        break;
                }
            }
            showDebris = true;
            BlockDropValue item;
            for (int i = 0; i < num; i++) {
                item = new BlockDropValue { Value = Terrain.MakeBlockValue(173, 0, data), Count = 1 };
                dropValues.Add(item);
            }
            if (size == 7
                && !isWild
                && Random.Bool(0.5f)) {
                item = new BlockDropValue { Value = 248, Count = 1 };
                dropValues.Add(item);
            }
        }

        public static int GetSize(int data) => data & 7;

        public static int SetSize(int data, int size) {
            size = Math.Clamp(size, 0, 7);
            return (data & -8) | (size & 7);
        }

        public static bool GetIsWild(int data) => (data & 8) != 0;

        public static int SetIsWild(int data, bool isWild) {
            if (!isWild) {
                return data & -9;
            }
            return data | 8;
        }
    }
}