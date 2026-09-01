using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class FenceGateBlock : Block, IElectricElementBlock, IPaintableBlock {
        public float m_pivotDistance;

        public string m_modelName;

        public bool m_doubleSided;

        public bool m_useAlphaTest;

        public int m_coloredTextureSlot;

        public Color m_postColor;

        public Color m_unpaintedColor;

        public BlockMesh m_standaloneBlockMesh = new();

        public BlockMesh m_standaloneColoredBlockMesh = new();

        public BlockMesh[] m_blockMeshes = new BlockMesh[16];

        public BlockMesh[] m_coloredBlockMeshes = new BlockMesh[16];

        public BoundingBox[][] m_collisionBoxes = new BoundingBox[16][];

        public FenceGateBlock(string modelName,
            float pivotDistance,
            bool doubleSided,
            bool useAlphaTest,
            int coloredTextureSlot,
            Color postColor,
            Color unpaintedColor) {
            m_modelName = modelName;
            m_pivotDistance = pivotDistance;
            m_doubleSided = doubleSided;
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
                int rotation = GetRotation(i);
                bool open = GetOpen(i);
                bool rightHanded = GetRightHanded(i);
                float num = !rightHanded ? 1 : -1;
                Matrix identity = Matrix.Identity;
                identity *= Matrix.CreateScale(0f - num, 1f, 1f);
                identity *= Matrix.CreateTranslation((0.5f - m_pivotDistance) * num, 0f, 0f)
                    * Matrix.CreateRotationY(open ? num * (float)Math.PI / 2f : 0f)
                    * Matrix.CreateTranslation((0f - (0.5f - m_pivotDistance)) * num, 0f, 0f);
                identity *= Matrix.CreateTranslation(0f, 0f, 0f)
                    * Matrix.CreateRotationY(rotation * (float)Math.PI / 2f)
                    * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
                m_blockMeshes[i] = new BlockMesh();
                m_blockMeshes[i]
                    .AppendModelMeshPart(
                        model.FindMesh("Post").MeshParts[0],
                        boneAbsoluteTransform * identity,
                        false,
                        !rightHanded,
                        false,
                        false,
                        m_postColor
                    );
                m_blockMeshes[i]
                    .AppendModelMeshPart(
                        model.FindMesh("Planks").MeshParts[0],
                        boneAbsoluteTransform2 * identity,
                        false,
                        !rightHanded,
                        false,
                        false,
                        Color.White
                    );
                if (m_doubleSided) {
                    m_blockMeshes[i]
                        .AppendModelMeshPart(
                            model.FindMesh("Planks").MeshParts[0],
                            boneAbsoluteTransform2 * identity,
                            false,
                            rightHanded,
                            false,
                            true,
                            Color.White
                        );
                }
                m_coloredBlockMeshes[i] = new BlockMesh();
                m_coloredBlockMeshes[i].AppendBlockMesh(m_blockMeshes[i]);
                m_blockMeshes[i]
                    .TransformTextureCoordinates(Matrix.CreateTranslation(DefaultTextureSlot % 16 / 16f, DefaultTextureSlot / 16 / 16f, 0f));
                m_coloredBlockMeshes[i]
                    .TransformTextureCoordinates(Matrix.CreateTranslation(m_coloredTextureSlot % 16 / 16f, m_coloredTextureSlot / 16 / 16f, 0f));
                BoundingBox boundingBox = m_blockMeshes[i].CalculateBoundingBox();
                boundingBox.Min.X = MathUtils.Saturate(boundingBox.Min.X);
                boundingBox.Min.Y = MathUtils.Saturate(boundingBox.Min.Y);
                boundingBox.Min.Z = MathUtils.Saturate(boundingBox.Min.Z);
                boundingBox.Max.X = MathUtils.Saturate(boundingBox.Max.X);
                boundingBox.Max.Y = MathUtils.Saturate(boundingBox.Max.Y);
                boundingBox.Max.Z = MathUtils.Saturate(boundingBox.Max.Z);
                m_collisionBoxes[i] = [boundingBox];
            }
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Post").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false,
                false,
                false,
                false,
                m_postColor
            );
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Planks").MeshParts[0],
                boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            if (m_doubleSided) {
                m_standaloneBlockMesh.AppendModelMeshPart(
                    model.FindMesh("Planks").MeshParts[0],
                    boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.5f, 0f),
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

        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
            float num = Vector3.Dot(forward, Vector3.UnitZ);
            float num2 = Vector3.Dot(forward, Vector3.UnitX);
            float num3 = Vector3.Dot(forward, -Vector3.UnitZ);
            float num4 = Vector3.Dot(forward, -Vector3.UnitX);
            int num5 = 0;
            if (num == MathUtils.Max(num, num2, num3, num4)) {
                num5 = 2;
            }
            else if (num2 == MathUtils.Max(num, num2, num3, num4)) {
                num5 = 3;
            }
            else if (num3 == MathUtils.Max(num, num2, num3, num4)) {
                num5 = 0;
            }
            else if (num4 == MathUtils.Max(num, num2, num3, num4)) {
                num5 = 1;
            }
            Point3 point = CellFace.FaceToPoint3(raycastResult.CellFace.Face);
            int num6 = raycastResult.CellFace.X + point.X;
            int y = raycastResult.CellFace.Y + point.Y;
            int num7 = raycastResult.CellFace.Z + point.Z;
            int num8 = 0;
            int num9 = 0;
            switch (num5) {
                case 0: num8 = -1; break;
                case 1: num9 = 1; break;
                case 2: num8 = 1; break;
                default: num9 = -1; break;
            }
            int cellValue = subsystemTerrain.Terrain.GetCellValue(num6 + num8, y, num7 + num9);
            int cellValue2 = subsystemTerrain.Terrain.GetCellValue(num6 - num8, y, num7 - num9);
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
            Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(cellValue2)];
            int data = Terrain.ExtractData(cellValue);
            int data2 = Terrain.ExtractData(cellValue2);
            int data3 = SetRightHanded(
                rightHanded: (block is FenceGateBlock && GetRotation(data) == num5)
                || ((!(block2 is FenceGateBlock) || GetRotation(data2) != num5) && !block.IsCollidable_(cellValue)),
                data: SetOpen(SetRotation(Terrain.ExtractData(value), num5), false)
            );
            BlockPlacementData result = default;
            result.Value = Terrain.ReplaceData(Terrain.ReplaceContents(BlockIndex), data3);
            result.CellFace = raycastResult.CellFace;
            return result;
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
            TerrainGeometrySubset subsetOpaque = texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque;
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
            generator.GenerateWireVertices(
                value,
                x,
                y,
                z,
                GetHingeFace(data),
                m_pivotDistance * 2f,
                Vector2.Zero,
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

        public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) {
            int data = Terrain.ExtractData(value);
            return new FenceGateElectricElement(subsystemElectricity, new CellFace(x, y, z, GetHingeFace(data)));
        }

        public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain,
            int value,
            int face,
            int connectorFace,
            int x,
            int y,
            int z) {
            int hingeFace = GetHingeFace(Terrain.ExtractData(value));
            if (face == hingeFace) {
                return ElectricConnectorType.Input;
            }
            return null;
        }

        public int GetConnectionMask(int value) => int.MaxValue;

        public static int GetRotation(int data) => data & 3;

        public static bool GetOpen(int data) => (data & 4) != 0;

        public static bool GetRightHanded(int data) => (data & 8) == 0;

        public static int SetRotation(int data, int rotation) => (data & -4) | (rotation & 3);

        public static int SetOpen(int data, bool open) {
            if (!open) {
                return data & -5;
            }
            return data | 4;
        }

        public static int SetRightHanded(int data, bool rightHanded) {
            if (rightHanded) {
                return data & -9;
            }
            return data | 8;
        }

        public static int GetHingeFace(int data) {
            int rotation = GetRotation(data);
            int num = rotation - 1 < 0 ? 3 : rotation - 1;
            if (!GetRightHanded(data)) {
                num = CellFace.OppositeFace(num);
            }
            return num;
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