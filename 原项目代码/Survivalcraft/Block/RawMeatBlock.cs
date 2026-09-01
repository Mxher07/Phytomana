using Engine;

namespace Game {
    public class RawMeatBlock : FoodBlock {
        public static int Index = 88;

        public RawMeatBlock() : base("Models/Meat", Matrix.Identity, Color.White, 240) { }
    }
}