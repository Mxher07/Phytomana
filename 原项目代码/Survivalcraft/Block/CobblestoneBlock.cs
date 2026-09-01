namespace Game {
    public class CobblestoneBlock : PaintedCubeBlock {
        public static int Index = 5;

        public CobblestoneBlock() : base(69) => CanBeBuiltIntoFurniture = true;
    }
}