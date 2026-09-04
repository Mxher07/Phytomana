using Engine;
using Engine.Graphics;

namespace Game {
    public class GrownRodBlock : Block {
        public BlockMesh m_standaloneBlockMesh = new();
        public Texture2D m_texture;
        
        public override void Initialize() {
            base.Initialize();
            
            Model model = ContentManager.Get<Model>("Models/Rod");
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/GrownWood");
            
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(
                model.FindMesh("IronRod").ParentBone
            );
            
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("IronRod").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
        }

        public override int GetTextureSlotCount(int value) => 1;
        
        public override Texture2D GetDefaultTexture(int value) => m_texture;

        public override void GenerateTerrainVertices(
            BlockGeometryGenerator generator, 
            TerrainGeometry geometry, 
            int value, 
            int x, 
            int y, 
            int z
        ) { 
        }

        public override void DrawBlock(
            PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData
        ) {
            BlocksManager.DrawMeshBlock(
                primitivesRenderer, 
                m_standaloneBlockMesh, 
                m_texture,
                color, 
                2f * size, 
                ref matrix, 
                environmentData
            );
        }
    }
}