namespace Game {
    public class SortedMultiCollection<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> {
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
            public SortedMultiCollection<TKey, TValue> m_collection;

            public KeyValuePair<TKey, TValue> m_current;

            public int m_index;

            public int m_version;

            public KeyValuePair<TKey, TValue> Current => m_current;

            object IEnumerator.Current => m_current;

            internal Enumerator(SortedMultiCollection<TKey, TValue> collection) {
                m_collection = collection;
                m_current = default;
                m_index = 0;
                m_version = collection.m_version;
            }

            public void Dispose() { }

            public bool MoveNext() {
                if (m_collection.m_version != m_version) {
                    throw new InvalidOperationException("SortedMultiCollection was modified, enumeration cannot continue.");
                }
                if (m_index < m_collection.m_count) {
                    m_current = m_collection.m_array[m_index];
                    m_index++;
                    return true;
                }
                m_current = default;
                return false;
            }

            public void Reset() {
                if (m_collection.m_version != m_version) {
                    throw new InvalidOperationException("SortedMultiCollection was modified, enumeration cannot continue.");
                }
                m_index = 0;
                m_current = default;
            }
        }

        public const int MinCapacity = 4;

        public KeyValuePair<TKey, TValue>[] m_array;

        public int m_count;

        public int m_version;

        public IComparer<TKey> m_comparer;

        public int Count => m_count;

        public int Capacity {
            get => m_array.Length;
            set {
                value = Math.Max(Math.Max(4, m_count), value);
                if (value != m_array.Length) {
                    KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[value];
                    Array.Copy(m_array, array, m_count);
                    m_array = array;
                }
            }
        }

        public KeyValuePair<TKey, TValue> this[int i] {
            get {
                if (i < m_count) {
                    return m_array[i];
                }
                throw new ArgumentOutOfRangeException();
            }
        }

        public SortedMultiCollection() {
            m_array = new KeyValuePair<TKey, TValue>[4];
            m_comparer = Comparer<TKey>.Default;
        }

        public SortedMultiCollection(IComparer<TKey> comparer) {
            m_array = new KeyValuePair<TKey, TValue>[4];
            m_comparer = comparer;
        }

        public SortedMultiCollection(int capacity) : this(capacity, null) {
            capacity = Math.Max(capacity, 4);
            m_array = new KeyValuePair<TKey, TValue>[capacity];
            m_comparer = Comparer<TKey>.Default;
        }

        public SortedMultiCollection(int capacity, IComparer<TKey> comparer) {
            capacity = Math.Max(capacity, 4);
            m_array = new KeyValuePair<TKey, TValue>[capacity];
            m_comparer = comparer;
        }

        public void Add(TKey key, TValue value) {
            int num = Find(key);
            if (num < 0) {
                num = ~num;
            }
            EnsureCapacity(m_count + 1);
            Array.Copy(m_array, num, m_array, num + 1, m_count - num);
            m_array[num] = new KeyValuePair<TKey, TValue>(key, value);
            m_count++;
            m_version++;
        }

        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items) {
            foreach (KeyValuePair<TKey, TValue> item in items) {
                Add(item.Key, item.Value);
            }
        }

        public bool Remove(TKey key) {
            int num = Find(key);
            if (num >= 0) {
                Array.Copy(m_array, num + 1, m_array, num, m_count - num - 1);
                m_array[m_count - 1] = default;
                m_count--;
                m_version++;
                return true;
            }
            return false;
        }

        public void Clear() {
            for (int i = 0; i < m_count; i++) {
                m_array[i] = default;
            }
            m_count = 0;
            m_version++;
        }

        public bool TryGetValue(TKey key, out TValue value) {
            int num = Find(key);
            if (num >= 0) {
                value = m_array[num].Value;
                return true;
            }
            value = default;
            return false;
        }

        public bool ContainsKey(TKey key) => Find(key) >= 0;

        public Enumerator GetEnumerator() => new(this);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        public void EnsureCapacity(int capacity) {
            if (capacity > Capacity) {
                Capacity = Math.Max(capacity, 2 * Capacity);
            }
        }

        // Find the last one. Edited by Deepseek.
        public int Find(TKey key) {
            if (m_count > 0) {
                int num = 0;
                int num2 = m_count - 1;
                int lastFound = -1;
                while (num <= num2) {
                    int num3 = (num + num2) >> 1;
                    int num4 = m_comparer.Compare(m_array[num3].Key, key);
                    if (num4 == 0) {
                        lastFound = num3;
                        num = num3 + 1;
                    }
                    else if (num4 < 0) {
                        num = num3 + 1;
                    }
                    else {
                        num2 = num3 - 1;
                    }
                }
                if (lastFound != -1) {
                    return lastFound;
                }
                return ~num;
            }
            return -1;
        }
    }
}