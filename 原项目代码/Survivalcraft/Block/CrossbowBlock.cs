using Engine;
using Engine.Graphics;

namespace Game {
    public class CrossbowBlock : Block {
        public static int Index = 200;

        public BlockMesh[] m_standaloneBlockMeshes = new BlockMesh[16];

        Block arrowBlock;

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Crossbows");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Body").ParentBone);
            Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("BowRelaxed").ParentBone);
            Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("StringRelaxed").ParentBone);
            Matrix boneAbsoluteTransform4 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("BowTensed").ParentBone);
            Matrix boneAbsoluteTransform5 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("StringTensed").ParentBone);
            BlockMesh blockMesh = new();
            blockMesh.AppendModelMeshPart(
                model.FindMesh("Body").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, 0f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            blockMesh.AppendModelMeshPart(
                model.FindMesh("BowRelaxed").MeshParts[0],
                boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, 0f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            blockMesh.AppendModelMeshPart(
                model.FindMesh("StringRelaxed").MeshParts[0],
                boneAbsoluteTransform3 * Matrix.CreateTranslation(0f, 0f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            BlockMesh blockMesh2 = new();
            blockMesh2.AppendModelMeshPart(
                model.FindMesh("Body").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, 0f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            blockMesh2.AppendModelMeshPart(
                model.FindMesh("BowTensed").MeshParts[0],
                boneAbsoluteTransform4 * Matrix.CreateTranslation(0f, 0f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            blockMesh2.AppendModelMeshPart(
                model.FindMesh("StringTensed").MeshParts[0],
                boneAbsoluteTransform5 * Matrix.CreateTranslation(0f, 0f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            for (int i = 0; i < 16; i++) {
                float factor = i / 15f;
                m_standaloneBlockMeshes[i] = new BlockMesh();
                m_standaloneBlockMeshes[i].AppendBlockMesh(blockMesh);
                m_standaloneBlockMeshes[i].BlendBlockMesh(blockMesh2, factor);
            }
            arrowBlock = BlocksManager.GetBlockGeneral<ArrowBlock>();
            base.Initialize();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) { }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            int data = Terrain.ExtractData(value);
            int draw = GetDraw(data);
            ArrowBlock.ArrowType? arrowType = GetArrowType(data);
            Texture2D texture = GetDefaultTexture(value);
            if (texture == null) {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshes[draw], color, 2f * size, ref matrix, environmentData);
            } else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshes[draw], texture, color, 2f * size, ref matrix, environmentData);
            }
            if (arrowType.HasValue) {
                Matrix matrix2 = Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0.2f * size, -0.09f * size) * matrix;
                int value2 = Terrain.MakeBlockValue(arrowBlock.BlockIndex, 0, ArrowBlock.SetArrowType(0, arrowType.Value));
                arrowBlock.DrawBlock(primitivesRenderer, value2, color, size, ref matrix2, environmentData);
            }
        }

        public override int GetDamage(int value) => (Terrain.ExtractData(value) >> 8) & 0xFF;

        public override int SetDamage(int value, int damage) {
            int num = Terrain.ExtractData(value);
            num &= -65281;
            num |= Math.Clamp(damage, 0, 255) << 8;
            return Terrain.ReplaceData(value, num);
        }

        public override bool IsSwapAnimationNeeded(int oldValue, int newValue) {
            int num = Terrain.ExtractContents(oldValue);
            int data = Terrain.ExtractData(oldValue);
            int data2 = Terrain.ExtractData(newValue);
            if (num == BlockIndex
                && GetArrowType(data) == GetArrowType(data2)) {
                return false;
            }
            return true;
        }

        public static ArrowBlock.ArrowType? GetArrowType(int data) {
            int num = (data >> 4) & 0xF;
            if (num != 0) {
                return (ArrowBlock.ArrowType)(num - 1);
            }
            return null;
        }

        public static int SetArrowType(int data, ArrowBlock.ArrowType? arrowType) {
            int num = (int)(arrowType.HasValue ? arrowType.Value + 1 : ArrowBlock.ArrowType.WoodenArrow);
            return (data & -241) | ((num & 0xF) << 4);
        }

        public static int GetDraw(int data) => data & 0xF;

        public static int SetDraw(int data, int draw) => (data & -16) | (draw & 0xF);
    }
}