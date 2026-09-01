using Engine;

namespace Game {
    public interface IAStarWorld<T> {
        float Cost(T p1, T p2);

        void Neighbors(T p, DynamicArray<T> neighbors);

        float Heuristic(T p1, T p2);

        bool IsGoal(T p);
    }
}