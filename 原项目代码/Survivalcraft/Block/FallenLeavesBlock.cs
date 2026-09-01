using Engine;
using Engine.Graphics;

namespace Game {
    public class FallenLeavesBlock : CubeBlock {
        public const int Index = 261;

        public const float m_height = 0.0625f;

        public BoundingBox[] m_collisionBoxes = [new(new Vector3(0f, 0f, 0f), new Vector3(1f, m_height, 1f))];

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            Color sideColor = new(180, 170, 160);
            Color color = GetColor(x, y, z);
            Color color2 = GetColor(x, y, z + 1);
            Color color3 = GetColor(x + 1, y, z);
            Color color4 = GetColor(x + 1, y, z + 1);
            generator.GenerateCubeVertices(
                this,
                value,
                x,
                y,
                z,
                m_height,
                m_height,
                m_height,
                m_height,
                sideColor,
                color,
                color3,
                color4,
                color2,
                -1,
                texture == null ? geometry.AlphaTestSubsetsByFace : geometry.GetGeometry(texture).AlphaTestSubsetsByFace
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
                    m_height,
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
                    m_height,
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
            Color color = GetColor(Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
            return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color, GetFaceTextureSlot(4, value));
        }

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            showDebris = true;
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) => m_collisionBoxes;

        public static Color GetColor(int x, int y, int z) {
            uint num = (uint)MathUtils.Hash(x + y * 59 + z * 2411);
            return Color.Lerp(new Color(128, 110, 110), new Color(255, 255, 220), num / 4.2949673E+09f);
        }
    }
}