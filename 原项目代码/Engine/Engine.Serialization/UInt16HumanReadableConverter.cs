using System.Globalization;

namespace Engine.Serialization {
    [HumanReadableConverter(typeof(ushort))]
    public class UInt16HumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => ((ushort)value).ToString(CultureInfo.InvariantCulture);

        public object ConvertFromString(Type type, string data) => ushort.Parse(data, CultureInfo.InvariantCulture);
    }
}