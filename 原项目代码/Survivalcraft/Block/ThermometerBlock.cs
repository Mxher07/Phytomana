using Engine;
using Engine.Graphics;

namespace Game {
    public class ThermometerBlock : Block, IElectricElementBlock {
        public static int Index = 120;

        public BlockMesh m_caseMesh = new();

        public BlockMesh m_fluidMesh = new();

        public Matrix[] m_matricesByData = new Matrix[4];

        public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[4][];

        public float m_fluidBottomPosition;

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Thermometer");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Case").ParentBone);
            Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Fluid").ParentBone);
            m_caseMesh.AppendModelMeshPart(
                model.FindMesh("Case").MeshParts[0],
                boneAbsoluteTransform,
                false,
                false,
                true,
                false,
                Color.White
            );
            m_fluidMesh.AppendModelMeshPart(
                model.FindMesh("Fluid").MeshParts[0],
                boneAbsoluteTransform2,
                false,
                false,
                false,
                false,
                Color.White
            );
            for (int i = 0; i < 4; i++) {
                m_matricesByData[i] = Matrix.CreateScale(1.5f)
                    * Matrix.CreateTranslation(0.95f, 0.15f, 0.5f)
                    * Matrix.CreateTranslation(-0.5f, 0f, -0.5f)
                    * Matrix.CreateRotationY((i + 1) * (float)Math.PI / 2f)
                    * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
                m_collisionBoxesByData[i] = [m_caseMesh.CalculateBoundingBox(m_matricesByData[i])];
            }
            m_fluidBottomPosition = m_fluidMesh.Vertices.Min(v => v.Position.Y);
            base.Initialize();
        }

        public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) {
            int num = Terrain.ExtractData(value);
            return new ThermometerElectricElement(subsystemElectricity, new CellFace(x, y, z, num & 3));
        }

        public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain,
            int value,
            int face,
            int connectorFace,
            int x,
            int y,
            int z) {
            if ((Terrain.ExtractData(value) & 3) == face) {
                return ElectricConnectorType.Output;
            }
            return null;
        }

        public int GetConnectionMask(int value) => int.MaxValue;

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) {
            int num = Terrain.ExtractData(value);
            if (num < m_collisionBoxesByData.Length) {
                return m_collisionBoxesByData[num];
            }
            return null;
        }

        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            int value2 = 0;
            if (raycastResult.CellFace.Face == 0) {
                value2 = Terrain.ReplaceData(Terrain.ReplaceContents(120), 0);
            }
            if (raycastResult.CellFace.Face == 1) {
                value2 = Terrain.ReplaceData(Terrain.ReplaceContents(120), 1);
            }
            if (raycastResult.CellFace.Face == 2) {
                value2 = Terrain.ReplaceData(Terrain.ReplaceContents(120), 2);
            }
            if (raycastResult.CellFace.Face == 3) {
                value2 = Terrain.ReplaceData(Terrain.ReplaceContents(120), 3);
            }
            BlockPlacementData result = default;
            result.Value = value2;
            result.CellFace = raycastResult.CellFace;
            return result;
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            int num = Terrain.ExtractData(value);
            if (num < m_matricesByData.Length) {
                int num2 = generator.SubsystemMetersBlockBehavior?.GetThermometerReading(x, y, z) ?? 8;
                float y2 = MathUtils.Lerp(1.02f, 3.91f, MathUtils.Saturate(num2 / 20f));
                Matrix matrix = m_matricesByData[num];
                Matrix value2 = Matrix.CreateTranslation(0f, 0f - m_fluidBottomPosition, 0f)
                    * Matrix.CreateScale(1f, y2, 1f)
                    * Matrix.CreateTranslation(0f, m_fluidBottomPosition, 0f)
                    * matrix;
                Texture2D texture = GetDefaultTexture(value);
                TerrainGeometrySubset subsetOpaque = texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque;
                generator.GenerateMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_caseMesh,
                    Color.White,
                    matrix,
                    subsetOpaque
                );
                generator.GenerateMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_fluidMesh,
                    Color.White,
                    value2,
                    subsetOpaque
                );
                generator.GenerateWireVertices(
                    value,
                    x,
                    y,
                    z,
                    num & 3,
                    0.2f,
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
            Matrix matrix2 = Matrix.CreateScale(3f * size) * Matrix.CreateTranslation(0f, -0.15f, 0f) * matrix;
            Texture2D texture = GetDefaultTexture(value);
            if (texture == null) {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_caseMesh, color, 1f, ref matrix2, environmentData);
            }
            else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_caseMesh, texture, color, 1f, ref matrix2, environmentData);
            }
            if (environmentData.EnvironmentTemperature.HasValue) {
                float y = MathUtils.Lerp(1.02f, 3.91f, MathUtils.Saturate(environmentData.EnvironmentTemperature.Value / 20f));
                Matrix matrix3 = Matrix.CreateTranslation(0f, 0f - m_fluidBottomPosition, 0f)
                    * Matrix.CreateScale(1f, y, 1f)
                    * Matrix.CreateTranslation(0f, m_fluidBottomPosition, 0f)
                    * matrix2;
                if (texture == null) {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_fluidMesh, color, 1f, ref matrix3, environmentData);
                }
                else {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_fluidMesh, texture, color, 1f, ref matrix3, environmentData);
                }
            }
        }
    }
}