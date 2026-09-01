namespace Game {
    public interface IDrawable {
        int[] DrawOrders { get; }

        void Draw(Camera camera, int drawOrder);
    }
}