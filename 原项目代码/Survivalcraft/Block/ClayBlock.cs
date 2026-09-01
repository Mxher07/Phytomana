namespace Game {
    public class ClayBlock : PaintedCubeBlock {
        public static int Index = 72;

        public ClayBlock() : base(15) => CanBeBuiltIntoFurniture = true;
    }
}