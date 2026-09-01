using Engine;

namespace Game {
    public class RottenFishBlock : FoodBlock {
        public static int Index = 241;

        public RottenFishBlock() : base("Models/Fish", Matrix.CreateTranslation(-0.125f, 0.125f, 0f), Color.White, m_compostValue) { }
    }
}