// ReSharper disable SuspiciousTypeConversion.Global

namespace Engine {
    public class DynamicArray<T> : IList<T> {
        public struct Enumerator : IEnumerator<T> {
            DynamicArray<T> m_array;

            int m_index;

            public T Current => m_array.Array[m_index];

            object IEnumerator.Current => m_array.Array[m_index];

            public Enumerator(DynamicArray<T> array) {
                m_array = array;
                m_index = -1;
            }

            public void Dispose() { }

            public bool MoveNext() {
                m_index++;
                return m_index < m_array.Count;
            }

            public void Reset() {
                m_index = -1;
            }
        }

        struct Comparer : IComparer<T> {
            public Comparison<T> Comparison;

            public Comparer(Comparison<T> comparison) {
                if (comparison == null) {
                    throw new ArgumentNullException(nameof(comparison));
                }
                Comparison = comparison;
            }

            int IComparer<T>.Compare(T x, T y) => Comparison(x, y);
        }

        const int MinCapacity = 4;

        T[] m_array = m_emptyArray;

        int m_count;

        static T[] m_emptyArray = [];

        public int Capacity {
            get => m_array.Length;
            set {
                if (value != Capacity) {
                    if (value < m_count) {
                        throw new InvalidOperationException("Capacity cannot be made smaller than number of elements.");
                    }
                    Reallocate(value);
                }
            }
        }

        public int Count {
            get => m_count;
            set {
                if (value > Capacity) {
                    Reallocate(value);
                }
                else if (value < 0) {
                    throw new InvalidOperationException("Count cannot be negative.");
                }
                m_count = value;
            }
        }

        public T this[int index] {
            get => index >= m_count ? throw new IndexOutOfRangeException() : m_array[index];
            set {
                if (index >= m_count) {
                    throw new IndexOutOfRangeException();
                }
                m_array[index] = value;
            }
        }

        public T[] Array => m_array;

        public bool IsReadOnly => false;

        public DynamicArray() { }

        public DynamicArray(int capacity) => Capacity = capacity;

        public DynamicArray(IEnumerable<T> items) {
            AddRange(items);
        }

        public DynamicArray(IReadOnlyCollection<T> items) {
            AddRange(items);
        }

        public DynamicArray(IReadOnlyList<T> items) {
            AddRange(items);
        }

        public DynamicArray(DynamicArray<T> items) {
            AddRange(items);
        }

        public int IndexOf(T item) {
            EqualityComparer<T> @default = EqualityComparer<T>.Default;
            for (int i = 0; i < m_count; i++) {
                if (@default.Equals(item, m_array[i])) {
                    return i;
                }
            }
            return -1;
        }

        public void Add(T item) {
            EnsureCapacityForOne();
            m_array[m_count] = item;
            m_count++;
        }

        public void AddRange(IEnumerable<T> items) {
            if (items is DynamicArray<T> items2) {
                AddRangeTyped(items2);
                return;
            }
            if (items is IReadOnlyList<T> items3) {
                AddRangeTyped(items3);
                return;
            }
            if (items is IList<T> items4) {
                AddRangeTyped(items4);
                return;
            }
            if (items is IReadOnlyCollection<T> items5) {
                AddRangeTyped(items5);
                return;
            }
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            AddRangeTyped(items);
        }

        public void AddRange(IReadOnlyCollection<T> items) {
            if (items is DynamicArray<T> items2) {
                AddRangeTyped(items2);
                return;
            }
            if (items is IReadOnlyList<T> items3) {
                AddRangeTyped(items3);
                return;
            }
            if (items is IList<T> items4) {
                AddRangeTyped(items4);
                return;
            }
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            AddRangeTyped(items);
        }

        public void AddRange(IList<T> items) {
            if (items is DynamicArray<T> items2) {
                AddRangeTyped(items2);
                return;
            }
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            AddRangeTyped(items);
        }

        public void AddRange(IReadOnlyList<T> items) {
            if (items is DynamicArray<T> items2) {
                AddRangeTyped(items2);
                return;
            }
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            AddRangeTyped(items);
        }

        public void AddRange(DynamicArray<T> items) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            AddRangeTyped(items);
        }

