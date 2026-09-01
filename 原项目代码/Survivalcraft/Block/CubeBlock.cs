using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class CubeBlock : Block {
        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            generator.GenerateCubeVertices(
                this,
                value,
                x,
                y,
                z,
                Color.White,
                texture == null ? geometry.OpaqueSubsetsByFace : geometry.GetGeometry(texture).OpaqueSubsetsByFace
            );
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            Texture2D texture = GetDefaultTexture(value);
            if (texture == null) {
                BlocksManager.DrawCubeBlock(
                    primitivesRenderer,
                    value,
                    new Vector3(size),
                    ref matrix,
                    color,
                    color,
                    environmentData
                );
            }
            else {
                BlocksManager.DrawCubeBlock(
                    primitivesRenderer,
                    value,
                    new Vector3(size),
                    ref matrix,
                    color,
                    color,
                    environmentData,
                    texture
                );
            }
        }
    }
}