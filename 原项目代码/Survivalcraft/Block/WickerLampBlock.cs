namespace Game {
    public class WickerLampBlock : AlphaTestCubeBlock {
        public static int Index = 17;

        public WickerLampBlock() => CanBeBuiltIntoFurniture = true;

        public override bool IsFurnitureEmissive(int value) => true;

        public override int GetFaceTextureSlot(int face, int value) {
            if (face != 5) {
                return DefaultTextureSlot;
            }
            return 4;
        }
    }
}