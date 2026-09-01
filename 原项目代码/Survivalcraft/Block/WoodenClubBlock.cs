using Engine;
using Engine.Graphics;

namespace Game {
    public class WoodenClubBlock : Block {
        public static int Index = 122;

        public BlockMesh m_standaloneBlockMesh = new();

        public override void Initialize() {
            int num = 47;
            Model model = ContentManager.Get<Model>("Models/WoodenClub");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Handle").ParentBone);
            BlockMesh blockMesh = new();
            blockMesh.AppendModelMeshPart(
                model.FindMesh("Handle").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            blockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(num % 16 / 16f, num / 16 / 16f, 0f));
            m_standaloneBlockMesh.AppendBlockMesh(blockMesh);
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