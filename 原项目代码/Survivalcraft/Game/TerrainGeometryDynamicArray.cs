using Engine;

namespace Game {
    public class TerrainGeometryDynamicArray<T> : DynamicArray<T>, IDisposable {
        public void Dispose() {
            Count = 0;
            Capacity = 0;
        }

        protected override T[] Allocate(int capacity) => m_cache.Rent(capacity, false);

        protected override void Free(T[] array) {
            m_cache.Return(array);
        }

        static ArrayCache<T> m_cache = new(Enumerable.Range(4, 30).Select(n => 1 << n), 0.66f, 60f, 0.33f, 5f);
    }
}