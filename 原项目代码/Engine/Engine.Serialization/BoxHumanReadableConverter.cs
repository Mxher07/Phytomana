namespace Engine.Serialization {
    [HumanReadableConverter(typeof(Box))]
    public class BoxHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) {
            Box box = (Box)value;
            return HumanReadableConverter.ValuesListToString(
                ',',
                box.Left,
                box.Top,
                box.Near,
                box.Width,
                box.Height,
                box.Depth
            );
        }

        public object ConvertFromString(Type type, string data) {
            int[] array = HumanReadableConverter.ValuesListFromString<int>(',', data);
            return array.Length == 6 ? (object)new Box(array[0], array[1], array[2], array[3], array[4], array[5]) : throw new Exception();
        }
    }
}