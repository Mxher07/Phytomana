using Engine;
using Engine.Graphics;

namespace Game {
    public class BulletBlock : FlatBlock {
        public enum BulletType {
            MusketBall = 0,
            Buckshot = 1,
            BuckshotBall = 2
        }

        public static int Index = 214;

        public static float[] m_sizes = [1f, 1f, 0.33f];

        public static int[] m_textureSlots = [229, 231, 229];

        public static float[] m_weaponPowers = [80f, 0f, 3.6f];

        public static float[] m_explosionPressures = new float[3];

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) { }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            int bulletType = (int)GetBulletType(Terrain.ExtractData(value));
            float size2 = bulletType >= 0 && bulletType < m_sizes.Length ? size * m_sizes[bulletType] : size;
            BlocksManager.DrawFlatOrImageExtrusionBlock(
                primitivesRenderer,
                value,
                size2,
                ref matrix,
                GetDefaultTexture(value),
                color,
                false,
                environmentData
            );
        }

        public override float GetProjectilePower(int value) {
            int bulletType = (int)GetBulletType(Terrain.ExtractData(value));
            if (bulletType < 0
                || bulletType >= m_weaponPowers.Length) {
                return 0f;
            }
            return m_weaponPowers[bulletType];
        }

        public override float GetExplosionPressure(int value) {
            int bulletType = (int)GetBulletType(Terrain.ExtractData(value));
            if (bulletType < 0
                || bulletType >= m_explosionPressures.Length) {
                return 0f;
            }
            return m_explosionPressures[bulletType];
        }

        public override IEnumerable<int> GetCreativeValues() {
            foreach (int enumValue in EnumUtils.GetEnumValues<BulletType>()) {
                yield return Terrain.MakeBlockValue(BlockIndex, 0, SetBulletType(0, (BulletType)enumValue));
            }
        }

        public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value) {
            int bulletType = (int)GetBulletType(Terrain.ExtractData(value));
            if (bulletType < 0
                || bulletType >= Enum.GetValues<BulletType>().Length) {
                return string.Empty;
            }
            return LanguageControl.Get("BulletBlock", bulletType);
        }

        public override int GetFaceTextureSlot(int face, int value) {
            int bulletType = (int)GetBulletType(Terrain.ExtractData(value));
            if (bulletType < 0
                || bulletType >= m_textureSlots.Length) {
                return 229;
            }
            return m_textureSlots[bulletType];
        }

        public static BulletType GetBulletType(int data) => (BulletType)(data & 0xF);

        public static int SetBulletType(int data, BulletType bulletType) => (data & -16) | ((int)bulletType & 15);
    }
}