namespace Game {
    public class SandBlock : CubeBlock {
        public static int Index = 7;

        public SandBlock() {
            CanBeBuiltIntoFurniture = true;
            IsCollapsable = true;
        }

        public override bool IsSuitableForPlants(int value, int plantValue) {
            int plantContent = Terrain.ExtractContents(plantValue);
            Block plantBlock = BlocksManager.Blocks[plantContent];
            if (plantBlock is SaplingBlock or BasePumpkinBlock) {
                return false;
            }
            return true;
        }
    }
}