using Engine;

namespace Game {
    public class CookedMeatBlock : FoodBlock {
        public static int Index = 89;

        public CookedMeatBlock() : base("Models/Meat", Matrix.Identity, new Color(155, 122, 51), 240) { }
    }
}