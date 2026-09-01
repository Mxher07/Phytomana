namespace Game {
    public class BricksBlock : PaintedCubeBlock {
        public static int Index = 73;

        public BricksBlock() : base(39) => CanBeBuiltIntoFurniture = true;
    }
}