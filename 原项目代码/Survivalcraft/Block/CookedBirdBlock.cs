using Engine;

namespace Game {
    public class CookedBirdBlock : FoodBlock {
        public static int Index = 78;

        public CookedBirdBlock() : base("Models/Bird", Matrix.Identity, new Color(150, 69, 15), 239) { }
    }
}