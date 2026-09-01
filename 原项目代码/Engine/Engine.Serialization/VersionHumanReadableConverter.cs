namespace Engine.Serialization {
    [HumanReadableConverter(typeof(Version))]
    public class VersionHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => ((Version)value).ToString();

        public object ConvertFromString(Type type, string data) => Version.Parse(data);
    }
}