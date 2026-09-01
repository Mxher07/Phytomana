using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class FenceBlock : Block, IPaintableBlock {
        public string m_modelName;

        public bool m_doubleSidedPlanks;

        public bool m_useAlphaTest;

        public int m_coloredTextureSlot;

        public Color m_postColor;

        public Color m_unpaintedColor;

        public BlockMesh m_standaloneBlockMesh = new();

        public BlockMesh m_standaloneColoredBlockMesh = new();

        public BlockMesh[] m_blockMeshes = new BlockMesh[16];

        public BlockMesh[] m_coloredBlockMeshes = new BlockMesh[16];

        public BoundingBox[][] m_collisionBoxes = new BoundingBox[16][];

        public FenceBlock(string modelName,
            bool doubleSidedPlanks,
            bool useAlphaTest,
            int coloredTextureSlot,
            Color postColor,
            Color unpaintedColor) {
            m_modelName = modelName;
            m_doubleSidedPlanks = doubleSidedPlanks;
            m_useAlphaTest = useAlphaTest;
            m_coloredTextureSlot = coloredTextureSlot;
            m_postColor = postColor;
            m_unpaintedColor = unpaintedColor;
        }

        public override void Initialize() {
            Model model = ContentManager.Get<Model>(m_modelName);
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Post").ParentBone);
            Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Planks").ParentBone);
            for (int i = 0; i < 16; i++) {
                bool num = (i & 1) != 0;
                bool flag = (i & 2) != 0;
                bool flag2 = (i & 4) != 0;
                bool flag3 = (i & 8) != 0;
                List<BoundingBox> list = new();
                Matrix m = Matrix.CreateTranslation(0.5f, 0f, 0.5f);
                BlockMesh blockMesh = new();
                blockMesh.AppendModelMeshPart(
                    model.FindMesh("Post").MeshParts[0],
                    boneAbsoluteTransform * m,
                    false,
                    false,
                    false,
                    false,
                    Color.White
                );
                BoundingBox item = blockMesh.CalculateBoundingBox();
                item.Min.X -= 0.1f;
                item.Min.Z -= 0.1f;
                item.Max.X += 0.1f;
                item.Max.Z += 0.1f;
                list.Add(item);
                BlockMesh blockMesh2 = new();
                if (num) {
                    BlockMesh blockMesh3 = new();
                    Matrix m2 = Matrix.CreateRotationY(0f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
                    blockMesh3.AppendModelMeshPart(
                        model.FindMesh("Planks").MeshParts[0],
                        boneAbsoluteTransform2 * m2,
                        false,
                        false,
                        false,
                        false,
                        Color.White
                    );
                    if (m_doubleSidedPlanks) {
                        blockMesh3.AppendModelMeshPart(
                            model.FindMesh("Planks").MeshParts[0],
                            boneAbsoluteTransform2 * m2,
                            false,
                            true,
                            false,
                            true,
                            Color.White
                        );
                    }
                    blockMesh2.AppendBlockMesh(blockMesh3);
                    BoundingBox item2 = blockMesh3.CalculateBoundingBox();
                    list.Add(item2);
                }
                if (flag) {
                    BlockMesh blockMesh4 = new();
                    Matrix m3 = Matrix.CreateRotationY((float)Math.PI) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
                    blockMesh4.AppendModelMeshPart(
                        model.FindMesh("Planks").MeshParts[0],
                        boneAbsoluteTransform2 * m3,
                        false,
                        false,
                        false,
                        false,
                        Color.White
                    );
                    if (m_doubleSidedPlanks) {
                        blockMesh4.AppendModelMeshPart(
                            model.FindMesh("Planks").MeshParts[0],
                            boneAbsoluteTransform2 * m3,
                            false,
                            true,
                            false,
                            true,
                            Color.White
                        );
                    }
                    blockMesh2.AppendBlockMesh(blockMesh4);
                    BoundingBox item3 = blockMesh4.CalculateBoundingBox();
                    list.Add(item3);
                }
                if (flag2) {
                    BlockMesh blockMesh5 = new();
                    Matrix m4 = Matrix.CreateRotationY(4.712389f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
                    blockMesh5.AppendModelMeshPart(
                        model.FindMesh("Planks").MeshParts[0],
                        boneAbsoluteTransform2 * m4,
                        false,
                        false,
                        false,
                        false,
                        Color.White
                    );
                    if (m_doubleSidedPlanks) {
                        blockMesh5.AppendModelMeshPart(
                            model.FindMesh("Planks").MeshParts[0],
                            boneAbsoluteTransform2 * m4,
                            false,
                            true,
                            false,
                            true,
                            Color.White
                        );
                    }
                    blockMesh2.AppendBlockMesh(blockMesh5);
                    BoundingBox item4 = blockMesh5.CalculateBoundingBox();
                    list.Add(item4);
                }
                if (flag3) {
                    BlockMesh blockMesh6 = new();
                    Matrix m5 = Matrix.CreateRotationY((float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
                    blockMesh6.AppendModelMeshPart(
                        model.FindMesh("Planks").MeshParts[0],
                        boneAbsoluteTransform2 * m5,
                        false,
                        false,
                        false,
                        false,
                        Color.White
                    );
                    if (m_doubleSidedPlanks) {
                        blockMesh6.AppendModelMeshPart(
                            model.FindMesh("Planks").MeshParts[0],
                            boneAbsoluteTransform2 * m5,
                            false,
                            true,
                            false,
                            true,
                            Color.White
                        );
                    }
                    blockMesh2.AppendBlockMesh(blockMesh6);
                    BoundingBox item5 = blockMesh6.CalculateBoundingBox();
                    list.Add(item5);
                }
                blockMesh.ModulateColor(m_postColor);
                m_blockMeshes[i] = new BlockMesh();
                m_blockMeshes[i].AppendBlockMesh(blockMesh);
                m_blockMeshes[i].AppendBlockMesh(blockMesh2);
                m_blockMeshes[i]
                    .TransformTextureCoordinates(Matrix.CreateTranslation(DefaultTextureSlot % 16 / 16f, DefaultTextureSlot / 16 / 16f, 0f));
                m_blockMeshes[i].GenerateSidesData();
                m_coloredBlockMeshes[i] = new BlockMesh();
                m_coloredBlockMeshes[i].AppendBlockMesh(blockMesh);
                m_coloredBlockMeshes[i].AppendBlockMesh(blockMesh2);
                m_coloredBlockMeshes[i]
                    .TransformTextureCoordinates(Matrix.CreateTranslation(m_coloredTextureSlot % 16 / 16f, m_coloredTextureSlot / 16 / 16f, 0f));
                m_coloredBlockMeshes[i].GenerateSidesData();
                m_collisionBoxes[i] = list.ToArray();
            }
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Post").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(-0.5f, -0.5f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Post").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, -0.5f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Planks").MeshParts[0],
                boneAbsoluteTransform2 * Matrix.CreateRotationY(0f) * Matrix.CreateTranslation(-0.5f, -0.5f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            if (m_doubleSidedPlanks) {
                m_standaloneBlockMesh.AppendModelMeshPart(
                    model.FindMesh("Planks").MeshParts[0],
                    boneAbsoluteTransform2 * Matrix.CreateRotationY(0f) * Matrix.CreateTranslation(-0.5f, -0.5f, 0f),
                    false,
                    true,
                    false,
                    true,
                    Color.White
                );
            }
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Planks").MeshParts[0],
                boneAbsoluteTransform2 * Matrix.CreateRotationY((float)Math.PI) * Matrix.CreateTranslation(0.5f, -0.5f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            if (m_doubleSidedPlanks) {
                m_standaloneBlockMesh.AppendModelMeshPart(
                    model.FindMesh("Planks").MeshParts[0],
                    boneAbsoluteTransform2 * Matrix.CreateRotationY((float)Math.PI) * Matrix.CreateTranslation(0.5f, -0.5f, 0f),
                    false,
                    true,
                    false,
                    true,
                    Color.White
                );
            }
            m_standaloneColoredBlockMesh.AppendBlockMesh(m_standaloneBlockMesh);
            m_standaloneBlockMesh.TransformTextureCoordinates(
                Matrix.CreateTranslation(DefaultTextureSlot % 16 / 16f, DefaultTextureSlot / 16 / 16f, 0f)
            );
            m_standaloneColoredBlockMesh.TransformTextureCoordinates(
                Matrix.CreateTranslation(m_coloredTextureSlot % 16 / 16f, m_coloredTextureSlot / 16 / 16f, 0f)
            );
            base.Initialize();
        }

        public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value) {
            int? color = GetColor(Terrain.ExtractData(value));
            return SubsystemPalette.GetName(subsystemTerrain, color, base.GetDisplayName(subsystemTerrain, value));
        }

        public override string GetCategory(int value) {
            if (!GetColor(Terrain.ExtractData(value)).HasValue) {
                return base.GetCategory(value);
            }
            return "Painted";
        }

        public override IEnumerable<int> GetCreativeValues() {
            yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, null));
            int i = 0;
            while (i < 16) {
                yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, i));
                int num = i + 1;
                i = num;
            }
        }

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            showDebris = true;
            int data = SetVariant(Terrain.ExtractData(oldValue), 0);
            dropValues.Add(new BlockDropValue { Value = Terrain.MakeBlockValue(BlockIndex, 0, data), Count = 1 });
        }

        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) {
            int? color = GetColor(Terrain.ExtractData(value));
            if (color.HasValue) {
                return new BlockDebrisParticleSystem(
                    subsystemTerrain,
                    position,
                    strength,
                    DestructionDebrisScale,
                    SubsystemPalette.GetColor(subsystemTerrain, color),
                    m_coloredTextureSlot
                );
            }
            return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, Color.White, DefaultTextureSlot);
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            TerrainGeometrySubset subset = m_useAlphaTest
                ? (texture == null ? geometry.SubsetAlphaTest : geometry.GetGeometry(texture).SubsetAlphaTest)
                : (texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque);
            int data = Terrain.ExtractData(value);
            int variant = GetVariant(data);
            int? color = GetColor(data);
            if (color.HasValue) {
                generator.GenerateMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_coloredBlockMeshes[variant],
                    SubsystemPalette.GetColor(generator, color),
                    null,
                    subset
                );
            }
            else {
                generator.GenerateMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_blockMeshes[variant],
                    m_unpaintedColor,
                    null,
                    subset
                );
            }
        }

        public override bool IsFaceNonAttachable(SubsystemTerrain subsystemTerrain, int face, int value, int attachBlockValue) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(attachBlockValue)];
            if (block is BasePumpkinBlock) {
                return false;
            }
            return base.IsFaceNonAttachable(subsystemTerrain, face, value, attachBlockValue);
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            Texture2D texture = GetDefaultTexture(value);
            int? color2 = GetColor(Terrain.ExtractData(value));
            if (color2.HasValue) {
                if (texture == null) {
                    BlocksManager.DrawMeshBlock(
                        primitivesRenderer,
                        m_standaloneColoredBlockMesh,
                        color * SubsystemPalette.GetColor(environmentData, color2),
                        size,
                        ref matrix,
                        environmentData
                    );
                }
                else {
                    BlocksManager.DrawMeshBlock(
                        primitivesRenderer,
                        m_standaloneColoredBlockMesh,
                        texture,
                        color * SubsystemPalette.GetColor(environmentData, color2),
                        size,
                        ref matrix,
                        environmentData
                    );
                }
            }
            else {
                if (texture == null) {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color * m_unpaintedColor, size, ref matrix, environmentData);
                }
                else {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, texture, color * m_unpaintedColor, size, ref matrix, environmentData);
                }
            }
        }

        public int? GetPaintColor(int value) => GetColor(Terrain.ExtractData(value));

        public int Paint(SubsystemTerrain terrain, int value, int? color) {
            int data = Terrain.ExtractData(value);
            return Terrain.ReplaceData(value, SetColor(data, color));
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) {
            int variant = GetVariant(Terrain.ExtractData(value));
            return m_collisionBoxes[variant];
        }

        public virtual bool ShouldConnectTo(int value) {
            int num = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[num];
            if (!(block is FenceBlock)) {
                return block is FenceGateBlock;
            }
            return true;
        }

        public static int GetVariant(int data) => data & 0xF;

        public static int SetVariant(int data, int variant) => (data & -16) | (variant & 0xF);

        public static int? GetColor(int data) {
            if ((data & 0x10) != 0) {
                return (data >> 5) & 0xF;
            }
            return null;
        }

        public static int SetColor(int data, int? color) {
            if (color.HasValue) {
                return (data & -497) | 0x10 | ((color.Value & 0xF) << 5);
            }
            return data & -497;
        }
    }
}