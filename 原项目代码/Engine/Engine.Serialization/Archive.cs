using System.Reflection;
using System.Runtime.CompilerServices;

namespace Engine.Serialization {
    public class Archive {
        protected delegate void ReadDelegateGeneric<T>(InputArchive archive, ref T value);

        protected delegate void WriteDelegateGeneric<T>(OutputArchive archive, T value);

        public delegate void ReadDelegate(InputArchive archive, ref object value);

        public delegate void WriteDelegate(OutputArchive archive, object value);

        protected class SerializeData<T> : SerializeData {
#pragma warning disable CS0649 // 从未对字段赋值，字段将一直保持其默认值
            internal ReadDelegateGeneric<T> ReadGeneric;

            internal WriteDelegateGeneric<T> WriteGeneric;
#pragma warning restore CS0649 // 从未对字段赋值，字段将一直保持其默认值

            internal SerializeData() : base(typeof(T)) { }
        }

        public class SerializeData {
            public bool IsValueType;

            public Type Type { get; set; }

            public bool IsHumanReadableSupported;

            public bool ConstructorSearched;

            public ConstructorInfo Constructor;

            public ReadDelegate Read;

            public WriteDelegate Write;

            public bool UseObjectInfo;

            public AutoConstructMode AutoConstruct;

            public bool IsSerializable => Read != null;

            public SerializeData(Type type) {
                Type = type;
                IsValueType = type.GetTypeInfo().IsValueType;
                UseObjectInfo = !type.GetTypeInfo().IsValueType && type != typeof(string);
                IsHumanReadableSupported = HumanReadableConverter.IsTypeSupported(type);
            }

            public SerializeData(bool useObjectInfo, AutoConstructMode autoConstruct) {
                UseObjectInfo = useObjectInfo;
                AutoConstruct = autoConstruct;
            }

            public void VerifySerializable() {
                if (!IsSerializable) {
                    throw new InvalidOperationException(
                        $"Type {Type.FullName} is not serializable. Type must have an associated ISerializer<T> or implement ISerializable."
                    );
                }
            }

            public void MergeOptionsFrom(SerializeData serializeData) {
                UseObjectInfo = serializeData.UseObjectInfo;
                AutoConstruct = serializeData.AutoConstruct;
            }

            public SerializeData Clone() => (SerializeData)MemberwiseClone();

            public object CreateInstance() {
                if (!ConstructorSearched) {
                    ConstructorSearched = true;
                    Constructor = FindConstructor(Type);
                }
                if (Constructor != null
                    && Constructor.DeclaringType == Type) {
                    return Activator.CreateInstance(Type, true);
                }
                object uninitializedObject = RuntimeHelpers.GetUninitializedObject(Type);
                if (Constructor != null) {
                    Constructor.Invoke(uninitializedObject, null);
                }
                return uninitializedObject;
            }

            public ConstructorInfo FindConstructor(Type type) {
                ConstructorInfo constructor = type.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    []
                );
                if (constructor == null
                    && type.BaseType != null) {
                    return FindConstructor(type.BaseType);
                }
                return constructor;
            }
        }

        static HashSet<Assembly> m_scannedAssemblies = [];

        static Dictionary<Type, SerializeData> m_serializeDataByType = [];

        static Dictionary<Type, SerializeData> m_pendingOptionsByType = [];

        static Dictionary<Type, TypeInfo> m_genericSerializersByType = [];

        public object Context;

        public int Version { get; set; }

        public bool UseObjectInfos { get; set; } = true;

        protected Archive(int version, object context) {
            Version = version;
            Context = context;
        }

        protected void Reset(int version, object context) {
            Version = version;
            Context = context;
        }

        public virtual void Dispose() { }

        public static bool IsTypeSerializable(Type type) => GetSerializeData(type, true).Read != null;

        public static void SetTypeSerializationOptions(Type type, bool useObjectInfo, AutoConstructMode autoConstruct) {
            lock (m_serializeDataByType) {
                SerializeData serializeData = new(useObjectInfo, autoConstruct);
                if (m_serializeDataByType.TryGetValue(type, out SerializeData value)) {
                    value.MergeOptionsFrom(serializeData);
                }
                else {
                    m_pendingOptionsByType[type] = serializeData;
                }
            }
        }

        public static object CreateInstance(Type type) => GetSerializeData(type, true).CreateInstance();

