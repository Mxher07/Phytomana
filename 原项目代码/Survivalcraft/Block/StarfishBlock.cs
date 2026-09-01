using Engine;
using Engine.Graphics;

namespace Game {
    public class StarfishBlock : BottomSuckerBlock {
        public new const int Index = 229;

        public BlockMesh[] m_blockMeshes = new BlockMesh[24];

        public BlockMesh m_standaloneBlockMesh;

        public BoundingBox[][] m_collisionBoxes = new BoundingBox[24][];

        public static Color[] m_colors = [new(100, 40, 20), new(90, 30, 20), new(100, 30, 30), new(80, 20, 10)];

        public static Vector2[] m_offsets = [
            0.15f * new Vector2(-0.8f, -1f), 0.15f * new Vector2(1f, -0.75f), 0.15f * new Vector2(-0.65f, 1f), 0.15f * new Vector2(0.9f, 0.7f)
        ];

        public override void Initialize() {
            int num = 63;
            Model model = ContentManager.Get<Model>("Models/Starfish");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Starfish").ParentBone);
            Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Bottom").ParentBone);
            for (int i = 0; i < 6; i++) {
                for (int j = 0; j < 4; j++) {
                    Vector2 zero = Vector2.Zero;
                    if (i < 4) {
                        zero.Y = i * (float)Math.PI / 2f;
                    }
                    else {
                        zero.X = i == 4 ? -(float)Math.PI / 2f : (float)Math.PI / 2f;
                    }
                    Matrix m = Matrix.CreateRotationX((float)Math.PI / 2f)
                        * Matrix.CreateRotationZ(0.3f + 2f * j)
                        * Matrix.CreateTranslation(m_offsets[j].X, m_offsets[j].Y, -0.49f)
                        * Matrix.CreateRotationX(zero.X)
                        * Matrix.CreateRotationY(zero.Y)
                        * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f);
                    int num2 = 4 * i + j;
                    m_blockMeshes[num2] = new BlockMesh();
                    m_blockMeshes[num2]
                        .AppendModelMeshPart(
                            model.FindMesh("Starfish").MeshParts[0],
                            boneAbsoluteTransform * m,
                            false,
                            false,
                            false,
                            false,
                            Color.White
                        );
                    m_blockMeshes[num2].TransformTextureCoordinates(Matrix.CreateTranslation(num % 16 / 16f, num / 16 / 16f, 0f));
                    m_collisionBoxes[num2] = [m_blockMeshes[num2].CalculateBoundingBox()];
                }
            }
            m_standaloneBlockMesh = new BlockMesh();
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Starfish").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.1f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Bottom").MeshParts[0],
                boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.1f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            m_standaloneBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(num % 16 / 16f, num / 16 / 16f, 0f));
            base.Initialize();
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) {
            int data = Terrain.ExtractData(value);
            int face = GetFace(data);
            int subvariant = GetSubvariant(data);
            return m_collisionBoxes[4 * face + subvariant];
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            int data = Terrain.ExtractData(value);
            int face = GetFace(data);
            int subvariant = GetSubvariant(data);
            Color color = m_colors[subvariant];
            generator.GenerateMeshVertices(
                this,
                x,
                y,
                z,
                m_blockMeshes[4 * face + subvariant],
                color,
                null,
                texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque
            );
            base.GenerateTerrainVertices(generator, geometry, value, x, y, z);
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            Texture2D texture = GetDefaultTexture(value);
            if (texture == null) {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color * m_colors[0], 3f * size, ref matrix, environmentData);
            }
            else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, texture, color * m_colors[0], 3f * size, ref matrix, environmentData);
            }
        }

        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) => new(subsystemTerrain, position, 0.75f * strength, DestructionDebrisScale, new Color(64, 64, 64), DefaultTextureSlot);
    }
}