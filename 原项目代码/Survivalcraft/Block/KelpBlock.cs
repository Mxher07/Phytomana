using Engine;
using Engine.Graphics;

namespace Game {
    public class KelpBlock : WaterPlantBlock {
        public new const int Index = 232;

        public override int GetFaceTextureSlot(int face, int value) {
            if (face < 0) {
                return 104;
            }
            return base.GetFaceTextureSlot(face, value);
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
                color * BlockColorsMap.Kelp.Lookup(environmentData.Temperature, environmentData.Humidity),
                false,
                environmentData
            );
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            Color color = BlockColorsMap.Kelp.Lookup(generator.Terrain, x, y, z);
            generator.GenerateCrossfaceVertices(
                this,
                value,
                x,
                y,
                z,
                color,
                GetFaceTextureSlot(-1, value),
                texture == null ? geometry.SubsetAlphaTest : geometry.GetGeometry(texture).SubsetAlphaTest
            );
            base.GenerateTerrainVertices(generator, geometry, value, x, y, z);
        }

        public override BlockDebrisParticleSystem
            CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength) => new(
            subsystemTerrain,
            position,
            0.75f * strength,
            DestructionDebrisScale,
            BlockColorsMap.Kelp.Lookup(subsystemTerrain.Terrain, Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z)),
            104
        );
    }
}