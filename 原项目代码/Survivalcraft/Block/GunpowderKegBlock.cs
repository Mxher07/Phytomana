using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class GunpowderKegBlock : Block, IElectricElementBlock {
        public BlockMesh m_standaloneBlockMesh = new();

        public BlockMesh m_blockMesh = new();

        public BoundingBox[] m_collisionBoxes;

        public string m_modelName;

        public bool m_isIncendiary;

        public Vector3 FuseOffset { get; set; }

        public GunpowderKegBlock(string modelName, bool isIncendiary) {
            m_modelName = modelName;
            m_isIncendiary = isIncendiary;
        }

        public override void Initialize() {
            Model model = ContentManager.Get<Model>(m_modelName);
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Keg").ParentBone);
            FuseOffset = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Fuse").ParentBone).Translation + new Vector3(0.5f, 0f, 0.5f);
            BlockMesh blockMesh = new();
            blockMesh.AppendModelMeshPart(
                model.FindMesh("Keg").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, 0f, 0.5f),
                false,
                false,
                false,
                false,
                Color.White
            );
            m_blockMesh.AppendBlockMesh(blockMesh);
            if (m_isIncendiary) {
                m_blockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(-0.25f, 0f, 0f));
            }
            m_collisionBoxes = [blockMesh.CalculateBoundingBox()];
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Keg").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            if (m_isIncendiary) {
                m_standaloneBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(-0.25f, 0f, 0f));
            }
            base.Initialize();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            TerrainGeometrySubset subsetOpaque = texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque;
            generator.GenerateMeshVertices(
                this,
                x,
                y,
                z,
                m_blockMesh,
                Color.White,
                null,
                subsetOpaque
            );
            generator.GenerateWireVertices(
                value,
                x,
                y,
                z,
                4,
                0.25f,
                Vector2.Zero,
                subsetOpaque
            );
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            Texture2D texture = GetDefaultTexture(value);
            if (texture == null) {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
            }
            else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, texture, color, size, ref matrix, environmentData);
            }
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) => m_collisionBoxes;

        public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) =>
            new GunpowderKegElectricElement(subsystemElectricity, new CellFace(x, y, z, 4));

        public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain,
            int value,
            int face,
            int connectorFace,
            int x,
            int y,
            int z) {
            if (face == 4) {
                return ElectricConnectorType.Input;
            }
            return null;
        }

        public int GetConnectionMask(int value) => int.MaxValue;
    }
}