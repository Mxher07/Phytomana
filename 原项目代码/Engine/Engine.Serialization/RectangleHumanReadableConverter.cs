namespace Engine.Serialization {
    [HumanReadableConverter(typeof(Rectangle))]
    public class RectangleHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) {
            Rectangle rectangle = (Rectangle)value;
            return HumanReadableConverter.ValuesListToString(',', rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        public object ConvertFromString(Type type, string data) {
            int[] array = HumanReadableConverter.ValuesListFromString<int>(',', data);
            return array.Length == 4 ? (object)new Rectangle(array[0], array[1], array[2], array[3]) : throw new Exception();
        }
    }
}