namespace Game {
    public class MarbleBlock : PaintedCubeBlock {
        public static int Index = 68;

        public MarbleBlock() : base(51) => CanBeBuiltIntoFurniture = true;
    }
}