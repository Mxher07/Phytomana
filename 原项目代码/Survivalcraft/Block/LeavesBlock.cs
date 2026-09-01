using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class LeavesBlock : AlphaTestCubeBlock {
        public Random m_random = new();

        public abstract Color GetLeavesBlockColor(int value, Terrain terrain, int x, int y, int z);

        public abstract Color GetLeavesItemColor(int value, DrawBlockEnvironmentData environmentData);

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            Color leavesBlockColor = GetLeavesBlockColor(value, generator.Terrain, x, y, z);
            generator.GenerateCubeVertices(
                this,
                value,
                x,
                y,
                z,
                leavesBlockColor,
                texture == null ? geometry.AlphaTestSubsetsByFace : geometry.GetGeometry(texture).AlphaTestSubsetsByFace
            );
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            color *= GetLeavesItemColor(value, environmentData);
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

        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) {
            Color leavesBlockColor = GetLeavesBlockColor(
                value,
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
                leavesBlockColor,
                GetFaceTextureSlot(4, value)
            );
        }
    }
}