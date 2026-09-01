using Engine;
using Engine.Graphics;

namespace Game {
    public class TallGrassBlock : CrossBlock {
        public static int Index = 19;

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            int data = Terrain.ExtractData(oldValue);
            if (!GetIsSmall(data)) {
                dropValues.Add(new BlockDropValue { Value = Terrain.MakeBlockValue(19, 0, data), Count = 1 });
            }
            showDebris = true;
        }

        public override int GetFaceTextureSlot(int face, int value) {
            if (!GetIsSmall(Terrain.ExtractData(value))) {
                return 85;
            }
            return 84;
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            BlocksManager.DrawFlatOrImageExtrusionBlock(
                primitivesRenderer,
                value,
                size,
                ref matrix,
                GetDefaultTexture(value),
                color * BlockColorsMap.Grass.Lookup(environmentData.Temperature, environmentData.Humidity),
                false,
                environmentData
            );
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            generator.GenerateCrossfaceVertices(
                this,
                value,
                x,
                y,
                z,
                BlockColorsMap.Grass.Lookup(generator.Terrain, x, y, z),
                GetFaceTextureSlot(0, value),
                texture == null ? geometry.SubsetAlphaTest : geometry.GetGeometry(texture).SubsetAlphaTest
            );
        }

        public override int GetShadowStrength(int value) {
            if (!GetIsSmall(Terrain.ExtractData(value))) {
                return DefaultShadowStrength;
            }
            return DefaultShadowStrength / 2;
        }

        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) {
            Color color = BlockColorsMap.Grass.Lookup(
                subsystemTerrain.Terrain,
                Terrain.ToCell(position.X),
                Terrain.ToCell(position.Y),
                Terrain.ToCell(position.Z)
            );
            return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color, GetFaceTextureSlot(4, value));
        }

        public static bool GetIsSmall(int data) => (data & 8) != 0;

        public static int SetIsSmall(int data, bool isSmall) {
            if (!isSmall) {
                return data & -9;
            }
            return data | 8;
        }
    }
}