using Engine;

namespace Game {
    public class BreadBlock : FoodBlock {
        public static int Index = 177;

        public BreadBlock() : base("Models/Bread", Matrix.Identity, Color.White, 242) { }
    }
}