        /*protected static SerializeData GetSerializeData(Type type, bool allowEmptySerializer)
        {
            lock (m_serializeDataByType)
            {
                if (!m_serializeDataByType.TryGetValue(type, out SerializeData value))
                {
                    if (type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(ISerializable)))
                    {
                        value = CreateSerializeDataForSerializable(type);
                        AddSerializeData(value);
                    }
                    else
                    {
                        ScanAssembliesForSerializers();
                        if (!m_serializeDataByType.TryGetValue(type, out value))
                        {
                            if (type.IsArray)
                            {
                                if (m_genericSerializersByType.TryGetValue(typeof(Array), out TypeInfo value2))
                                {
                                    value = CreateSerializeDataForSerializer(value2.MakeGenericType(type.GetElementType()).GetTypeInfo(), type, typeof(Array));
                                    AddSerializeData(value);
                                }
                            }
                            else if (type.GetTypeInfo().IsGenericType)
                            {
                                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                                if (m_genericSerializersByType.TryGetValue(genericTypeDefinition, out TypeInfo value3))
                                {
                                    value = CreateSerializeDataForSerializer(value3.MakeGenericType(type.GenericTypeArguments).GetTypeInfo(), type, type);
                                    AddSerializeData(value);
                                }
                            }
#if ANDROID
                            else if (type.GetTypeInfo().BaseType != null && IsTypeSerializable(type.GetTypeInfo().BaseType))
                            {
                                value = GetSerializeData(type.GetTypeInfo().BaseType, allowEmptySerializer: true).Clone();
                                value.Type = type;
                                value.AutoConstructObject = true;
                            }
#else
                            else if (type.BaseType != null && IsTypeSerializable(type.BaseType))
                            {
                                value = GetSerializeData(type.BaseType, allowEmptySerializer: true).Clone();
                                value.Type = type;
                                value.AutoConstructObject = true;
                            }
#endif
                        }
                        if (value == null)
                        {
                            value = CreateEmptySerializeData(type);
                            AddSerializeData(value);
                        }
                    }
                }
                return !allowEmptySerializer && value.Read == null
                    ?                    throw new InvalidOperationException($"ISerializer suitable for type \"{type.FullName}\" not found in any loaded assembly.")
                    : value;
            }
        }*/
        protected static SerializeData GetSerializeData(Type type, bool allowEmptySerializer) {
            lock (m_serializeDataByType) {
                if (!m_serializeDataByType.TryGetValue(type, out SerializeData value)) {
                    ScanAssembliesForSerializers();
                    if (!m_serializeDataByType.TryGetValue(type, out value)) {
                        value = CreateSerializeData(type);
                        AddSerializeData(value);
                    }
                }
                if (allowEmptySerializer || value.Read != null) {
                    return value;
                }
                throw new InvalidOperationException($"ISerializer suitable for type \"{type.FullName}\" not found in any loaded assembly.");
            }
        }

        static void ScanAssembliesForSerializers() {
            foreach (Assembly item in TypeCache.LoadedAssemblies.Where(a => !TypeCache.IsKnownSystemAssembly(a))) {
                if (!m_scannedAssemblies.Contains(item)) {
                    foreach (TypeInfo definedType in item.DefinedTypes) {
                        foreach (Type implementedInterface in definedType.ImplementedInterfaces) {
                            if (implementedInterface.IsConstructedGenericType
                                && implementedInterface.GetGenericTypeDefinition() == typeof(ISerializer<>)) {
                                Type type = implementedInterface.GenericTypeArguments[0];
                                if (type.IsGenericParameter) {
                                    continue;
                                }
                                if (!definedType.IsGenericType
                                    || !definedType.IsGenericTypeDefinition) {
                                    if (!m_serializeDataByType.ContainsKey(type)) {
                                        SerializeData serializeData = CreateSerializeDataForSerializer(definedType, type);
                                        if (serializeData != null) {
                                            AddSerializeData(serializeData);
                                        }
                                    }
                                }
                                else if (type.GetTypeInfo().BaseType != typeof(Array)
                                    && type != typeof(Array)
                                    && !type.GetTypeInfo().IsEnum) {
                                    m_genericSerializersByType.Add(type.GetGenericTypeDefinition(), definedType);
                                }
                            }
                        }
                    }
                    m_scannedAssemblies.Add(item);
                }
            }
        }

