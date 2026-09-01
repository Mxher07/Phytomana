namespace Game {
    public class DirtBlock : CubeBlock {
        public static int Index = 2;

        public override bool IsSuitableForPlants(int value, int plantValue) => true;
    }
}