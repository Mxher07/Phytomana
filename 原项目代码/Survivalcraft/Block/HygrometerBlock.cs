using Engine;
using Engine.Graphics;

namespace Game {
    public class HygrometerBlock : Block, IElectricElementBlock {
        public static int Index = 121;

        public BlockMesh m_caseMesh = new();

        public BlockMesh m_pointerMesh = new();

        public Matrix m_pointerMatrix;

        public Matrix m_invPointerMatrix;

        public Matrix[] m_matricesByData = new Matrix[4];

        public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[4][];

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Hygrometer");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Case").ParentBone);
            Matrix matrix = m_pointerMatrix = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Pointer").ParentBone);
            m_invPointerMatrix = Matrix.Invert(m_pointerMatrix);
            m_caseMesh.AppendModelMeshPart(
                model.FindMesh("Case").MeshParts[0],
                boneAbsoluteTransform,
                false,
                false,
                true,
                false,
                Color.White
            );
            m_pointerMesh.AppendModelMeshPart(
                model.FindMesh("Pointer").MeshParts[0],
                matrix,
                false,
                false,
                false,
                false,
                Color.White
            );
            for (int i = 0; i < 4; i++) {
                m_matricesByData[i] = Matrix.CreateScale(5f)
                    * Matrix.CreateTranslation(0.95f, 0.15f, 0.5f)
                    * Matrix.CreateTranslation(-0.5f, 0f, -0.5f)
                    * Matrix.CreateRotationY((i + 1) * (float)Math.PI / 2f)
                    * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
                m_collisionBoxesByData[i] = [m_caseMesh.CalculateBoundingBox(m_matricesByData[i])];
            }
            base.Initialize();
        }

        public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) {
            int num = Terrain.ExtractData(value);
            return new HygrometerElectricElement(subsystemElectricity, new CellFace(x, y, z, num & 3));
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
                value2 = Terrain.ReplaceData(Terrain.ReplaceContents(121), 0);
            }
            else if (raycastResult.CellFace.Face == 1) {
                value2 = Terrain.ReplaceData(Terrain.ReplaceContents(121), 1);
            }
            else if (raycastResult.CellFace.Face == 2) {
                value2 = Terrain.ReplaceData(Terrain.ReplaceContents(121), 2);
            }
            else if (raycastResult.CellFace.Face == 3) {
                value2 = Terrain.ReplaceData(Terrain.ReplaceContents(121), 3);
            }
            BlockPlacementData result = default;
            result.Value = value2;
            result.CellFace = raycastResult.CellFace;
            return result;
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            TerrainGeometrySubset subsetOpaque = texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque;
            int num = Terrain.ExtractData(value);
            if (num < m_matricesByData.Length) {
                int humidity = generator.Terrain.GetHumidity(x, z);
                float radians = MathUtils.Lerp(1.5f, -1.5f, humidity / 15f);
                Matrix matrix = m_matricesByData[num];
                Matrix value2 = m_invPointerMatrix * Matrix.CreateRotationX(radians) * m_pointerMatrix * matrix;
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
                    m_pointerMesh,
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
            float num = 8f;
            if (environmentData != null
                && environmentData.SubsystemTerrain != null) {
                Vector3 translation = environmentData.InWorldMatrix.Translation;
                int num2 = Terrain.ToCell(translation.X);
                int num3 = Terrain.ToCell(translation.Z);
                float f = translation.X - num2;
                float f2 = translation.Z - num3;
                float x = environmentData.SubsystemTerrain.Terrain.GetSeasonalHumidity(num2, num3);
                float x2 = environmentData.SubsystemTerrain.Terrain.GetSeasonalHumidity(num2, num3 + 1);
                float x3 = environmentData.SubsystemTerrain.Terrain.GetSeasonalHumidity(num2 + 1, num3);
                float x4 = environmentData.SubsystemTerrain.Terrain.GetSeasonalHumidity(num2 + 1, num3 + 1);
                float x5 = MathUtils.Lerp(x, x2, f2);
                float x6 = MathUtils.Lerp(x3, x4, f2);
                num = MathUtils.Lerp(x5, x6, f);
            }
            float radians = MathUtils.Lerp(1.5f, -1.5f, num / 15f);
            Matrix matrix2 = Matrix.CreateScale(7f * size) * Matrix.CreateTranslation(0f, -0.1f, 0f) * matrix;
            Matrix matrix3 = m_invPointerMatrix * Matrix.CreateRotationX(radians) * m_pointerMatrix * matrix2;
            if (texture == null) {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_caseMesh, color, 1f, ref matrix2, environmentData);
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_pointerMesh, color, 1f, ref matrix3, environmentData);
            }
            else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_caseMesh, texture, color, 1f, ref matrix2, environmentData);
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_pointerMesh, texture, color, 1f, ref matrix3, environmentData);
            }
        }
    }
}