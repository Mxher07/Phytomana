namespace Game {
    public interface IUpdateable {
        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public float FloatUpdateOrder => (float)UpdateOrder;
        public void Update(float dt);
    }
}