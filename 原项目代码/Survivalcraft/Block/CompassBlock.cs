using Engine;
using Engine.Graphics;

namespace Game {
    public class CompassBlock : Block {
        public static int Index = 117;

        public BlockMesh m_caseMesh = new();

        public BlockMesh m_pointerMesh = new();

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Compass");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Case").ParentBone);
            Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Pointer").ParentBone);
            m_caseMesh.AppendModelMeshPart(
                model.FindMesh("Case").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.01f, 0f),
                false,
                false,
                true,
                false,
                Color.White
            );
            m_pointerMesh.AppendModelMeshPart(
                model.FindMesh("Pointer").MeshParts[0],
                boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.01f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            base.Initialize();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) { }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            float radians = 0f;
            if (environmentData != null
                && environmentData.SubsystemTerrain != null) {
                Vector3 forward = environmentData.InWorldMatrix.Forward;
                Vector3 translation = environmentData.InWorldMatrix.Translation;
                Vector3 v = environmentData.SubsystemTerrain.Project.FindSubsystem<SubsystemMagnetBlockBehavior>(true)
                    .FindNearestCompassTarget(translation);
                Vector3 vector = translation - v;
                radians = Vector2.Angle(v2: new Vector2(forward.X, forward.Z), v1: new Vector2(vector.X, vector.Z));
            }
            Matrix matrix2 = matrix;
            Matrix matrix3 = Matrix.CreateRotationY(radians) * matrix;
            Texture2D texture = GetDefaultTexture(value);
            if (texture == null) {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_caseMesh, color, size * 6f, ref matrix2, environmentData);
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_pointerMesh, color, size * 6f, ref matrix3, environmentData);
            }
            else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_caseMesh, texture, color, size * 6f, ref matrix2, environmentData);
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_pointerMesh, texture, color, size * 6f, ref matrix3, environmentData);
            }
        }
    }
}