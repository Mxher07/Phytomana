using Engine;

namespace Game {
    public class RottenDoughBlock : FoodBlock {
        public static int Index = 247;

        public RottenDoughBlock() : base("Models/Bread", Matrix.CreateTranslation(-0.375f, -0.25f, 0f), new Color(192, 255, 212), m_compostValue) { }
    }
}