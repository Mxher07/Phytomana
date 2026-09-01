using Engine;

namespace Game {
    public static class EnumUtils {
        public struct NamesValues {
            public ReadOnlyList<string> Names;

            public ReadOnlyList<int> Values;
        }

        public static class Cache {
            public static Dictionary<Type, NamesValues> m_namesValuesByType = new();

            [Obsolete("Use Query<T> instead.")]
            public static NamesValues Query(Type type) {
                lock (m_namesValuesByType) {
                    if (!m_namesValuesByType.TryGetValue(type, out NamesValues namesValues)) {
                        namesValues = default;
                        namesValues.Names = new ReadOnlyList<string>(new List<string>(Enum.GetNames(type)));
#pragma warning disable IL3050
                        namesValues.Values = new ReadOnlyList<int>(new List<int>(Enum.GetValues(type).Cast<int>()));
#pragma warning restore IL3050
                        m_namesValuesByType.Add(type, namesValues);
                    }
                    return namesValues;
                }
            }

            public static NamesValues Query<T>() where T : struct, Enum {
                lock (m_namesValuesByType) {
                    Type type = typeof(T);
                    if (!m_namesValuesByType.TryGetValue(type, out NamesValues namesValues)) {
                        namesValues = default;
                        namesValues.Names = new ReadOnlyList<string>(Enum.GetNames<T>());
                        namesValues.Values = new ReadOnlyList<int>(Enum.GetValues<T>().Select(x => Convert.ToInt32(x)).ToArray());
                        m_namesValuesByType.Add(type, namesValues);
                    }
                    return namesValues;
                }
            }
        }

        [Obsolete("Use GetEnumName<T> instead.")]
        public static string GetEnumName(Type type, int value) {
            int num = GetEnumValues(type).IndexOf(value);
            return num >= 0 ? GetEnumNames(type)[num] : "<invalid enum>";
        }

        public static string GetEnumName<T>(T value) where T : struct, Enum {
            int num = GetEnumValues<T>().IndexOf(Convert.ToInt32(value));
            return num >= 0 ? GetEnumNames<T>()[num] : "<invalid enum>";
        }

        [Obsolete("Use GetEnumNames<T> instead.")]
        public static IList<string> GetEnumNames(Type type) => Cache.Query(type).Names;

        public static IList<string> GetEnumNames<T>() where T : struct, Enum => Cache.Query<T>().Names;

        [Obsolete("Use GetEnumValues<T> instead.")]
        public static IList<int> GetEnumValues(Type type) => Cache.Query(type).Values;

        public static IList<int> GetEnumValues<T>() where T : struct, Enum => Cache.Query<T>().Values;
    }
}