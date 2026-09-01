using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class FoodBlock : Block {
        public static int m_compostValue = Terrain.MakeBlockValue(168, 0, SoilBlock.SetHydration(SoilBlock.SetNitrogen(0, 1), false));

        public BlockMesh m_standaloneBlockMesh = new();

        public string m_modelName;

        public Matrix m_tcTransform;

        public Color m_color;

        public int m_rottenValue;

        public FoodBlock(string modelName, Matrix tcTransform, Color color, int rottenValue) {
            m_modelName = modelName;
            m_tcTransform = tcTransform;
            m_color = color;
            m_rottenValue = rottenValue;
        }

        public override void Initialize() {
            Model model = ContentManager.Get<Model>(m_modelName);
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.Meshes[0].ParentBone);
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.Meshes[0].MeshParts[0],
                boneAbsoluteTransform,
                false,
                false,
                false,
                false,
                m_color
            );
            m_standaloneBlockMesh.TransformTextureCoordinates(m_tcTransform);
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

        public override int GetDamageDestructionValue(int value) => m_rottenValue;
    }
}