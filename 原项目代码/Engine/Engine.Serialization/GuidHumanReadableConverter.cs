namespace Engine.Serialization {
    [HumanReadableConverter(typeof(Guid))]
    public class GuidHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => ((Guid)value).ToString("D");

        public object ConvertFromString(Type type, string data) => new Guid(data);
    }
}