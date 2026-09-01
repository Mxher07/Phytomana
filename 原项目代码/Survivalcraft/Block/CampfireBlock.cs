using Engine;
using Engine.Graphics;

namespace Game {
    public class CampfireBlock : Block {
        public static int Index = 209;

        public BlockMesh[] m_meshesByData = new BlockMesh[16];

        public BlockMesh m_standaloneMesh = new();

        public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[16][];

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Campfire");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Wood").ParentBone);
            Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Ashes").ParentBone);
            for (int i = 0; i < 16; i++) {
                m_meshesByData[i] = new BlockMesh();
                if (i == 0) {
                    m_meshesByData[i]
                        .AppendModelMeshPart(
                            model.FindMesh("Ashes").MeshParts[0],
                            boneAbsoluteTransform2 * Matrix.CreateScale(3f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f),
                            false,
                            false,
                            false,
                            false,
                            Color.White
                        );
                }
                else {
                    float scale = MathUtils.Lerp(1.5f, 4f, i / 15f);
                    float radians = i * (float)Math.PI / 2f;
                    m_meshesByData[i]
                        .AppendModelMeshPart(
                            model.FindMesh("Wood").MeshParts[0],
                            boneAbsoluteTransform
                            * Matrix.CreateScale(scale)
                            * Matrix.CreateRotationY(radians)
                            * Matrix.CreateTranslation(0.5f, 0f, 0.5f),
                            false,
                            false,
                            false,
                            false,
                            Color.White
                        );
                    m_meshesByData[i]
                        .AppendModelMeshPart(
                            model.FindMesh("Ashes").MeshParts[0],
                            boneAbsoluteTransform2
                            * Matrix.CreateScale(scale)
                            * Matrix.CreateRotationY(radians)
                            * Matrix.CreateTranslation(0.5f, 0f, 0.5f),
                            false,
                            false,
                            false,
                            false,
                            Color.White
                        );
                }
                BoundingBox boundingBox = m_meshesByData[i].CalculateBoundingBox();
                boundingBox.Min.X = 0f;
                boundingBox.Min.Z = 0f;
                boundingBox.Max.X = 1f;
                boundingBox.Max.Z = 1f;
                m_collisionBoxesByData[i] = [boundingBox];
            }
            m_standaloneMesh.AppendModelMeshPart(
                model.FindMesh("Wood").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateScale(3f) * Matrix.CreateTranslation(0f, 0f, 0f),
                false,
                false,
                true,
                false,
                Color.White
            );
            m_standaloneMesh.AppendModelMeshPart(
                model.FindMesh("Ashes").MeshParts[0],
                boneAbsoluteTransform2 * Matrix.CreateScale(3f) * Matrix.CreateTranslation(0f, 0f, 0f),
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
            Texture2D texture = GetDefaultTexture(value);
            TerrainGeometrySubset subsetOpaque = texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque;
            int num = Terrain.ExtractData(value);
            if (num < m_meshesByData.Length) {
                generator.GenerateMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_meshesByData[num],
                    Color.White,
                    null,
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
            BlockPlacementData result = default;
            result.CellFace = raycastResult.CellFace;
            result.Value = Terrain.MakeBlockValue(209, 0, 3);
            return result;
        }

        public override bool ShouldAvoid(int value) => Terrain.ExtractData(value) > 0;

        public override int GetEmittedLightAmount(int value) {
            int num = Terrain.ExtractData(value);
            if (num > 0) {
                return MathUtils.Min(8 + num / 2, 15);
            }
            return 0;
        }

        public override float GetHeat(int value) {
            if (Terrain.ExtractData(value) <= 0) {
                return 0f;
            }
            return base.GetHeat(value);
        }
    }
}