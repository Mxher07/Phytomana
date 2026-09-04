using Engine;
using Engine.Graphics;

namespace Game {
    public class ManaPoolBlock : Block {
        public BlockMesh m_meshStone = new();
        public BlockMesh m_meshMana = new();
        public Texture2D m_textureStone;
        public Texture2D m_textureMana;
        public SubsystemMana m_subsystemMana;

        public override bool IsTransparent_(int value) => true;

        public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value) => true;
        
        public override void Initialize() {
            base.Initialize();
            
            Model model = ContentManager.Get<Model>("Models/PhytoMana/mana_pool");
            m_textureStone = ContentManager.Get<Texture2D>("Textures/PhytoMana/GrownStone");
            m_textureMana = ContentManager.Get<Texture2D>("Textures/PhytoMana/Mana2");
            
            
            var poolMesh = model.FindMesh("base");
            Matrix poolBone = BlockMesh.GetBoneAbsoluteTransform(poolMesh.ParentBone);
            m_meshStone.AppendModelMeshPart(
                poolMesh.MeshParts[0],
                poolBone * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false, false, false, false,
                Color.White
            );
            
            
            var manaMesh = model.FindMesh("mana");
            Matrix manaBone = BlockMesh.GetBoneAbsoluteTransform(manaMesh.ParentBone);
            m_meshMana.AppendModelMeshPart(
                manaMesh.MeshParts[0],
                manaBone * Matrix.CreateTranslation(0f, 0.5f, 0f),
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
            if (m_subsystemMana == null) {
                m_subsystemMana = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemMana>(true);
            }
            Matrix baseMatrix = Matrix.CreateScale(0.0625f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
            generator.GenerateMeshVertices(
                this,
                x,
                y,
                z,
                m_meshStone,
                Color.White,
                baseMatrix,
                geometry.GetGeometry(m_textureStone).SubsetOpaque
            );
            float currentMana = m_subsystemMana.GetManaAmount(new Point3(x, y, z));
            float maxMana = m_subsystemMana.GetMaxManaAmount(Terrain.ExtractContents(value));
            if (currentMana > 0f && maxMana > 0f) {
                float pct = MathUtils.Clamp(currentMana / maxMana, 0f, 1f);
                float manaY = 0.5f + 0.2f * pct;
                Matrix manaMatrix = Matrix.CreateScale(0.0625f) * Matrix.CreateTranslation(0.5f, manaY, 0.5f);
                generator.GenerateMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_meshMana,
                    Color.White,
                    manaMatrix,
                    geometry.GetGeometry(m_textureMana).SubsetOpaque
                );
            }
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
