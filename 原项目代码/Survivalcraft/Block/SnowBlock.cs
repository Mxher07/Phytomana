using Engine;
using Engine.Graphics;

namespace Game {
    public class SnowBlock : CubeBlock {
        public static int Index = 61;

        const float m_height = 0.125f;

        public BoundingBox[] m_collisionBoxes = [new(new Vector3(0f, 0f, 0f), new Vector3(1f, m_height, 1f))];

        public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value) => face != 5;

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
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
                Color.White,
                Color.White,
                Color.White,
                Color.White,
                Color.White,
                -1,
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

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            showDebris = true;
            if (toolLevel >= RequiredToolLevel) {
                int num = Random.Int(1, 3);
                for (int i = 0; i < num; i++) {
                    dropValues.Add(new BlockDropValue { Value = Terrain.MakeBlockValue(85), Count = 1 });
                }
            }
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) => m_collisionBoxes;
    }
}