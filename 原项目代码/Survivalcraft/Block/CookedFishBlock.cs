using Engine;

namespace Game {
    public class CookedFishBlock : FoodBlock {
        public static int Index = 162;

        public CookedFishBlock() : base("Models/Fish", Matrix.Identity, new Color(160, 80, 40), 241) { }
    }
}