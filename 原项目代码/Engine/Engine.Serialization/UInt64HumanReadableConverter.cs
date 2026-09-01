using System.Globalization;

namespace Engine.Serialization {
    [HumanReadableConverter(typeof(ulong))]
    public class UInt64HumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => ((ulong)value).ToString(CultureInfo.InvariantCulture);

        public object ConvertFromString(Type type, string data) => ulong.Parse(data, CultureInfo.InvariantCulture);
    }
}