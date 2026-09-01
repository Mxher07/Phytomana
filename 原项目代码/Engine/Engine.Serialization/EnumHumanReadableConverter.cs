namespace Engine.Serialization {
    [HumanReadableConverter(typeof(Enum))]
    public class EnumHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => ((Enum)value).ToString();

        public object ConvertFromString(Type type, string data) => Enum.Parse(type, data, false);
    }
}