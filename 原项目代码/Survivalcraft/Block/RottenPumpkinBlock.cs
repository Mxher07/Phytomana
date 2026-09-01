namespace Game {
    public class RottenPumpkinBlock : BasePumpkinBlock {
        public static int Index = 244;

        public RottenPumpkinBlock() : base(true) { }

        public override bool IsMovableByPiston(int value, int pistonFace, int y, out bool isEnd) {
            isEnd = false;
            return false;
        }
    }
}