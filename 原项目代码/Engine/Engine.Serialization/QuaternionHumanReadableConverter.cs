namespace Engine.Serialization {
    [HumanReadableConverter(typeof(Quaternion))]
    public class QuaternionHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) {
            Quaternion quaternion = (Quaternion)value;
            return HumanReadableConverter.ValuesListToString(',', quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        public object ConvertFromString(Type type, string data) {
            float[] array = HumanReadableConverter.ValuesListFromString<float>(',', data);
            return array.Length == 4 ? (object)new Quaternion(array[0], array[1], array[2], array[3]) : throw new Exception();
        }
    }
}