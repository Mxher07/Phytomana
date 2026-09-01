using Engine;
using Engine.Graphics;

namespace Game {
    public class SeedsBlock : FlatBlock {
        public enum SeedType {
            TallGrass = 0,
            RedFlower = 1,
            PurpleFlower = 2,
            WhiteFlower = 3,
            WildRye = 4,
            Rye = 5,
            Cotton = 6,
            Pumpkin = 7
        }

        public static int Index = 173;

        public override IEnumerable<int> GetCreativeValues() {
            List<int> list = new();
            foreach (int enumValue in EnumUtils.GetEnumValues<SeedType>()) {
                list.Add(Terrain.MakeBlockValue(173, 0, enumValue));
            }
            return list;
        }

        public override int GetFaceTextureSlot(int face, int value) {
            int num = Terrain.ExtractData(value);
            if (num == 5
                || num == 4) {
                return 74;
            }
            return 75;
        }

        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            BlockPlacementData result = default;
            result.CellFace = raycastResult.CellFace;
            if (raycastResult.CellFace.Face == 4) {
                switch ((SeedType)Terrain.ExtractData(value)) {
                    case SeedType.TallGrass: result.Value = Terrain.MakeBlockValue(19, 0, TallGrassBlock.SetIsSmall(0, true)); break;
                    case SeedType.RedFlower: result.Value = Terrain.MakeBlockValue(20, 0, FlowerBlock.SetIsSmall(0, true)); break;
                    case SeedType.PurpleFlower: result.Value = Terrain.MakeBlockValue(24, 0, FlowerBlock.SetIsSmall(0, true)); break;
                    case SeedType.WhiteFlower: result.Value = Terrain.MakeBlockValue(25, 0, FlowerBlock.SetIsSmall(0, true)); break;
                    case SeedType.WildRye: result.Value = Terrain.MakeBlockValue(174, 0, RyeBlock.SetSize(RyeBlock.SetIsWild(0, false), 0)); break;
                    case SeedType.Rye: result.Value = Terrain.MakeBlockValue(174, 0, RyeBlock.SetSize(RyeBlock.SetIsWild(0, false), 0)); break;
                    case SeedType.Cotton: result.Value = Terrain.MakeBlockValue(204, 0, CottonBlock.SetSize(CottonBlock.SetIsWild(0, false), 0)); break;
                    case SeedType.Pumpkin: result.Value = Terrain.MakeBlockValue(131, 0, BasePumpkinBlock.SetSize(BasePumpkinBlock.SetIsDead(0, false), 0)); break;
                }
            }
            return result;
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            switch ((SeedType)Terrain.ExtractData(value)) {
                case SeedType.TallGrass: color *= new Color(160, 150, 125); break;
                case SeedType.RedFlower: color *= new Color(192, 160, 160); break;
                case SeedType.PurpleFlower: color *= new Color(192, 160, 192); break;
                case SeedType.WhiteFlower: color *= new Color(192, 192, 192); break;
                case SeedType.WildRye: color *= new Color(60, 138, 76); break;
                case SeedType.Rye: color *= new Color(255, 255, 255); break;
                case SeedType.Pumpkin: color *= new Color(240, 225, 190); break;
            }
            BlocksManager.DrawFlatOrImageExtrusionBlock(
                primitivesRenderer,
                value,
                size,
                ref matrix,
                GetDefaultTexture(value),
                color,
                false,
                environmentData
            );
        }
    }
}