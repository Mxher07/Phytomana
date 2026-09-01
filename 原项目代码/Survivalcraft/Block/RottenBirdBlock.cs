using Engine;

namespace Game {
    public class RottenBirdBlock : FoodBlock {
        public static int Index = 239;

        public RottenBirdBlock() : base("Models/Bird", Matrix.CreateTranslation(-0.9375f, 0.4375f, 0f), Color.White, m_compostValue) { }
    }
}