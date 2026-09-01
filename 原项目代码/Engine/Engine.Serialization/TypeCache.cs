using System.Reflection;

namespace Engine.Serialization {
    public static class TypeCache {
        public static Dictionary<string, Type> m_typesByName;

        public static Dictionary<string, string> m_shortToLong;

        public static Dictionary<string, string> m_longToShort;

        public static List<Assembly> m_loadedAssemblies;

        public static bool m_rescanAssemblies;

        public static ReadOnlyList<Assembly> LoadedAssemblies {
            get {
                lock (m_typesByName) {
                    if (m_rescanAssemblies) {
                        m_loadedAssemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
                        m_rescanAssemblies = false;
                    }
                    return new ReadOnlyList<Assembly>(m_loadedAssemblies);
                }
            }
        }

        static TypeCache() {
            m_typesByName = [];
            m_shortToLong = [];
            m_longToShort = [];
            m_loadedAssemblies = [];
            m_rescanAssemblies = true;
            AddShortTypeName("bool", typeof(bool).FullName);
            AddShortTypeName("sbyte", typeof(sbyte).FullName);
            AddShortTypeName("byte", typeof(byte).FullName);
            AddShortTypeName("short", typeof(short).FullName);
            AddShortTypeName("ushort", typeof(ushort).FullName);
            AddShortTypeName("int", typeof(int).FullName);
            AddShortTypeName("uint", typeof(uint).FullName);
            AddShortTypeName("long", typeof(long).FullName);
            AddShortTypeName("ulong", typeof(ulong).FullName);
            AddShortTypeName("float", typeof(float).FullName);
            AddShortTypeName("double", typeof(double).FullName);
            AddShortTypeName("char", typeof(char).FullName);
            AddShortTypeName("string", typeof(string).FullName);
            AddShortTypeName("Vector2", typeof(Vector2).FullName);
            AddShortTypeName("Vector3", typeof(Vector3).FullName);
            AddShortTypeName("Vector4", typeof(Vector4).FullName);
            AddShortTypeName("Quaternion", typeof(Quaternion).FullName);
            AddShortTypeName("Matrix", typeof(Matrix).FullName);
            AddShortTypeName("Color", typeof(Color).FullName);
            AddShortTypeName("Point2", typeof(Point2).FullName);
            AddShortTypeName("Point3", typeof(Point3).FullName);
            AddShortTypeName("Rectangle", typeof(Rectangle).FullName);
            AddShortTypeName("Box", typeof(Box).FullName);
            AddShortTypeName("BoundingRectangle", typeof(BoundingRectangle).FullName);
            AddShortTypeName("BoundingBox", typeof(BoundingBox).FullName);
            AddShortTypeName("BoundingCircle", typeof(BoundingCircle).FullName);
            AddShortTypeName("BoundingSphere", typeof(BoundingSphere).FullName);
            AddShortTypeName("Plane", typeof(Plane).FullName);
            AddShortTypeName("Ray2", typeof(Ray2).FullName);
            AddShortTypeName("Ray3", typeof(Ray3).FullName);
            AppDomain.CurrentDomain.AssemblyLoad += delegate {
                lock (m_typesByName) {
                    m_rescanAssemblies = true;
                }
            };
        }

        public static bool IsKnownSystemAssembly(Assembly assembly) {
            string text = assembly.FullName.ToLower();
            if (text.Contains("b77a5c561934e089")) {
                return true;
            }
            if (text.Contains("31bf3856ad364e35")) {
                return true;
            }
            if (text.Contains("b03f5f7f11d50a3a")) {
                return true;
            }
            if (text.Contains("89845dcd8080cc91")) {
                return true;
            }
            return text.Contains("opentk") || text.Contains("sharpdx") || text.Contains("silk");
        }

        public static Type FindType(string typeName, bool skipSystemAssemblies, bool throwIfNotFound) {
            lock (m_typesByName) {
                if (!m_typesByName.TryGetValue(typeName, out Type value)) {
                    string longTypeName = GetLongTypeName(typeName);
                    foreach (Assembly loadedAssembly in LoadedAssemblies) {
                        if (!skipSystemAssemblies
                            || !IsKnownSystemAssembly(loadedAssembly)) {
#pragma warning disable IL2026
                            value = loadedAssembly.GetType(longTypeName);
#pragma warning restore IL2026
                            if (value != null) {
                                break;
                            }
                        }
                    }
                    if (value == null) {
                        return throwIfNotFound
                            ? throw new InvalidOperationException($"Type \"{longTypeName}\" not found in any loaded assembly.")
                            : null;
                    }
                    m_typesByName.Add(typeName, value);
                }
                return value;
            }
        }

        public static string GetShortTypeName(string longTypeName) =>
            m_longToShort.TryGetValue(longTypeName, out string value) ? value : longTypeName;

        public static string GetLongTypeName(string shortTypeName) =>
            m_shortToLong.TryGetValue(shortTypeName, out string value) ? value : shortTypeName;

        static void AddShortTypeName(string shortTypeName, string longTypeName) {
            m_shortToLong.Add(shortTypeName, longTypeName);
            m_longToShort.Add(longTypeName, shortTypeName);
        }
    }
}