namespace Game {
    public class PlanksBlock : PaintedCubeBlock {
        public static int Index = 21;

        public PlanksBlock() : base(23) => CanBeBuiltIntoFurniture = true;
    }
}