        static SerializeData CreateSerializeData(Type type) {
            if (type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(ISerializable))) {
                return CreateSerializeDataForSerializable(type);
            }
            if (type.IsArray) {
                return CreateSerializeDataForSerializer(typeof(ArraySerializer<>).MakeGenericType(type.GetElementType()).GetTypeInfo(), type);
            }
            if (type.GetTypeInfo().IsEnum) {
                Type enumUnderlyingType = type.GetEnumUnderlyingType();
                if (enumUnderlyingType == typeof(int)
                    || enumUnderlyingType == typeof(uint)) {
                    return CreateSerializeDataForSerializer(typeof(Enum32Serializer<>).MakeGenericType(type).GetTypeInfo(), type);
                }
                if (enumUnderlyingType == typeof(long)
                    || enumUnderlyingType == typeof(ulong)) {
                    return CreateSerializeDataForSerializer(typeof(Enum64Serializer<>).MakeGenericType(type).GetTypeInfo(), type);
                }
                throw new InvalidOperationException("Unsupported underlying enum type.");
            }
            if (type.GetTypeInfo().IsGenericType) {
                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                if (m_genericSerializersByType.TryGetValue(genericTypeDefinition, out TypeInfo value)) {
                    return CreateSerializeDataForSerializer(value.MakeGenericType(type.GenericTypeArguments).GetTypeInfo(), type);
                }
            }
            if (type.GetTypeInfo().BaseType != null
                && IsTypeSerializable(type.GetTypeInfo().BaseType)) {
                SerializeData serializeData = GetSerializeData(type.GetTypeInfo().BaseType, true).Clone();
                serializeData.Type = type;
                if (serializeData.AutoConstruct == AutoConstructMode.NotSet) {
                    serializeData.AutoConstruct = AutoConstructMode.Yes;
                }
                serializeData.ConstructorSearched = false;
                return serializeData;
            }
            return new SerializeData(type);
        }

        static SerializeData CreateSerializeDataForSerializable(Type type) {
            SerializeData obj = (SerializeData)typeof(Archive).GetTypeInfo()
                .GetDeclaredMethod("CreateSerializeDataForSerializableHelper")
                .MakeGenericMethod(type)
                .Invoke(null, []);
            ApplySerializationOptionsAttribute(obj, type.GetTypeInfo());
            return obj;
        }

        static SerializeData CreateSerializeDataForSerializer(TypeInfo serializerType, Type type) {
            MethodInfo methodInfo = serializerType.AsType()
                .GetRuntimeMethods()
                .FirstOrDefault(
                    delegate(MethodInfo m) {
                        if (m.Name != "Serialize") {
                            return false;
                        }
                        ParameterInfo[] parameters2 = m.GetParameters();
                        return parameters2.Length == 2
                            && parameters2[0].ParameterType == typeof(InputArchive)
                            && parameters2[1].ParameterType == type.MakeByRefType();
                    }
                );
            MethodInfo methodInfo2 = serializerType.AsType()
                .GetRuntimeMethods()
                .FirstOrDefault(
                    delegate(MethodInfo m) {
                        if (m.Name != "Serialize") {
                            return false;
                        }
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 2 && parameters[0].ParameterType == typeof(OutputArchive) && parameters[1].ParameterType == type;
                    }
                );
            if (methodInfo != null
                && methodInfo2 != null) {
                object obj = Activator.CreateInstance(serializerType.AsType());
                Type type2 = typeof(ReadDelegateGeneric<>).MakeGenericType(type);
                Type type3 = typeof(WriteDelegateGeneric<>).MakeGenericType(type);
                Delegate @delegate = methodInfo.CreateDelegate(type2, obj);
                Delegate delegate2 = methodInfo2.CreateDelegate(type3, obj);
                return (SerializeData)typeof(Archive).GetTypeInfo()
                    .GetDeclaredMethod("CreateSerializeDataForSerializerHelper")
                    .MakeGenericMethod(type)
                    .Invoke(null, [@delegate, delegate2]);
            }
            throw new InvalidOperationException($"Serialization methods not found in {serializerType.Name}");
        }

        private static SerializeData CreateSerializeDataForSerializableHelper<T>() where T : ISerializable {
            SerializeData<T> serializeData = new SerializeData<T>();
            serializeData.ReadGeneric = delegate(InputArchive archive, ref T value) { value.Serialize(archive); };
            serializeData.WriteGeneric = delegate(OutputArchive archive, T value) { value.Serialize(archive); };
            if (serializeData.IsValueType) {
                serializeData.Read = delegate(InputArchive archive, ref object value) {
                    T val = (T)value;
                    val.Serialize(archive);
                    value = val;
                };
            }
            else {
                serializeData.Read = delegate(InputArchive archive, ref object value) { ((T)value).Serialize(archive); };
            }
            serializeData.Write = delegate(OutputArchive archive, object value) { ((T)value).Serialize(archive); };
            serializeData.AutoConstruct = AutoConstructMode.Yes;
            return serializeData;
        }

        private static SerializeData CreateSerializeDataForSerializerHelper<T>(Delegate readDelegate, Delegate writeDelegate) {
            ReadDelegateGeneric<T> readDelegateGeneric = (ReadDelegateGeneric<T>)readDelegate;
            WriteDelegateGeneric<T> writeDelegateGeneric = (WriteDelegateGeneric<T>)writeDelegate;
            return new SerializeData<T> {
                ReadGeneric = (ReadDelegateGeneric<T>)readDelegate,
                WriteGeneric = (WriteDelegateGeneric<T>)writeDelegate,
                Read = delegate(InputArchive archive, ref object value) {
                    T value2 = ((value != null) ? ((T)value) : default(T));
                    readDelegateGeneric(archive, ref value2);
                    value = value2;
                },
                Write = delegate(OutputArchive archive, object value) { writeDelegateGeneric(archive, (T)value); }
            };
        }

        static void ApplySerializationOptionsAttribute(SerializeData serializeData, TypeInfo attributeTarget) {
            SerializationOptionsAttribute serializationOptionsAttribute =
                (SerializationOptionsAttribute)attributeTarget.GetCustomAttribute(typeof(SerializationOptionsAttribute));
            if (serializationOptionsAttribute != null) {
                serializeData.UseObjectInfo = serializationOptionsAttribute.UseObjectInfo;
                serializeData.AutoConstruct = serializationOptionsAttribute.AutoConstruct;
            }
        }

        static void AddSerializeData(SerializeData serializeData) {
            if (m_pendingOptionsByType.TryGetValue(serializeData.Type, out SerializeData value)) {
                serializeData.MergeOptionsFrom(value);
            }
            m_serializeDataByType.Add(serializeData.Type, serializeData);
        }
    }
}