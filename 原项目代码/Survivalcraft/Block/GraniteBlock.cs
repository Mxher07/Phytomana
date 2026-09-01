namespace Game {
    public class GraniteBlock : PaintedCubeBlock {
        public static int Index = 3;

        public GraniteBlock() : base(24) => CanBeBuiltIntoFurniture = true;
    }
}