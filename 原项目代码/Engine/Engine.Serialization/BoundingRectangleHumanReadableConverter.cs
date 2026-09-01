namespace Engine.Serialization {
    [HumanReadableConverter(typeof(BoundingRectangle))]
    public class BoundingRectangleHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) {
            BoundingRectangle boundingRectangle = (BoundingRectangle)value;
            return HumanReadableConverter.ValuesListToString(
                ',',
                boundingRectangle.Min.X,
                boundingRectangle.Min.Y,
                boundingRectangle.Max.X,
                boundingRectangle.Max.Y
            );
        }

        public object ConvertFromString(Type type, string data) {
            float[] array = HumanReadableConverter.ValuesListFromString<float>(',', data);
            return array.Length == 4 ? (object)new BoundingRectangle(array[0], array[1], array[2], array[3]) : throw new Exception();
        }
    }
}