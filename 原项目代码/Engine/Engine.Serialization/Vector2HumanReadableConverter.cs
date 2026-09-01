namespace Engine.Serialization {
    [HumanReadableConverter(typeof(Vector2))]
    public class Vector2HumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) {
            Vector2 vector = (Vector2)value;
            return HumanReadableConverter.ValuesListToString(',', vector.X, vector.Y);
        }

        public object ConvertFromString(Type type, string data) {
            float[] array = HumanReadableConverter.ValuesListFromString<float>(',', data);
            switch (array.Length) {
                case 1: return new Vector2(array[0]);
                case >= 2: return new Vector2(array[0], array[1]);
            }
            throw new Exception();
        }
    }
}