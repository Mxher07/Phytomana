namespace Engine.Serialization {
    [HumanReadableConverter(typeof(Point2))]
    public class Point2HumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) {
            Point2 point = (Point2)value;
            return HumanReadableConverter.ValuesListToString(',', point.X, point.Y);
        }

        public object ConvertFromString(Type type, string data) {
            int[] array = HumanReadableConverter.ValuesListFromString<int>(',', data);
            return array.Length == 2 ? (object)new Point2(array[0], array[1]) : throw new Exception();
        }
    }
}