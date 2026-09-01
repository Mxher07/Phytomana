namespace Engine.Serialization {
    [HumanReadableConverter(typeof(char))]
    public class CharHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => ((char)value).ToString();

        public object ConvertFromString(Type type, string data) => data[0];
    }
}