using Engine;
using Engine.Graphics;

namespace Game {
    public class ManaIngotBlock : Block {
        public Texture2D m_texture;
        public BlockMesh m_standaloneBlockMesh = new();

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Ingots");
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/ManaIngot");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("IronIngot").ParentBone);
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("IronIngot").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.1f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            int contents = BlocksManager.GetBlockIndex<ManaIngotBlock>();
            Log.Information($"[PhytoMana]{contents} Registered.");
            base.Initialize();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) { }

        public override Texture2D GetDefaultTexture(int value) => m_texture;

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, m_texture, color, 2f * size, ref matrix, environmentData);
        }
    }
}