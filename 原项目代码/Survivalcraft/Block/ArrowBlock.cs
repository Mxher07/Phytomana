using Engine;
using Engine.Graphics;

namespace Game {
    public class ArrowBlock : Block {
        public enum ArrowType {
            WoodenArrow = 0,
            StoneArrow = 1,
            IronArrow = 2,
            DiamondArrow = 3,
            FireArrow = 4,
            IronBolt = 5,
            DiamondBolt = 6,
            ExplosiveBolt = 7,
            CopperArrow = 8
        }

        public static int Index = 192;

        public List<BlockMesh> m_standaloneBlockMeshes = [];

        public static int[] m_order = [
            0,
            1,
            8,
            2,
            3,
            4,
            5,
            6,
            7
        ];

        public static string[] m_tipNames = [
            "ArrowTip",
            "ArrowTip",
            "ArrowTip",
            "ArrowTip",
            "ArrowFireTip",
            "BoltTip",
            "BoltTip",
            "BoltExplosiveTip",
            "ArrowTip"
        ];

        public static int[] m_tipTextureSlots = [
            47,
            1,
            63,
            182,
            62,
            63,
            182,
            183,
            79
        ];

        public static string[] m_shaftNames = [
            "ArrowShaft",
            "ArrowShaft",
            "ArrowShaft",
            "ArrowShaft",
            "ArrowShaft",
            "BoltShaft",
            "BoltShaft",
            "BoltShaft",
            "ArrowShaft"
        ];

        public static int[] m_shaftTextureSlots = [
            4,
            4,
            4,
            4,
            4,
            63,
            63,
            63,
            4
        ];

        public static string[] m_stabilizerNames = [
            "ArrowStabilizer",
            "ArrowStabilizer",
            "ArrowStabilizer",
            "ArrowStabilizer",
            "ArrowStabilizer",
            "BoltStabilizer",
            "BoltStabilizer",
            "BoltStabilizer",
            "ArrowStabilizer"
        ];

        public static int[] m_stabilizerTextureSlots = [
            15,
            15,
            15,
            15,
            15,
            63,
            63,
            63,
            15
        ];

        public static float[] m_offsets = [
            -0.5f,
            -0.5f,
            -0.5f,
            -0.5f,
            -0.5f,
            -0.3f,
            -0.3f,
            -0.3f,
            -0.5f
        ];

        public static float[] m_weaponPowers = [
            5f,
            7f,
            14f,
            18f,
            4f,
            28f,
            36f,
            8f,
            10f
        ];

        public static float[] m_iconViewScales = [
            0.8f,
            0.8f,
            0.8f,
            0.8f,
            0.8f,
            1.1f,
            1.1f,
            1.1f,
            0.8f
        ];

        public static float[] m_explosionPressures = [
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            40f,
            0f
        ];

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Arrows");
            foreach (int enumValue in EnumUtils.GetEnumValues<ArrowType>()) {
                if (enumValue > 15) {
                    throw new InvalidOperationException("Too many arrow types.");
                }
                Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(m_shaftNames[enumValue]).ParentBone);
                Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(m_stabilizerNames[enumValue]).ParentBone);
                Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(m_tipNames[enumValue]).ParentBone);
                BlockMesh blockMesh = new();
                blockMesh.AppendModelMeshPart(
                    model.FindMesh(m_tipNames[enumValue]).MeshParts[0],
                    boneAbsoluteTransform3 * Matrix.CreateTranslation(0f, m_offsets[enumValue], 0f),
                    false,
                    false,
                    false,
                    false,
                    Color.White
                );
                blockMesh.TransformTextureCoordinates(
                    Matrix.CreateTranslation(m_tipTextureSlots[enumValue] % 16 / 16f, m_tipTextureSlots[enumValue] / 16 / 16f, 0f)
                );
                BlockMesh blockMesh2 = new();
                blockMesh2.AppendModelMeshPart(
                    model.FindMesh(m_shaftNames[enumValue]).MeshParts[0],
                    boneAbsoluteTransform * Matrix.CreateTranslation(0f, m_offsets[enumValue], 0f),
                    false,
                    false,
                    false,
                    false,
                    Color.White
                );
                blockMesh2.TransformTextureCoordinates(
                    Matrix.CreateTranslation(m_shaftTextureSlots[enumValue] % 16 / 16f, m_shaftTextureSlots[enumValue] / 16 / 16f, 0f)
                );
                BlockMesh blockMesh3 = new();
                blockMesh3.AppendModelMeshPart(
                    model.FindMesh(m_stabilizerNames[enumValue]).MeshParts[0],
                    boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, m_offsets[enumValue], 0f),
                    false,
                    false,
                    true,
                    false,
                    Color.White
                );
                blockMesh3.TransformTextureCoordinates(
                    Matrix.CreateTranslation(m_stabilizerTextureSlots[enumValue] % 16 / 16f, m_stabilizerTextureSlots[enumValue] / 16 / 16f, 0f)
                );
                BlockMesh blockMesh4 = new();
                blockMesh4.AppendBlockMesh(blockMesh);
                blockMesh4.AppendBlockMesh(blockMesh2);
                blockMesh4.AppendBlockMesh(blockMesh3);
                m_standaloneBlockMeshes.Add(blockMesh4);
            }
            base.Initialize();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) { }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            int arrowType = (int)GetArrowType(Terrain.ExtractData(value));
            if (arrowType >= 0
                && arrowType < m_standaloneBlockMeshes.Count) {
                Texture2D texture = GetDefaultTexture(value);
                if (texture == null) {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshes[arrowType], color, 2f * size, ref matrix, environmentData);
                } else {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshes[arrowType], texture, color, 2f * size, ref matrix, environmentData);
                }
            }
        }

        public override float GetProjectilePower(int value) {
            int arrowType = (int)GetArrowType(Terrain.ExtractData(value));
            if (arrowType < 0
                || arrowType >= m_weaponPowers.Length) {
                return 0f;
            }
            return m_weaponPowers[arrowType];
        }

        public override float GetExplosionPressure(int value) {
            int arrowType = (int)GetArrowType(Terrain.ExtractData(value));
            if (arrowType < 0
                || arrowType >= m_explosionPressures.Length) {
                return 0f;
            }
            return m_explosionPressures[arrowType];
        }

        public override float GetIconViewScale(int value, DrawBlockEnvironmentData environmentData) {
            int arrowType = (int)GetArrowType(Terrain.ExtractData(value));
            if (arrowType < 0
                || arrowType >= m_iconViewScales.Length) {
                return 1f;
            }
            return m_iconViewScales[arrowType];
        }

        public override IEnumerable<int> GetCreativeValues() {
            int i = 0;
            while (i < m_order.Length) {
                yield return Terrain.MakeBlockValue(BlockIndex, 0, SetArrowType(0, (ArrowType)m_order[i]));
                int num = i + 1;
                i = num;
            }
        }

        public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value) {
            int arrowType = (int)GetArrowType(Terrain.ExtractData(value));
            if (arrowType < 0
                || arrowType >= Enum.GetValues<ArrowType>().Length) {
                return string.Empty;
            }
            return LanguageControl.Get("ArrowBlock", arrowType);
        }

        public static ArrowType GetArrowType(int data) => (ArrowType)(data & 0xF);

        public static int SetArrowType(int data, ArrowType arrowType) => (data & -16) | ((int)arrowType & 15);
    }
}