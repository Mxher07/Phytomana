using Engine;
using Engine.Graphics;

namespace Game {
    public class ExperienceBlock : Block {
        public static int Index = 248;

        public Texture2D m_texture;

        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/Experience");
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) { }
        public override int GetTextureSlotCount(int value) => 1;

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            BlocksManager.DrawFlatBlock(
                primitivesRenderer,
                value,
                size * 0.18f,
                ref matrix,
                m_texture,
                color,
                true,
                environmentData
            );
        }
    }
}