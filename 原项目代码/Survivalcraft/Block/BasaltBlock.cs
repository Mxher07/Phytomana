namespace Game {
    public class BasaltBlock : PaintedCubeBlock {
        public static int Index = 67;

        public BasaltBlock() : base(40) => CanBeBuiltIntoFurniture = true;
    }
}