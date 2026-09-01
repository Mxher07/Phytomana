namespace Game {
    public class StoneBrickBlock : PaintedCubeBlock {
        public static int Index = 26;

        public StoneBrickBlock() : base(50) => CanBeBuiltIntoFurniture = true;
    }
}