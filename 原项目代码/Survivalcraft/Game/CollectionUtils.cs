namespace Game {
    public static class CollectionUtils {
        public static T ElementAt<T, E>(E enumerator, int index) where E : IEnumerator<T> {
            int num = 0;
            do {
                if (!enumerator.MoveNext()) {
                    throw new IndexOutOfRangeException("ElementAt() index out of range.");
                }
                num++;
            }
            while (num <= index);
            return enumerator.Current;
        }

        public static void RandomShuffle<T>(this IList<T> list, Func<int, int> random) {
            for (int num = list.Count - 1; num > 0; num--) {
                int index = random(num + 1);
                T value = list[index];
                list[index] = list[num];
                list[num] = value;
            }
        }

        public static int FirstIndex<T>(this IEnumerable<T> collection, T value) {
            int num = 0;
            foreach (T item in collection) {
                if (Equals(item, value)) {
                    return num;
                }
                num++;
            }
            return -1;
        }

        public static int FirstIndex<T>(this IEnumerable<T> collection, Func<T, bool> predicate) {
            int num = 0;
            foreach (T item in collection) {
                if (predicate(item)) {
                    return num;
                }
                num++;
            }
            return -1;
        }

        public static T SelectNth<T>(this IList<T> list, int n, IComparer<T> comparer) {
            if (list == null
                || list.Count <= n) {
                throw new ArgumentException();
            }
            int num = 0;
            int num2 = list.Count - 1;
            while (num < num2) {
                int num3 = num;
                int num4 = num2;
                T y = list[(num3 + num4) / 2];
                while (num3 < num4) {
                    if (comparer.Compare(list[num3], y) >= 0) {
                        T value = list[num4];
                        list[num4] = list[num3];
                        list[num3] = value;
                        num4--;
                    }
                    else {
                        num3++;
                    }
                }
                if (comparer.Compare(list[num3], y) > 0) {
                    num3--;
                }
                if (n <= num3) {
                    num2 = num3;
                }
                else {
                    num = num3 + 1;
                }
            }
            return list[n];
        }
    }
}