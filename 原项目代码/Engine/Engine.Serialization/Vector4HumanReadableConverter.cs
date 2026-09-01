namespace Engine.Serialization {
    [HumanReadableConverter(typeof(Vector4))]
    public class Vector4HumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) {
            Vector4 vector = (Vector4)value;
            return HumanReadableConverter.ValuesListToString(',', vector.X, vector.Y, vector.Z, vector.W);
        }

        public object ConvertFromString(Type type, string data) {
            float[] array = HumanReadableConverter.ValuesListFromString<float>(',', data);
            return array.Length == 4 ? (object)new Vector4(array[0], array[1], array[2], array[3]) : throw new Exception();
        }
    }
}