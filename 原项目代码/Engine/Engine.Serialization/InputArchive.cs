namespace Engine.Serialization {
    public abstract class InputArchive : Archive {
        Dictionary<int, object> m_objectById = [];

        DynamicArray<object> m_stack = new();

        public ReadOnlyList<object> Stack => new(m_stack);

        protected InputArchive(int version, object context) : base(version, context) { }

        protected new void Reset(int version, object context) {
            base.Reset(version, context);
            m_objectById.Clear();
            m_stack.Clear();
        }

        public abstract void Serialize(string name, ref sbyte value);

        public abstract void Serialize(string name, ref byte value);

        public abstract void Serialize(string name, ref short value);

        public abstract void Serialize(string name, ref ushort value);

        public abstract void Serialize(string name, ref int value);

        public abstract void Serialize(string name, ref uint value);

        public abstract void Serialize(string name, ref long value);

        public abstract void Serialize(string name, ref ulong value);

        public abstract void Serialize(string name, ref float value);

        public abstract void Serialize(string name, ref double value);

        public abstract void Serialize(string name, ref bool value);

        public abstract void Serialize(string name, ref char value);

        public abstract void Serialize(string name, ref string value);

        public abstract void Serialize(string name, ref byte[] value);

        public abstract void Serialize(string name, int length, ref byte[] value);

        public abstract void SerializeCollection<T>(string name, ICollection<T> collection);

        public abstract void SerializeDictionary<K, V>(string name, IDictionary<K, V> dictionary);

        public void Serialize(string name, Type type, ref object value) {
            ReadObject(name, GetSerializeData(type, true), ref value, true);
        }

        public void Serialize(string name, Type type, object value) {
            if (value == null) {
                throw new InvalidOperationException("Value cannot be null");
            }
            ReadObject(name, GetSerializeData(type, true), ref value, false);
        }

        public void Serialize<T>(string name, T value) where T : class {
            ReadObject(name, GetSerializeData(typeof(T), true), ref value, false);
        }

        public void Serialize<T>(string name, ref T value) {
            ReadObject(name, GetSerializeData(typeof(T), true), ref value, true);
        }

        public void Serialize<T>(string name, Action<T> setter) {
            T value = default;
            ReadObject(name, GetSerializeData(typeof(T), true), ref value, true);
            setter(value);
        }

        public T Serialize<T>(string name) {
            T value = default;
            ReadObject(name, GetSerializeData(typeof(T), true), ref value, true);
            return value;
        }

        public object Serialize(string name, Type type) {
            object value = null;
            Serialize(name, type, ref value);
            return value;
        }

        public List<T> SerializeCollection<T>(string name) {
            List<T> list = new();
            SerializeCollection(name, list);
            return list;
        }

        public void SerializeCollection<T>(string name, Action<T> adder) {
            List<T> list = new();
            SerializeCollection(name, list);
            foreach (T item in list) {
                adder(item);
            }
        }

        public Dictionary<K, V> SerializeDictionary<K, V>(string name) {
            Dictionary<K, V> dictionary = new();
            SerializeDictionary(name, dictionary);
            return dictionary;
        }

        public T FindParentObject<T>(bool throwIfNotFound = true) where T : class {
            for (int num = m_stack.Count - 1; num >= 0; num--) {
                if (m_stack[num] is T result) {
                    return result;
                }
            }
            if (throwIfNotFound) {
                throw new InvalidOperationException($"Required parent object of type {typeof(T).FullName} not found on serialization stack.");
            }
            return null;
        }

        public abstract void ReadObjectInfo(out int? objectId, out bool isReference, out Type runtimeType);

        protected virtual void ReadObject(string name, SerializeData staticSerializeData, ref object value, bool allowOverwriteOfExistingObject) {
            if (!staticSerializeData.UseObjectInfo
                || !UseObjectInfos) {
                ReadObjectWithoutObjectInfo(staticSerializeData, ref value);
            }
            else {
                ReadObjectWithObjectInfo(staticSerializeData, ref value, allowOverwriteOfExistingObject);
            }
        }

        protected virtual void ReadObject<T>(string name, SerializeData staticSerializeData, ref T value, bool allowOverwriteOfExistingObject) {
            if (staticSerializeData.IsValueType) {
                staticSerializeData.VerifySerializable();
                ((SerializeData<T>)staticSerializeData).ReadGeneric(this, ref value);
            }
            else {
                object value2 = value;
                ReadObject(name, staticSerializeData, ref value2, allowOverwriteOfExistingObject);
                value = (T)value2;
            }
        }

        void ReadObjectWithoutObjectInfo(SerializeData staticSerializeData, ref object value) {
            Type type = value?.GetType();
            SerializeData serializeData = !(type == null) && !(staticSerializeData.Type == type)
                ? GetSerializeData(type, false)
                : staticSerializeData;
            if (serializeData.AutoConstruct == AutoConstructMode.Yes
                && value == null) {
                value = Activator.CreateInstance(serializeData.Type, true);
            }
            serializeData.Read(this, ref value);
        }

        void ReadObjectWithObjectInfo(SerializeData staticSerializeData, ref object value, bool allowOverwriteOfExistingObject) {
            ReadObjectInfo(out int? objectId, out bool isReference, out Type runtimeType);
            if (objectId == 0) {
                if (!allowOverwriteOfExistingObject
                    && value != null) {
                    throw new InvalidOperationException("Serializing null reference into an existing object.");
                }
                return;
            }
            if (isReference) {
                if (!allowOverwriteOfExistingObject
                    && value != null) {
                    throw new InvalidOperationException("Serializing a reference into an existing object.");
                }
                value = m_objectById[objectId.Value];
                return;
            }
            Type type = value?.GetType();
            SerializeData serializeData;
            if (!(type != null)) {
                serializeData = !(runtimeType != null) ? staticSerializeData : GetSerializeData(runtimeType, false);
            }
            else {
                if (runtimeType != null
                    && runtimeType != type) {
                    throw new InvalidOperationException("Serialized object has different type than existing object.");
                }
                serializeData = GetSerializeData(type, false);
            }
            if (serializeData.AutoConstruct == AutoConstructMode.Yes
                && value == null) {
                value = serializeData.CreateInstance();
            }
            serializeData.VerifySerializable();
            m_stack.Add(value);
            serializeData.Read(this, ref value);
            m_stack.RemoveAtEnd();
            if (objectId.HasValue) {
                m_objectById.Add(objectId.Value, value);
            }
        }
    }
}