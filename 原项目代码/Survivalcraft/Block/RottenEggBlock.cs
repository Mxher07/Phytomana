using Engine;

namespace Game {
    public class RottenEggBlock : FoodBlock {
        public static int Index = 246;

        public RottenEggBlock() : base("Models/RottenEgg", Matrix.Identity, Color.White, 246) { }
    }
}