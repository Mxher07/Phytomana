namespace Engine.Serialization {
    [HumanReadableConverter(typeof(string))]
    public class StringHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => (string)value;

        public object ConvertFromString(Type type, string data) => data;
    }
}