namespace Game {
    public class BedrockBlock : CubeBlock {
        public static int Index = 1;

        public override bool IsMovableByPiston(int value, int pistonFace, int y, out bool isEnd) {
            isEnd = false;
            return y > 1;
        }
    }
}