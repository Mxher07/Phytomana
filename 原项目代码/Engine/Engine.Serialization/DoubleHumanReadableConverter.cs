using System.Globalization;

namespace Engine.Serialization {
    [HumanReadableConverter(typeof(double))]
    public class DoubleHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => ((double)value).ToString("R", CultureInfo.InvariantCulture);

        public object ConvertFromString(Type type, string data) => double.Parse(data, CultureInfo.InvariantCulture);
    }
}