using Engine;
using Engine.Graphics;

namespace Game {
    public class ManaAxeBlock : Block {
        public BlockMesh m_meshStone = new();
        public BlockMesh m_meshMana = new();
        public Texture2D m_textureStone;
        public Texture2D m_textureMana;
        
        public override void Initialize() {
            base.Initialize();
            
            Model model = ContentManager.Get<Model>("Models/Axe");
            m_textureStone = ContentManager.Get<Texture2D>("Textures/PhytoMana/GrownWood");
            m_textureMana = ContentManager.Get<Texture2D>("Textures/PhytoMana/ManaIngot");
            
            
            var poolMesh = model.FindMesh("Handle");
            Matrix poolBone = BlockMesh.GetBoneAbsoluteTransform(poolMesh.ParentBone);
            m_meshStone.AppendModelMeshPart(
                poolMesh.MeshParts[0],
                poolBone * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false, false, false, false,
                Color.White
            );
            
            
            var manaMesh = model.FindMesh("Head");
            Matrix manaBone = BlockMesh.GetBoneAbsoluteTransform(manaMesh.ParentBone);
            m_meshMana.AppendModelMeshPart(
                manaMesh.MeshParts[0],
                manaBone * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false, false, false, false,
                Color.White
            );
            
        
        }
        
        
        public override Texture2D GetDefaultTexture(int value) => m_textureStone;
        
        
        public override void GenerateTerrainVertices(
            BlockGeometryGenerator generator, 
            TerrainGeometry geometry, 
            int value, 
            int x, 
            int y, 
            int z
        ) {
            Matrix matrix = Matrix.CreateScale(0.0625f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
            generator.GenerateMeshVertices(
                this,
                x,
                y,
                z,
                m_meshStone,
                Color.White,
                matrix,
                geometry.GetGeometry(m_textureStone).SubsetOpaque
            );
            generator.GenerateMeshVertices(
                this,
                x,
                y,
                z,
                m_meshMana,
                Color.White,
                matrix,
                geometry.GetGeometry(m_textureMana).SubsetOpaque
            );
        }
        
        
        public override void DrawBlock(
            PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData
        ) {
            float drawSize = environmentData.DrawBlockMode == DrawBlockMode.World ? 2f * size * 0.05f : 2f * size;
            BlocksManager.DrawMeshBlock(
                primitivesRenderer, 
                m_meshStone, 
                m_textureStone,
                color, 
                drawSize, 
                ref matrix, 
                environmentData
            );
            
            BlocksManager.DrawMeshBlock(
                primitivesRenderer, 
                m_meshMana, 
                m_textureMana,
                color, 
                drawSize, 
                ref matrix, 
                environmentData
            );
        }
    }
}