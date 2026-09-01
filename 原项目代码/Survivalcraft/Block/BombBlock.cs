using Engine;
using Engine.Graphics;

namespace Game {
    public class BombBlock : Block {
        public static int Index = 201;

        public BlockMesh m_standaloneBlockMesh = new();

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Bomb");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Bomb").ParentBone);
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Bomb").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.25f, 0f),
                false,
                false,
                false,
                false,
                new Color(0.3f, 0.3f, 0.3f)
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