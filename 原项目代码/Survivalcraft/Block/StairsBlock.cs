using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class StairsBlock : Block, IPaintableBlock {
        public enum CornerType {
            None = 0,
            OneQuarter = 1,
            ThreeQuarters = 2
        }

        public BlockMesh m_standaloneUncoloredBlockMesh = new();

        public BlockMesh m_standaloneColoredBlockMesh = new();

        public BlockMesh[] m_uncoloredBlockMeshes = new BlockMesh[24];

        public BlockMesh[] m_coloredBlockMeshes = new BlockMesh[24];

        public BoundingBox[][] m_collisionBoxes = new BoundingBox[24][];

        public int m_coloredTextureSlot;

        public StairsBlock(int coloredTextureSlot) => m_coloredTextureSlot = coloredTextureSlot;

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Stairs");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Stairs").ParentBone);
            Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("StairsOuterCorner").ParentBone);
            Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("StairsInnerCorner").ParentBone);
            for (int i = 0; i < 24; i++) {
                int rotation = GetRotation(i);
                bool isUpsideDown = GetIsUpsideDown(i);
                CornerType cornerType = GetCornerType(i);
                Matrix m = !isUpsideDown
                    ? Matrix.CreateRotationY(rotation * (float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f)
                    : Matrix.CreateRotationY(rotation * (float)Math.PI / 2f)
                    * Matrix.CreateTranslation(0.5f, -0.5f, 0.5f)
                    * Matrix.CreateScale(1f, -1f, 1f)
                    * Matrix.CreateTranslation(0f, 0.5f, 0f);
                BlockMesh blockMesh = new();
                switch (cornerType) {
                    case CornerType.None:
                        blockMesh.AppendModelMeshPart(
                            model.FindMesh("Stairs").MeshParts[0],
                            boneAbsoluteTransform * m,
                            false,
                            isUpsideDown,
                            false,
                            false,
                            Color.White
                        ); break;
                    case CornerType.OneQuarter:
                        blockMesh.AppendModelMeshPart(
                            model.FindMesh("StairsOuterCorner").MeshParts[0],
                            boneAbsoluteTransform2 * m,
                            false,
                            isUpsideDown,
                            false,
                            false,
                            Color.White
                        ); break;
                    case CornerType.ThreeQuarters:
                        blockMesh.AppendModelMeshPart(
                            model.FindMesh("StairsInnerCorner").MeshParts[0],
                            boneAbsoluteTransform3 * m,
                            false,
                            isUpsideDown,
                            false,
                            false,
                            Color.White
                        ); break;
                }
                float num = isUpsideDown ? rotation : -rotation;
                blockMesh.TransformTextureCoordinates(
                    Matrix.CreateTranslation(-0.03125f, -0.03125f, 0f)
                    * Matrix.CreateRotationZ(num * (float)Math.PI / 2f)
                    * Matrix.CreateTranslation(0.03125f, 0.03125f, 0f),
                    16
                );
                blockMesh.TransformTextureCoordinates(
                    Matrix.CreateTranslation(-0.03125f, -0.03125f, 0f)
                    * Matrix.CreateRotationZ((0f - num) * (float)Math.PI / 2f)
                    * Matrix.CreateTranslation(0.03125f, 0.03125f, 0f),
                    32
                );
                if (isUpsideDown) {
                    blockMesh.TransformTextureCoordinates(
                        Matrix.CreateTranslation(-0.03125f, -0.03125f, 0f)
                        * Matrix.CreateScale(1f, -1f, 1f)
                        * Matrix.CreateTranslation(0.03125f, 0.03125f, 0f)
                    );
                }
                m_coloredBlockMeshes[i] = new BlockMesh();
                m_coloredBlockMeshes[i].AppendBlockMesh(blockMesh);
                m_coloredBlockMeshes[i]
                    .TransformTextureCoordinates(Matrix.CreateTranslation(m_coloredTextureSlot % 16 / 16f, m_coloredTextureSlot / 16 / 16f, 0f));
                m_coloredBlockMeshes[i].GenerateSidesData();
                m_uncoloredBlockMeshes[i] = new BlockMesh();
                m_uncoloredBlockMeshes[i].AppendBlockMesh(blockMesh);
                m_uncoloredBlockMeshes[i]
                    .TransformTextureCoordinates(Matrix.CreateTranslation(DefaultTextureSlot % 16 / 16f, DefaultTextureSlot / 16 / 16f, 0f));
                m_uncoloredBlockMeshes[i].GenerateSidesData();
            }
            m_standaloneUncoloredBlockMesh.AppendModelMeshPart(
                model.FindMesh("Stairs").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            m_standaloneUncoloredBlockMesh.TransformTextureCoordinates(
                Matrix.CreateTranslation(DefaultTextureSlot % 16 / 16f, DefaultTextureSlot / 16 / 16f, 0f)
            );
            m_standaloneColoredBlockMesh.AppendModelMeshPart(
                model.FindMesh("Stairs").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            m_standaloneColoredBlockMesh.TransformTextureCoordinates(
                Matrix.CreateTranslation(m_coloredTextureSlot % 16 / 16f, m_coloredTextureSlot / 16 / 16f, 0f)
            );
            m_collisionBoxes[0] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 0.5f)),
                new BoundingBox(new Vector3(0f, 0f, 0.5f), new Vector3(1f, 0.5f, 1f))
            ];
            m_collisionBoxes[1] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(0.5f, 1f, 1f)),
                new BoundingBox(new Vector3(0.5f, 0f, 0f), new Vector3(1f, 0.5f, 1f))
            ];
            m_collisionBoxes[2] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 0.5f)),
                new BoundingBox(new Vector3(0f, 0f, 0.5f), new Vector3(1f, 1f, 1f))
            ];
            m_collisionBoxes[3] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0.5f, 1f)),
                new BoundingBox(new Vector3(0.5f, 0f, 0f), new Vector3(1f, 1f, 1f))
            ];
            m_collisionBoxes[4] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 0.5f)),
                new BoundingBox(new Vector3(0f, 0.5f, 0.5f), new Vector3(1f, 1f, 1f))
            ];
            m_collisionBoxes[5] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(0.5f, 1f, 1f)),
                new BoundingBox(new Vector3(0.5f, 0.5f, 0f), new Vector3(1f, 1f, 1f))
            ];
            m_collisionBoxes[6] = [
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 0.5f)),
                new BoundingBox(new Vector3(0f, 0f, 0.5f), new Vector3(1f, 1f, 1f))
            ];
            m_collisionBoxes[7] = [
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 1f, 1f)),
                new BoundingBox(new Vector3(0.5f, 0f, 0f), new Vector3(1f, 1f, 1f))
            ];
            m_collisionBoxes[8] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f)),
                new BoundingBox(new Vector3(0.5f, 0.5f, 0f), new Vector3(1f, 1f, 0.5f))
            ];
            m_collisionBoxes[9] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f)),
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 1f, 0.5f))
            ];
            m_collisionBoxes[10] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f)),
                new BoundingBox(new Vector3(0f, 0.5f, 0.5f), new Vector3(0.5f, 1f, 1f))
            ];
            m_collisionBoxes[11] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f)),
                new BoundingBox(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1f, 1f, 1f))
            ];
            m_collisionBoxes[12] = [
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f)),
                new BoundingBox(new Vector3(0.5f, 0f, 0f), new Vector3(1f, 0.5f, 0.5f))
            ];
            m_collisionBoxes[13] = [
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f)),
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0.5f, 0.5f))
            ];
            m_collisionBoxes[14] = [
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f)),
                new BoundingBox(new Vector3(0f, 0f, 0.5f), new Vector3(0.5f, 0.5f, 1f))
            ];
            m_collisionBoxes[15] = [
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f)),
                new BoundingBox(new Vector3(0.5f, 0f, 0.5f), new Vector3(1f, 0.5f, 1f))
            ];
            m_collisionBoxes[16] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f)),
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 0.5f)),
                new BoundingBox(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1f, 1f, 1f))
            ];
            m_collisionBoxes[17] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f)),
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 0.5f)),
                new BoundingBox(new Vector3(0f, 0.5f, 0.5f), new Vector3(0.5f, 1f, 1f))
            ];
            m_collisionBoxes[18] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f)),
                new BoundingBox(new Vector3(0f, 0.5f, 0.5f), new Vector3(1f, 1f, 1f)),
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 1f, 0.5f))
            ];
            m_collisionBoxes[19] = [
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f)),
                new BoundingBox(new Vector3(0f, 0.5f, 0.5f), new Vector3(1f, 1f, 1f)),
                new BoundingBox(new Vector3(0.5f, 0.5f, 0f), new Vector3(1f, 1f, 0.5f))
            ];
            m_collisionBoxes[20] = [
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f)),
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 0.5f)),
                new BoundingBox(new Vector3(0.5f, 0f, 0.5f), new Vector3(1f, 0.5f, 1f))
            ];
            m_collisionBoxes[21] = [
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f)),
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 0.5f)),
                new BoundingBox(new Vector3(0f, 0f, 0.5f), new Vector3(0.5f, 0.5f, 1f))
            ];
            m_collisionBoxes[22] = [
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f)),
                new BoundingBox(new Vector3(0f, 0f, 0.5f), new Vector3(1f, 0.5f, 1f)),
                new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0.5f, 0.5f))
            ];
            m_collisionBoxes[23] = [
                new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f)),
                new BoundingBox(new Vector3(0f, 0f, 0.5f), new Vector3(1f, 0.5f, 1f)),
                new BoundingBox(new Vector3(0.5f, 0f, 0f), new Vector3(1f, 0.5f, 0.5f))
            ];
            base.Initialize();
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

        public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value) {
            int data = Terrain.ExtractData(value);
            bool isUpsideDown = GetIsUpsideDown(data);
            switch (face) {
                case 4: return !isUpsideDown;
                case 5: return isUpsideDown;
                default:
                    switch (GetCornerType(data)) {
                        case CornerType.None: {
                            int rotation2 = GetRotation(data);
                            return face != ((rotation2 + 2) & 3);
                        }
                        case CornerType.OneQuarter: return true;
                        default: {
                            int rotation = GetRotation(data);
                            if (face != ((rotation + 1) & 3)) {
                                return face != ((rotation + 2) & 3);
                            }
                            return false;
                        }
                    }
            }
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            int data = Terrain.ExtractData(value);
            int? color = GetColor(data);
            Texture2D texture = GetDefaultTexture(value);
            TerrainGeometrySubset subsetOpaque = texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque;
            if (color.HasValue) {
                generator.GenerateShadedMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_coloredBlockMeshes[GetVariant(data)],
                    SubsystemPalette.GetColor(generator, color),
                    null,
                    null,
                    subsetOpaque
                );
            }
            else {
                generator.GenerateShadedMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_uncoloredBlockMeshes[GetVariant(data)],
                    Color.White,
                    null,
                    null,
                    subsetOpaque
                );
            }
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
            int rotation = 0;
            if (num == MathUtils.Max(num, num2, num3, num4)) {
                rotation = 2;
            }
            else if (num2 == MathUtils.Max(num, num2, num3, num4)) {
                rotation = 3;
            }
            else if (num3 == MathUtils.Max(num, num2, num3, num4)) {
                rotation = 0;
            }
            else if (num4 == MathUtils.Max(num, num2, num3, num4)) {
                rotation = 1;
            }
            bool isUpsideDown = raycastResult.CellFace.Face == 5;
            int data = Terrain.ExtractData(value);
            BlockPlacementData result = default;
            result.Value = Terrain.MakeBlockValue(BlockIndex, 0, SetIsUpsideDown(SetRotation(data, rotation), isUpsideDown));
            result.CellFace = raycastResult.CellFace;
            return result;
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) {
            int data = Terrain.ExtractData(value);
            return m_collisionBoxes[GetVariant(data)];
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            int? color2 = GetColor(Terrain.ExtractData(value));
            Texture2D texture = GetDefaultTexture(value);
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
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneUncoloredBlockMesh, color, size, ref matrix, environmentData);
                }
                else {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneUncoloredBlockMesh, texture, color, size, ref matrix, environmentData);
                }
            }
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

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            showDebris = true;
            int data = Terrain.ExtractData(oldValue);
            int data2 = SetColor(0, GetColor(data));
            int value = Terrain.MakeBlockValue(BlockIndex, 0, data2);
            dropValues.Add(new BlockDropValue { Value = value, Count = 1 });
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
            return new BlockDebrisParticleSystem(
                subsystemTerrain,
                position,
                strength,
                DestructionDebrisScale,
                Color.White,
                base.GetFaceTextureSlot(0, value)
            );
        }

        public virtual int? GetPaintColor(int value) => GetColor(Terrain.ExtractData(value));

        public virtual int Paint(SubsystemTerrain terrain, int value, int? color) =>
            Terrain.MakeBlockValue(BlockIndex, 0, SetColor(Terrain.ExtractData(value), color));

        public static Point3 RotationToDirection(int rotation) => CellFace.FaceToPoint3((rotation + 2) % 4);

        public static int GetRotation(int data) => data & 3;

        public static int SetRotation(int data, int rotation) => (data & -4) | (rotation & 3);

        public static bool GetIsUpsideDown(int data) => (data & 4) != 0;

        public static int SetIsUpsideDown(int data, bool isUpsideDown) {
            if (isUpsideDown) {
                return data | 4;
            }
            return data & -5;
        }

        public static CornerType GetCornerType(int data) => (CornerType)((data >> 3) & 3);

        public static int SetCornerType(int data, CornerType cornerType) => (data & -25) | (((int)cornerType & 3) << 3);

        public static int? GetColor(int data) {
            if ((data & 0x20) != 0) {
                return (data >> 6) & 0xF;
            }
            return null;
        }

        public static int SetColor(int data, int? color) {
            if (color.HasValue) {
                return (data & -993) | 0x20 | ((color.Value & 0xF) << 6);
            }
            return data & -993;
        }

        public static int GetVariant(int data) => data & 0x1F;
    }
}