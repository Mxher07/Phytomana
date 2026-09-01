namespace Engine.Serialization {
    public class BinaryOutputArchive : OutputArchive, IDisposable {
#pragma warning disable CS0169 // 字段从未被使用
        int m_nextTypeId;
#pragma warning restore CS0169 // 字段从未被使用

        Dictionary<string, int> m_stringIds = new();

        EngineBinaryWriter m_writer;

        public bool Use7BitInts = true;

        public Stream Stream => m_writer.BaseStream;

        public BinaryOutputArchive(Stream stream, int version = 0, object context = null) : base(version, context) =>
            m_writer = new EngineBinaryWriter(stream);

        public void Reset(Stream stream, int version = 0, object context = null) {
            m_writer = new EngineBinaryWriter(stream);
            m_stringIds.Clear();
            Reset(version, context);
        }

        public override void Dispose() {
            Utilities.Dispose(ref m_writer);
        }

        public override void Serialize(string name, sbyte value) {
            m_writer.Write(value);
        }

        public override void Serialize(string name, byte value) {
            m_writer.Write(value);
        }

        public override void Serialize(string name, short value) {
            m_writer.Write(value);
        }

        public override void Serialize(string name, ushort value) {
            m_writer.Write(value);
        }

        public override void Serialize(string name, int value) {
            if (Use7BitInts) {
                m_writer.Write7BitEncodedInt(value);
            }
            else {
                m_writer.Write(value);
            }
        }

        public override void Serialize(string name, uint value) {
            m_writer.Write(value);
        }

        public override void Serialize(string name, long value) {
            m_writer.Write(value);
        }

        public override void Serialize(string name, ulong value) {
            m_writer.Write(value);
        }

        public override void Serialize(string name, float value) {
            m_writer.Write(value);
        }

        public override void Serialize(string name, double value) {
            m_writer.Write(value);
        }

        public override void Serialize(string name, bool value) {
            m_writer.Write(value);
        }

        public override void Serialize(string name, char value) {
            m_writer.Write(value);
        }

        public override void Serialize(string name, string value) {
            if (value == null) {
                m_writer.Write7BitEncodedInt(0);
            }
            else if (!m_stringIds.TryGetValue(value, out int value2)) {
                value2 = m_stringIds.Count + 1;
                m_stringIds.Add(value, value2);
                m_writer.Write7BitEncodedInt(value2);
                m_writer.Write(value);
            }
            else {
                m_writer.Write7BitEncodedInt(value2);
            }
        }

        public override void Serialize(string name, byte[] value) {
            m_writer.Write7BitEncodedInt(value.Length);
            m_writer.Write(value);
        }

        public override void Serialize(string name, int length, byte[] value) {
            if (value.Length != length) {
                throw new InvalidOperationException("Invalid fixed array length.");
            }
            m_writer.Write(value, 0, length);
        }

        public override void SerializeCollection<T>(string name, Func<T, string> itemNameFunc, IEnumerable<T> collection) {
            SerializeData serializeData = GetSerializeData(typeof(T), true);
            if (collection is IList<T> { Count: var count } list) {
                Serialize(null, count);
                for (int i = 0; i < count; i++) {
                    WriteObject(null, serializeData, list[i]);
                }
                return;
            }
            // ReSharper disable ConstantNullCoalescingCondition
            IEnumerable<T> enumerable = collection as T[] ?? collection.ToArray();
            // ReSharper restore ConstantNullCoalescingCondition
            Serialize(null, enumerable.Count());
            foreach (T item in enumerable) {
                WriteObject(null, serializeData, item);
            }
        }

        public override void SerializeDictionary<K, V>(string name, IDictionary<K, V> dictionary) {
            SerializeData serializeData = GetSerializeData(typeof(K), true);
            SerializeData serializeData2 = GetSerializeData(typeof(V), true);
            Serialize(null, dictionary.Count());
            foreach (KeyValuePair<K, V> item in dictionary) {
                WriteObject(null, serializeData, item.Key);
                WriteObject(null, serializeData2, item.Value);
            }
        }

        public override void WriteObjectInfo(int? objectId, bool isReference, Type runtimeType) {
            if (isReference) {
                Serialize(null, objectId << 3);
            }
            else if (runtimeType != null) {
                if (objectId.HasValue) {
                    Serialize(null, 7 | (objectId.Value << 4));
                }
                else {
                    Serialize(null, 5);
                }
                Serialize(null, runtimeType.FullName);
            }
            else if (objectId.HasValue) {
                Serialize(null, 3 | (objectId.Value << 4));
            }
            else {
                Serialize(null, 1);
            }
        }
    }
}