using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class AlphaTestCubeBlock : CubeBlock {
        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            generator.GenerateCubeVertices(
                this,
                value,
                x,
                y,
                z,
                Color.White,
                texture == null ? geometry.AlphaTestSubsetsByFace : geometry.GetGeometry(texture).AlphaTestSubsetsByFace
            );
        }
    }
}