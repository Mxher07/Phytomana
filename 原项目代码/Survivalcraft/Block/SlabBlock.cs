using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class SlabBlock : Block, IPaintableBlock {
        public int m_coloredTextureSlot;

        public int m_fullBlockIndex;

        public BlockMesh m_standaloneColoredBlockMesh = new();

        public BlockMesh m_standaloneUncoloredBlockMesh = new();

        public BlockMesh[] m_coloredBlockMeshes = new BlockMesh[2];

        public BlockMesh[] m_uncoloredBlockMeshes = new BlockMesh[2];

        public BoundingBox[][] m_collisionBoxes = new BoundingBox[2][];

        public SlabBlock(int coloredTextureSlot, int fullBlockIndex) {
            m_coloredTextureSlot = coloredTextureSlot;
            m_fullBlockIndex = fullBlockIndex;
        }

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Slab");
            ModelMeshPart meshPart = model.FindMesh("Slab").MeshParts[0];
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Slab").ParentBone);
            for (int i = 0; i < 2; i++) {
                Matrix matrix = boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, i == 0 ? 0f : 0.5f, 0.5f);
                m_uncoloredBlockMeshes[i] = new BlockMesh();
                m_uncoloredBlockMeshes[i]
                .AppendModelMeshPart(
                    meshPart,
                    matrix,
                    false,
                    false,
                    false,
                    false,
                    Color.White
                );
                m_uncoloredBlockMeshes[i]
                    .TransformTextureCoordinates(Matrix.CreateTranslation(DefaultTextureSlot % 16 / 16f, DefaultTextureSlot / 16 / 16f, 0f));
                m_uncoloredBlockMeshes[i].GenerateSidesData();
                m_coloredBlockMeshes[i] = new BlockMesh();
                m_coloredBlockMeshes[i]
                .AppendModelMeshPart(
                    meshPart,
                    matrix,
                    false,
                    false,
                    false,
                    false,
                    Color.White
                );
                m_coloredBlockMeshes[i]
                    .TransformTextureCoordinates(Matrix.CreateTranslation(m_coloredTextureSlot % 16 / 16f, m_coloredTextureSlot / 16 / 16f, 0f));
                m_coloredBlockMeshes[i].GenerateSidesData();
            }
            m_standaloneUncoloredBlockMesh.AppendModelMeshPart(
                meshPart,
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
                meshPart,
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
            m_collisionBoxes[0] = [new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f))];
            m_collisionBoxes[1] = [new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f))];
            base.Initialize();
        }

        public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value) {
            if (GetIsTop(Terrain.ExtractData(value))) {
                return face != 4;
            }
            return face != 5;
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            int data = Terrain.ExtractData(value);
            int num = GetIsTop(data) ? 1 : 0;
            int? color = GetColor(data);
            Texture2D texture = GetDefaultTexture(value);
            TerrainGeometrySubset subsetOpaque = texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque;
            if (color.HasValue) {
                generator.GenerateShadedMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_coloredBlockMeshes[num],
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
                    m_uncoloredBlockMeshes[num],
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
            int num = Terrain.ExtractContents(value);
            int data = Terrain.ExtractData(value);
            int num2 = Terrain.ExtractContents(raycastResult.Value);
            int data2 = Terrain.ExtractData(raycastResult.Value);
            BlockPlacementData result;
            if (num2 == num
                && ((GetIsTop(data2) && raycastResult.CellFace.Face == 5) || (!GetIsTop(data2) && raycastResult.CellFace.Face == 4))) {
                int value2 = Terrain.MakeBlockValue(m_fullBlockIndex, 0, 0);
                if (BlocksManager.Blocks[m_fullBlockIndex] is IPaintableBlock paintableBlock) {
                    int? color = GetColor(data);
                    value2 = paintableBlock.Paint(subsystemTerrain, value2, color);
                }
                CellFace cellFace = raycastResult.CellFace;
                cellFace.Point -= CellFace.FaceToPoint3(cellFace.Face);
                result = default;
                result.Value = value2;
                result.CellFace = cellFace;
                return result;
            }
            bool isTop = raycastResult.CellFace.Face >= 4
                ? raycastResult.CellFace.Face == 5
                : raycastResult.HitPoint().Y - raycastResult.CellFace.Y > 0.5f;
            result = default;
            result.Value = Terrain.MakeBlockValue(BlockIndex, 0, SetIsTop(data, isTop));
            result.CellFace = raycastResult.CellFace;
            return result;
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) {
            int num = GetIsTop(Terrain.ExtractData(value)) ? 1 : 0;
            return m_collisionBoxes[num];
        }

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            if (Terrain.ExtractContents(newValue) != m_fullBlockIndex) {
                int data = Terrain.ExtractData(oldValue);
                int data2 = SetColor(0, GetColor(data));
                int value = Terrain.MakeBlockValue(BlockIndex, 0, data2);
                dropValues.Add(new BlockDropValue { Value = value, Count = 1 });
                showDebris = true;
            }
            else {
                showDebris = false;
            }
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
                GetFaceTextureSlot(0, value)
            );
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

        public override IEnumerable<int> GetCreativeValues() {
            yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, null));
            int i = 0;
            while (i < 16) {
                yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, i));
                int num = i + 1;
                i = num;
            }
        }

        public virtual int? GetPaintColor(int value) => GetColor(Terrain.ExtractData(value));

        public virtual int Paint(SubsystemTerrain terrain, int value, int? color) {
            int data = Terrain.ExtractData(value);
            return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(data, color));
        }

        public static bool GetIsTop(int data) => (data & 1) != 0;

        public static int SetIsTop(int data, bool isTop) => (data & -2) | (isTop ? 1 : 0);

        public static int? GetColor(int data) {
            if ((data & 2) != 0) {
                return (data >> 2) & 0xF;
            }
            return null;
        }

        public static int SetColor(int data, int? color) {
            if (color.HasValue) {
                return (data & -63) | 2 | ((color.Value & 0xF) << 2);
            }
            return data & -63;
        }
    }
}