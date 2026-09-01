using Engine;
using Engine.Graphics;

namespace Game {
    public class ButtonBlock : MountedElectricElementBlock {
        public static int Index = 142;

        public BlockMesh m_standaloneBlockMesh = new();

        public BlockMesh[] m_blockMeshesByFace = new BlockMesh[6];

        public BoundingBox[][] m_collisionBoxesByFace = new BoundingBox[6][];

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Button");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Button").ParentBone);
            for (int i = 0; i < 6; i++) {
                Matrix matrix = i >= 4
                    ? i != 4
                        ? Matrix.CreateRotationX((float)Math.PI) * Matrix.CreateTranslation(0.5f, 1f, 0.5f)
                        : Matrix.CreateTranslation(0.5f, 0f, 0.5f)
                    : Matrix.CreateRotationX((float)Math.PI / 2f)
                    * Matrix.CreateTranslation(0f, 0f, -0.5f)
                    * Matrix.CreateRotationY(i * (float)Math.PI / 2f)
                    * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f);
                m_blockMeshesByFace[i] = new BlockMesh();
                m_blockMeshesByFace[i]
                    .AppendModelMeshPart(
                        model.FindMesh("Button").MeshParts[0],
                        boneAbsoluteTransform * matrix,
                        false,
                        false,
                        false,
                        false,
                        Color.White
                    );
                m_collisionBoxesByFace[i] = [m_blockMeshesByFace[i].CalculateBoundingBox()];
            }
            Matrix matrix2 = Matrix.CreateRotationY(-(float)Math.PI / 2f) * Matrix.CreateRotationZ((float)Math.PI / 2f);
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Button").MeshParts[0],
                boneAbsoluteTransform * matrix2,
                false,
                false,
                false,
                false,
                Color.White
            );
        }

        public static int GetVoltageLevel(int data) => 15 - ((data >> 3) & 0xF);

        public static int SetVoltageLevel(int data, int voltageLevel) => (data & -121) | ((15 - (voltageLevel & 0xF)) << 3);

        public override int GetFace(int value) => Terrain.ExtractData(value) & 7;

        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            BlockPlacementData result = default;
            result.Value = Terrain.ReplaceData(value, raycastResult.CellFace.Face);
            int data = SetVoltageLevel(Terrain.ExtractData(result.Value), GetVoltageLevel(Terrain.ExtractData(value)));
            result.Value = Terrain.ReplaceData(value, data);
            result.CellFace = raycastResult.CellFace;
            return result;
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) {
            int face = GetFace(value);
            if (face >= m_collisionBoxesByFace.Length) {
                return null;
            }
            return m_collisionBoxesByFace[face];
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            TerrainGeometrySubset subsetOpaque = texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque;
            int face = GetFace(value);
            if (face < m_blockMeshesByFace.Length) {
                generator.GenerateMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_blockMeshesByFace[face],
                    Color.White,
                    null,
                    subsetOpaque
                );
                generator.GenerateWireVertices(
                    value,
                    x,
                    y,
                    z,
                    GetFace(value),
                    0.25f,
                    Vector2.Zero,
                    subsetOpaque
                );
            }
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            Texture2D texture = GetDefaultTexture(value);
            if (texture == null) {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 2f * size, ref matrix, environmentData);
            }
            else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, texture, color, 2f * size, ref matrix, environmentData);
            }
        }

        public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) =>
            new ButtonElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)), value);

        public override ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain,
            int value,
            int face,
            int connectorFace,
            int x,
            int y,
            int z) {
            int face2 = GetFace(value);
            if (face == face2
                && SubsystemElectricity.GetConnectorDirection(face2, 0, connectorFace).HasValue) {
                return ElectricConnectorType.Output;
            }
            return null;
        }
    }
}