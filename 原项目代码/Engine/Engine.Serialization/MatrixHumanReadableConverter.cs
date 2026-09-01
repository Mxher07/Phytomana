namespace Engine.Serialization {
    [HumanReadableConverter(typeof(Matrix))]
    public class MatrixHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) {
            Matrix matrix = (Matrix)value;
            return HumanReadableConverter.ValuesListToString(
                ',',
                matrix.M11,
                matrix.M12,
                matrix.M13,
                matrix.M14,
                matrix.M21,
                matrix.M22,
                matrix.M23,
                matrix.M24,
                matrix.M31,
                matrix.M32,
                matrix.M33,
                matrix.M34,
                matrix.M41,
                matrix.M42,
                matrix.M43,
                matrix.M44
            );
        }

        public object ConvertFromString(Type type, string data) {
            float[] array = HumanReadableConverter.ValuesListFromString<float>(',', data);
            return array.Length == 16
                ? (object)new Matrix(
                    array[0],
                    array[1],
                    array[2],
                    array[3],
                    array[4],
                    array[5],
                    array[6],
                    array[7],
                    array[8],
                    array[9],
                    array[10],
                    array[11],
                    array[12],
                    array[13],
                    array[14],
                    array[15]
                )
                : throw new Exception();
        }
    }
}