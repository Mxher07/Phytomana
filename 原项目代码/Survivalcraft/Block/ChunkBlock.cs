using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class ChunkBlock : Block {
        public BlockMesh m_standaloneBlockMesh = new();

        public Matrix m_transform;

        public Matrix m_tcTransform;

        public Color m_color;

        public bool m_smooth;

        public ChunkBlock(Matrix transform, Matrix tcTransform, Color color, bool smooth) {
            m_transform = transform;
            m_tcTransform = tcTransform;
            m_color = color;
            m_smooth = smooth;
        }

        public override void Initialize() {
            Model model = m_smooth ? ContentManager.Get<Model>("Models/ChunkSmooth") : ContentManager.Get<Model>("Models/Chunk");
            Matrix matrix = BlockMesh.GetBoneAbsoluteTransform(model.Meshes[0].ParentBone) * m_transform;
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.Meshes[0].MeshParts[0],
                matrix,
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
    }
}