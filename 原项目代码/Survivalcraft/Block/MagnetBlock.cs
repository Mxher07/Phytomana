using Engine;
using Engine.Graphics;

namespace Game {
    public class MagnetBlock : Block {
        public static int Index = 167;

        public BlockMesh[] m_meshesByData = new BlockMesh[2];

        public BlockMesh m_standaloneMesh = new();

        public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[2][];

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Magnet");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Magnet").ParentBone);
            for (int i = 0; i < 2; i++) {
                m_meshesByData[i] = new BlockMesh();
                m_meshesByData[i]
                    .AppendModelMeshPart(
                        model.FindMesh("Magnet").MeshParts[0],
                        boneAbsoluteTransform * Matrix.CreateRotationY((float)Math.PI / 2f * i) * Matrix.CreateTranslation(0.5f, 0f, 0.5f),
                        false,
                        false,
                        true,
                        false,
                        Color.White
                    );
                m_collisionBoxesByData[i] = [m_meshesByData[i].CalculateBoundingBox()];
            }
            m_standaloneMesh.AppendModelMeshPart(
                model.FindMesh("Magnet").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateScale(1.5f) * Matrix.CreateTranslation(0f, -0.25f, 0f),
                false,
                false,
                true,
                false,
                Color.White
            );
            base.Initialize();
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) {
            int num = Terrain.ExtractData(value);
            if (num < m_collisionBoxesByData.Length) {
                return m_collisionBoxesByData[num];
            }
            return null;
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            int num = Terrain.ExtractData(value);
            if (num < m_collisionBoxesByData.Length) {
                Texture2D texture = GetDefaultTexture(value);
                generator.GenerateMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_meshesByData[num],
                    Color.White,
                    null,
                    texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque
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
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneMesh, color, size, ref matrix, environmentData);
            }
            else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneMesh, texture, color, size, ref matrix, environmentData);
            }
        }

        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            BlockPlacementData result;
            if (componentMiner.Project.FindSubsystem<SubsystemMagnetBlockBehavior>(true).MagnetsCount < 8) {
                Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
                int data = !(MathF.Abs(forward.X) > MathF.Abs(forward.Z)) ? 1 : 0;
                result = default;
                result.CellFace = raycastResult.CellFace;
                result.Value = Terrain.ReplaceData(value, data);
                return result;
            }
            componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage("Too many magnets", Color.White, true, false);
            result = default;
            return result;
        }
    }
}