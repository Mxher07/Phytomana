using Engine;

namespace Game {
    public class RawFishBlock : FoodBlock {
        public static int Index = 161;

        public RawFishBlock() : base("Models/Fish", Matrix.Identity, Color.White, 241) { }
    }
}