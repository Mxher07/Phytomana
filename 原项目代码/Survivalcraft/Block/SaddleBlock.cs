using Engine;
using Engine.Graphics;

namespace Game {
    public class SaddleBlock : Block {
        public static int Index = 158;

        public BlockMesh m_standaloneBlockMesh = new();

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Saddle");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Saddle").ParentBone);
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Saddle").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.2f, 0f),
                false,
                false,
                false,
                false,
                new Color(224, 224, 224)
            );
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Saddle").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.2f, 0f),
                false,
                true,
                false,
                false,
                new Color(96, 96, 96)
            );
            base.Initialize();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) { }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            Texture2D texture = GetDefaultTexture(value);
            if (texture == null) {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 2f * size, ref matrix, environmentData);
            } else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, texture, color, 2f * size, ref matrix, environmentData);
            }
        }
    }
}