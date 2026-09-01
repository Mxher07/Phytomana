using Engine;
using Engine.Graphics;

namespace Game {
    public class TreasureGeneratorBlock : Block {
        public static int Index = 190;

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) { }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) { }
    }
}