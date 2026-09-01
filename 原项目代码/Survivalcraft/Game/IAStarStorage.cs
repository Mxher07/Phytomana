namespace Game {
    public interface IAStarStorage<T> {
        void Clear();

        object Get(T p);

        void Set(T p, object data);
    }
}