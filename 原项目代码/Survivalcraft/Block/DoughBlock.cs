using Engine;

namespace Game {
    public class DoughBlock : FoodBlock {
        public static int Index = 176;

        public DoughBlock() : base("Models/Bread", Matrix.CreateTranslation(0.5625f, -0.875f, 0f), new Color(241, 231, 214), 247) { }
    }
}