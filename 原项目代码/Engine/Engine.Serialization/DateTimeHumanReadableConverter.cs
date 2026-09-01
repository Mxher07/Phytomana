using System.Globalization;

namespace Engine.Serialization {
    [HumanReadableConverter(typeof(DateTime))]
    public class DateTimeHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => ((DateTime)value).Ticks.ToString(CultureInfo.InvariantCulture);

        public object ConvertFromString(Type type, string data) => new DateTime(long.Parse(data, CultureInfo.InvariantCulture));
    }
}