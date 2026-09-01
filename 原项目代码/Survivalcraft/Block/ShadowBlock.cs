using Engine;
using Engine.Graphics;

namespace Game {
    public class ShadowBlock : Block {
        public static int Index = 257;

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) { }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) { }

        public override int GetShadowStrength(int value) => Terrain.ExtractData(value) - 128;

        public static int SetShadowStrength(int data, int shadowStrength) {
            shadowStrength = Math.Clamp(shadowStrength, -128, 128);
            return shadowStrength + 128;
        }
    }
}