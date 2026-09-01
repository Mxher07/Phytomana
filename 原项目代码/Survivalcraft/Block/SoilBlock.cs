using Engine;
using Engine.Graphics;

namespace Game {
    public class SoilBlock : CubeBlock {
        public static int Index = 168;

        public static string fName = "SoilBlock";

        public static BoundingBox[] m_collisionBoxes = [new(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.9375f, 1f))];

        public override bool IsFaceNonAttachable(SubsystemTerrain subsystemTerrain, int face, int value, int attachBlockValue) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(attachBlockValue)];
            if (block is BasePumpkinBlock) {
                return false;
            }
            return base.IsFaceNonAttachable(subsystemTerrain, face, value, attachBlockValue);
        }

        public override int GetFaceTextureSlot(int face, int value) {
            int nitrogen = GetNitrogen(Terrain.ExtractData(value));
            if (face != 4) {
                return 2;
            }
            if (nitrogen <= 0) {
                return 37;
            }
            return 53;
        }

        public static bool GetHydration(int data) => (data & 1) != 0;

        public static int GetNitrogen(int data) => (data >> 1) & 3;

        public static int SetHydration(int data, bool hydration) {
            if (!hydration) {
                return data & -2;
            }
            return data | 1;
        }

        public static int SetNitrogen(int data, int nitrogen) {
            nitrogen = Math.Clamp(nitrogen, 0, 3);
            return (data & -7) | ((nitrogen & 3) << 1);
        }

        public override IEnumerable<int> GetCreativeValues() {
            yield return Terrain.MakeBlockValue(BlockIndex, 0, SetHydration(0, false));
            yield return Terrain.MakeBlockValue(BlockIndex, 0, SetHydration(0, true));
            yield return Terrain.MakeBlockValue(BlockIndex, 0, SetHydration(SetNitrogen(0, 3), true));
        }

        public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value) {
            int data = Terrain.ExtractData(value);
            int nitrogen = GetNitrogen(data);
            bool hydration = GetHydration(data);
            if (nitrogen > 0 && hydration) {
                return LanguageControl.Get(fName, 1);
            }
            if (nitrogen > 0) {
                return LanguageControl.Get(fName, 2);
            }
            if (hydration) {
                return LanguageControl.Get(fName, 3);
            }
            return LanguageControl.Get(fName, 4);
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            Color color = GetHydration(Terrain.ExtractData(value)) ? new Color(180, 170, 150) : Color.White;
            generator.GenerateCubeVertices(
                this,
                value,
                x,
                y,
                z,
                0.9375f,
                0.9375f,
                0.9375f,
                0.9375f,
                color,
                color,
                color,
                color,
                color,
                -1,
                texture == null ? geometry.OpaqueSubsetsByFace : geometry.GetGeometry(texture).OpaqueSubsetsByFace
            );
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            Color c = GetHydration(Terrain.ExtractData(value)) ? new Color(180, 170, 150) : Color.White;
            base.DrawBlock(primitivesRenderer, value, color * c, size, ref matrix, environmentData);
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) => m_collisionBoxes;

        public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value) => face != 5;

        public override bool IsCollapseSupportBlock(SubsystemTerrain subsystemTerrain, int value) => true;

        public override bool IsSuitableForPlants(int value, int plantValue) {
            int plantContent = Terrain.ExtractContents(plantValue);
            Block plantBlock = BlocksManager.Blocks[plantContent];
            if (plantBlock is SaplingBlock) {
                return false;
            }
            return true;
        }
    }
}