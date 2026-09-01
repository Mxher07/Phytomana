using Engine;
using Engine.Graphics;

namespace Game {
    public class GrassTrapBlock : Block {
        public static int Index = 87;

        public BlockMesh m_blockMesh = new();

        public BlockMesh m_standaloneBlockMesh = new();

        public BoundingBox[] m_collisionBoxes = new BoundingBox[1];

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/GrassTrap");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("GrassTrap").ParentBone);
            Color color = BlockColorsMap.Grass.Lookup(8, 15);
            m_blockMesh.AppendModelMeshPart(
                model.FindMesh("GrassTrap").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, 0.75f, 0.5f),
                false,
                false,
                false,
                false,
                Color.White
            );
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("GrassTrap").MeshParts[0],
                boneAbsoluteTransform,
                false,
                false,
                false,
                false,
                color
            );
            m_collisionBoxes[0] = new BoundingBox(new Vector3(0f, 0.75f, 0f), new Vector3(1f, 0.95f, 1f));
            base.Initialize();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            TerrainGeometrySubset subsetAlphaTest = texture == null ? geometry.SubsetAlphaTest : geometry.GetGeometry(texture).SubsetAlphaTest;
            generator.GenerateShadedMeshVertices(
                this,
                x,
                y,
                z,
                m_blockMesh,
                BlockColorsMap.Grass.Lookup(generator.Terrain, x, y, z),
                null,
                null,
                subsetAlphaTest
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

        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) {
            Color color = BlockColorsMap.Grass.Lookup(
                subsystemTerrain.Terrain,
                Terrain.ToCell(position.X),
                Terrain.ToCell(position.Y),
                Terrain.ToCell(position.Z)
            );
            return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color, GetFaceTextureSlot(4, value));
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) => m_collisionBoxes;
    }
}