        public bool Remove(T item) {
            int num = IndexOf(item);
            if (num >= 0) {
                RemoveAt(num);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index) {
            if (index < m_count) {
                m_count--;
                if (index < m_count) {
                    System.Array.Copy(m_array, index + 1, m_array, index, m_count - index);
                }
                m_array[m_count] = default;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        public void RemoveAtEnd() {
            if (m_count > 0) {
                m_count--;
                m_array[m_count] = default;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        public int RemoveAll(Predicate<T> match) {
            ArgumentNullException.ThrowIfNull(match);
            int i;
            for (i = 0; i < m_count && !match(m_array[i]); i++) { }
            if (i >= m_count) {
                return 0;
            }
            int j = i + 1;
            while (j < m_count) {
                for (; j < m_count && match(m_array[j]); j++) { }
                if (j < m_count) {
                    m_array[i++] = m_array[j++];
                }
            }
            System.Array.Clear(m_array, i, m_count - i);
            int result = m_count - i;
            m_count = i;
            return result;
        }

        public void RemoveRange(int index, int count) {
            if (index < 0
                || count < 0
                || m_count - index < count) {
                throw new IndexOutOfRangeException();
            }
            if (count > 0) {
                m_count -= count;
                if (index < m_count) {
                    System.Array.Copy(m_array, index + count, m_array, index, m_count - index);
                }
                System.Array.Clear(m_array, m_count, count);
            }
        }

        public void RemoveRange(IEnumerable<T> items) {
            foreach (T t in items) {
                Remove(t);
            }
        }

        public void Insert(int index, T item) {
            if (index <= m_count) {
                EnsureCapacityForOne();
                if (index < m_count) {
                    System.Array.Copy(m_array, index, m_array, index + 1, m_count - index);
                }
                m_array[index] = item;
                m_count++;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        public void Clear() {
            System.Array.Clear(m_array, 0, m_count);
            m_count = 0;
        }

        public void Reverse() {
            int num = 0;
            int num2 = m_count - 1;
            while (num < num2) {
                T val = m_array[num];
                m_array[num] = m_array[num2];
                m_array[num2] = val;
                num++;
                num2--;
            }
        }

        public void Sort() {
            System.Array.Sort(m_array, 0, m_count);
        }

        public void Sort(Comparison<T> comparison) {
            System.Array.Sort(m_array, 0, m_count, new Comparer(comparison));
        }

        public void Sort(int index, int count) {
            if (index < 0
                || count < 0
                || index + count > m_count) {
                throw new ArgumentOutOfRangeException();
            }
            System.Array.Sort(m_array, index, count);
        }

        public void Sort(int index, int count, Comparison<T> comparison) {
            if (index < 0
                || count < 0
                || index + count > m_count) {
                throw new ArgumentOutOfRangeException();
            }
            System.Array.Sort(m_array, index, count, new Comparer(comparison));
        }

        public Enumerator GetEnumerator() => new(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        public bool Contains(T item) {
            EqualityComparer<T> @default = EqualityComparer<T>.Default;
            for (int i = 0; i < m_count; i++) {
                if (@default.Equals(item, m_array[i])) {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex) {
            System.Array.Copy(m_array, 0, array, arrayIndex, m_count);
        }

        protected virtual T[] Allocate(int capacity) => new T[capacity];

        protected virtual void Free(T[] array) { }

        void Reallocate(int capacity) {
            if (capacity > 0) {
                ReallocateNonZero(capacity);
            }
            else if (m_array != m_emptyArray) {
                Free(m_array);
                m_array = m_emptyArray;
            }
        }

        void ReallocateNonZero(int capacity) {
            T[] array = Allocate(capacity);
            if (m_array != m_emptyArray) {
                System.Array.Copy(m_array, 0, array, 0, m_count);
                Free(m_array);
            }
            m_array = array;
        }

        void EnsureCapacityForOne() {
            if (Capacity <= m_count) {
                ReallocateNonZero(MathUtils.Max(Capacity * 2, MinCapacity));
            }
        }

        void EnsureCapacityExact(int capacity) {
            if (capacity > Capacity) {
                ReallocateNonZero(capacity);
            }
        }

        void AddRangeTyped(IEnumerable<T> items) {
            foreach (T item in items) {
                Add(item);
            }
        }

        void AddRangeTyped(IReadOnlyCollection<T> items) {
            EnsureCapacityExact(Count + items.Count);
            foreach (T item in items) {
                m_array[m_count] = item;
                m_count++;
            }
        }

        void AddRangeTyped(IReadOnlyList<T> items) {
            EnsureCapacityExact(Count + items.Count);
            for (int i = 0; i < items.Count; i++) {
                m_array[m_count] = items[i];
                m_count++;
            }
        }

        void AddRangeTyped(IList<T> items) {
            EnsureCapacityExact(Count + items.Count);
            for (int i = 0; i < items.Count; i++) {
                m_array[m_count] = items[i];
                m_count++;
            }
        }

        void AddRangeTyped(DynamicArray<T> items) {
            EnsureCapacityExact(Count + items.Count);
            System.Array.Copy(items.Array, 0, m_array, Count, items.Count);
            m_count += items.Count;
        }
    }
}