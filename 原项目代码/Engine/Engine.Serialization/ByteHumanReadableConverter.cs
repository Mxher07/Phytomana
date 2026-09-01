using System.Globalization;

namespace Engine.Serialization {
    [HumanReadableConverter(typeof(byte))]
    public class ByteHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => ((byte)value).ToString(CultureInfo.InvariantCulture);

        public object ConvertFromString(Type type, string data) => byte.Parse(data, CultureInfo.InvariantCulture);
    }
}