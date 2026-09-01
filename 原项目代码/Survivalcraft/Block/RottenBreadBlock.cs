using Engine;

namespace Game {
    public class RottenBreadBlock : FoodBlock {
        public static int Index = 242;

        public RottenBreadBlock() : base("Models/Bread", Matrix.CreateTranslation(-0.375f, -0.25f, 0f), Color.White, m_compostValue) { }
    }
}