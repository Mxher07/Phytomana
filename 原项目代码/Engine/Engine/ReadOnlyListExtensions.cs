namespace Engine {
    public static class ReadOnlyListExtensions {
        public static ReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source) => new(source.ToArray());

        public static ReadOnlyList<T> ToReadOnlyList<T>(this IList<T> source) => new(source);
    }
}