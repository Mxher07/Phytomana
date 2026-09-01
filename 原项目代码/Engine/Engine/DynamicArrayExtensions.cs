namespace Engine {
    public static class DynamicArrayExtensions {
        public static DynamicArray<T> ToDynamicArray<T>(this IEnumerable<T> source) => new(source);

        public static DynamicArray<T> ToDynamicArray<T>(this IReadOnlyCollection<T> source) => new(source);

        public static DynamicArray<T> ToDynamicArray<T>(this IList<T> source) => new(source);

        public static DynamicArray<T> ToDynamicArray<T>(this IReadOnlyList<T> source) => new(source);

        public static DynamicArray<T> ToDynamicArray<T>(this DynamicArray<T> source) => new(source);
    }
}