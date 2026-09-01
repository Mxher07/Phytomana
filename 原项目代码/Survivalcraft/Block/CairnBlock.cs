using Engine;
using Engine.Graphics;

namespace Game {
    public class CairnBlock : Block {
        public static int Index = 258;

        public BlockMesh m_mesh = new();

        public BlockMesh m_standaloneMesh = new();

        public BoundingBox[] m_collisionBoxes;

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Cairn");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Cairn").ParentBone);
            Color white = Color.White;
            BlockMesh blockMesh = new();
            blockMesh.AppendModelMeshPart(
                model.FindMesh("Cairn").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f),
                false,
                false,
                false,
                false,
                white
            );
            BlockMesh blockMesh2 = new();
            blockMesh2.AppendModelMeshPart(
                model.FindMesh("Wood").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f),
                false,
                false,
                false,
                false,
                white
            );
            m_mesh.AppendBlockMesh(blockMesh);
            m_mesh.AppendBlockMesh(blockMesh2);
            m_standaloneMesh.AppendModelMeshPart(
                model.FindMesh("Cairn").MeshParts[0],
                boneAbsoluteTransform
                * Matrix.CreateScale(1.3f)
                * Matrix.CreateRotationX(-(float)Math.PI / 2f)
                * Matrix.CreateTranslation(0f, 0f, 0f),
                false,
                false,
                true,
                false,
                white
            );
            m_standaloneMesh.AppendModelMeshPart(
                model.FindMesh("Wood").MeshParts[0],
                boneAbsoluteTransform
                * Matrix.CreateScale(1.3f)
                * Matrix.CreateRotationX(-(float)Math.PI / 2f)
                * Matrix.CreateTranslation(0f, 0f, 0f),
                false,
                false,
                true,
                false,
                white
            );
            m_collisionBoxes = [blockMesh.CalculateBoundingBox()];
            base.Initialize();
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) => m_collisionBoxes;

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            TerrainGeometrySubset subsetOpaque = texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque;
            generator.GenerateMeshVertices(
                this,
                x,
                y,
                z,
                m_mesh,
                Color.White,
                null,
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
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneMesh, color, size, ref matrix, environmentData);
            }
            else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneMesh, texture, color, size, ref matrix, environmentData);
            }
        }

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            int num = Terrain.ExtractData(oldValue);
            int num2 = 10 + 4 * num;
            int num3 = num >= 3 ? 1 : 0;
            BlockDropValue item;
            for (int i = 0; i < 3; i++) {
                item = new BlockDropValue { Value = 79, Count = 1 };
                dropValues.Add(item);
            }
            for (int j = 0; j < num2; j++) {
                item = new BlockDropValue { Value = 248, Count = 1 };
                dropValues.Add(item);
            }
            for (int k = 0; k < num3; k++) {
                item = new BlockDropValue { Value = 111, Count = 1 };
                dropValues.Add(item);
            }
            for (int l = 0; l < 2; l++) {
                item = new BlockDropValue { Value = 23, Count = 1 };
                dropValues.Add(item);
            }
            showDebris = false;
        }
    }
}