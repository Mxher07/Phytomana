using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class MacheteBlock : Block {
        public int m_handleTextureSlot;

        public int m_headTextureSlot;

        public BlockMesh m_standaloneBlockMesh = new();

        public MacheteBlock(int handleTextureSlot, int headTextureSlot) {
            m_handleTextureSlot = handleTextureSlot;
            m_headTextureSlot = headTextureSlot;
        }

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Machete");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Handle").ParentBone);
            Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Head").ParentBone);
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
            blockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(m_handleTextureSlot % 16 / 16f, m_handleTextureSlot / 16 / 16f, 0f));
            BlockMesh blockMesh2 = new();
            blockMesh2.AppendModelMeshPart(
                model.FindMesh("Head").MeshParts[0],
                boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            blockMesh2.TransformTextureCoordinates(Matrix.CreateTranslation(m_headTextureSlot % 16 / 16f, m_headTextureSlot / 16 / 16f, 0f));
            m_standaloneBlockMesh.AppendBlockMesh(blockMesh);
            m_standaloneBlockMesh.AppendBlockMesh(blockMesh2);
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