using System.Globalization;

namespace Engine.Serialization {
    [HumanReadableConverter(typeof(float))]
    public class SingleHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => ((float)value).ToString("R", CultureInfo.InvariantCulture);

        public object ConvertFromString(Type type, string data) => float.Parse(data, CultureInfo.InvariantCulture);
    }
}