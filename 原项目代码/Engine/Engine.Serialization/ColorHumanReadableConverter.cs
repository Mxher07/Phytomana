namespace Engine.Serialization {
    [HumanReadableConverter(typeof(Color))]
    public class ColorHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) {
            Color color = (Color)value;
            return color.A != byte.MaxValue
                ? HumanReadableConverter.ValuesListToString(',', color.R, color.G, color.B, color.A)
                : HumanReadableConverter.ValuesListToString(',', color.R, color.G, color.B);
        }

        public object ConvertFromString(Type type, string data) {
            if (data[0] == '#') {
                if (data.Length == 7) {
                    byte r = (byte)(16 * HexToDecimal(data[1]) + HexToDecimal(data[2]));
                    byte g = (byte)(16 * HexToDecimal(data[3]) + HexToDecimal(data[4]));
                    byte b = (byte)(16 * HexToDecimal(data[5]) + HexToDecimal(data[6]));
                    return new Color(r, g, b);
                }
                if (data.Length == 9) {
                    byte r2 = (byte)(16 * HexToDecimal(data[1]) + HexToDecimal(data[2]));
                    byte g2 = (byte)(16 * HexToDecimal(data[3]) + HexToDecimal(data[4]));
                    byte b2 = (byte)(16 * HexToDecimal(data[5]) + HexToDecimal(data[6]));
                    byte a = (byte)(16 * HexToDecimal(data[7]) + HexToDecimal(data[8]));
                    return new Color(r2, g2, b2, a);
                }
                throw new Exception();
            }
            int[] array = HumanReadableConverter.ValuesListFromString<int>(',', data);
            return array.Length == 3 ? new Color(array[0], array[1], array[2]) :
                array.Length == 4 ? (object)new Color(array[0], array[1], array[2], array[3]) : throw new Exception();
        }

        static int HexToDecimal(char digit) {
            if (digit >= '0'
                && digit <= '9') {
                return digit - 48;
            }
            return digit >= 'A' && digit <= 'F' ? digit - 65 + 10 :
                digit >= 'a' && digit <= 'f' ? digit - 97 + 10 : throw new Exception();
        }
    }
}