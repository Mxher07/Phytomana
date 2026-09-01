using System.Globalization;

namespace Engine.Serialization {
    [HumanReadableConverter(typeof(decimal))]
    public class DecimalHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => ((decimal)value).ToString("R", CultureInfo.InvariantCulture);

        public object ConvertFromString(Type type, string data) => decimal.Parse(data, CultureInfo.InvariantCulture);
    }
}