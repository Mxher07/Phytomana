using Engine;
using Engine.Graphics;

namespace Game {
    public class SpikedPlankBlock : MountedElectricElementBlock {
        public static int Index = 86;

        public BlockMesh m_standaloneBlockMesh = new();

        public BlockMesh[] m_blockMeshesByData = new BlockMesh[12];

        public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[12][];

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/SpikedPlanks");
            string[] array = ["SpikedPlankRetracted", "SpikedPlank"];
            for (int i = 0; i < 2; i++) {
                string name = array[i];
                Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(name).ParentBone);
                for (int j = 0; j < 6; j++) {
                    int num = SetMountingFace(SetSpikesState(0, i != 0), j);
                    Matrix m = j >= 4
                        ? j != 4
                            ? Matrix.CreateRotationX((float)Math.PI) * Matrix.CreateTranslation(0.5f, 1f, 0.5f)
                            : Matrix.CreateTranslation(0.5f, 0f, 0.5f)
                        : Matrix.CreateRotationX((float)Math.PI / 2f)
                        * Matrix.CreateTranslation(0f, 0f, -0.5f)
                        * Matrix.CreateRotationY(j * (float)Math.PI / 2f)
                        * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f);
                    m_blockMeshesByData[num] = new BlockMesh();
                    m_blockMeshesByData[num]
                        .AppendModelMeshPart(
                            model.FindMesh(name).MeshParts[0],
                            boneAbsoluteTransform * m,
                            false,
                            false,
                            false,
                            false,
                            Color.White
                        );
                    m_collisionBoxesByData[num] = [m_blockMeshesByData[num].CalculateBoundingBox()];
                }
                Matrix identity = Matrix.Identity;
                m_standaloneBlockMesh.AppendModelMeshPart(
                    model.FindMesh(name).MeshParts[0],
                    boneAbsoluteTransform * identity,
                    false,
                    false,
                    false,
                    false,
                    Color.White
                );
            }
        }

        public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value) {
            int mountingFace = GetMountingFace(Terrain.ExtractData(value));
            return face != CellFace.OppositeFace(mountingFace);
        }

        public override bool ShouldAvoid(int value) => GetSpikesState(Terrain.ExtractData(value));

        public static bool GetSpikesState(int data) => (data & 1) == 0;

        public static int SetSpikesState(int data, bool spikesState) {
            if (spikesState) {
                return data & -2;
            }
            return data | 1;
        }

        public static int GetMountingFace(int data) => ((data >> 1) + 4) % 6;

        public static int SetMountingFace(int data, int face) {
            data &= -15;
            data |= (((face + 2) % 6) & 7) << 1;
            return data;
        }

        public override int GetFace(int value) => GetMountingFace(Terrain.ExtractData(value));

        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            int data = SetMountingFace(SetSpikesState(Terrain.ExtractData(value), true), raycastResult.CellFace.Face);
            BlockPlacementData result = default;
            result.Value = Terrain.ReplaceData(value, data);
            result.CellFace = raycastResult.CellFace;
            return result;
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) {
            int num = Terrain.ExtractData(value);
            if (num >= m_collisionBoxesByData.Length) {
                return base.GetCustomCollisionBoxes(terrain, value);
            }
            return m_collisionBoxesByData[num];
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            int num = Terrain.ExtractData(value);
            if (num < m_blockMeshesByData.Length
                && m_blockMeshesByData[num] != null) {
                Texture2D texture = GetDefaultTexture(value);
                TerrainGeometrySubset subsetOpaque = texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque;
                generator.GenerateShadedMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_blockMeshesByData[num],
                    Color.White,
                    null,
                    null,
                    subsetOpaque
                );
                generator.GenerateWireVertices(
                    value,
                    x,
                    y,
                    z,
                    GetFace(value),
                    1f,
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
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 1f * size, ref matrix, environmentData);
            }
            else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, texture, color, 1f * size, ref matrix, environmentData);
            }
        }

        public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) =>
            new SpikedPlankElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)));

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
                return ElectricConnectorType.Input;
            }
            return null;
        }
    }
}