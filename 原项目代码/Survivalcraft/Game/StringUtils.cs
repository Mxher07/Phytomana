using System.Text;

namespace Game {
    public static class StringUtils {
        public static char[] m_digits = [
            '0',
            '1',
            '2',
            '3',
            '4',
            '5',
            '6',
            '7',
            '8',
            '9',
            'A',
            'B',
            'C',
            'D',
            'E',
            'F'
        ];

        public static int Compare(StringBuilder s1, string s2) {
            int num = 0;
            while (true) {
                if (num >= s1.Length
                    && num >= s2.Length) {
                    return 0;
                }
                if (num > s1.Length) {
                    return -1;
                }
                if (num > s2.Length) {
                    return 1;
                }
                char c = s1[num];
                char c2 = s2[num];
                if (c < c2) {
                    return -1;
                }
                if (c > c2) {
                    break;
                }
                num++;
            }
            return 1;
        }

        public static int CalculateNumberLength(uint value, int numberBase) {
            if (numberBase < 2
                || numberBase > 16) {
                throw new ArgumentException("Number base is out of range.");
            }
            int num = 0;
            do {
                num++;
                value /= (uint)numberBase;
            }
            while (value != 0);
            return num;
        }

        public static int CalculateNumberLength(int value, int numberBase) {
            if (value >= 0) {
                return CalculateNumberLength((uint)value, numberBase);
            }
            return CalculateNumberLength((uint)-value, numberBase) + 1;
        }

        public static int CalculateNumberLength(ulong value, int numberBase) {
            if (numberBase < 2
                || numberBase > 16) {
                throw new ArgumentException("Number base is out of range.");
            }
            int num = 0;
            do {
                num++;
                value /= (uint)numberBase;
            }
            while (value != 0);
            return num;
        }

        public static int CalculateNumberLength(long value, int numberBase) {
            if (value >= 0) {
                return CalculateNumberLength((ulong)value, numberBase);
            }
            return CalculateNumberLength((ulong)-value, numberBase) + 1;
        }

        public static void AppendNumber(this StringBuilder stringBuilder,
            uint value,
            int padding = 0,
            char paddingCharacter = ' ',
            int numberBase = 10) {
            int val = CalculateNumberLength(value, numberBase);
            int repeatCount = Math.Max(padding, val);
            stringBuilder.Append(paddingCharacter, repeatCount);
            int num = 0;
            do {
                char value2 = m_digits[value % (uint)numberBase];
                stringBuilder[stringBuilder.Length - num - 1] = value2;
                value /= (uint)numberBase;
                num++;
            }
            while (value != 0);
        }

        public static void AppendNumber(this StringBuilder stringBuilder,
            int value,
            int padding = 0,
            char paddingCharacter = ' ',
            int numberBase = 10) {
            if (value >= 0) {
                stringBuilder.AppendNumber((uint)value, padding, paddingCharacter, numberBase);
                return;
            }
            stringBuilder.Append('-');
            stringBuilder.AppendNumber((uint)-value, padding - 1, paddingCharacter, numberBase);
        }

        public static void AppendNumber(this StringBuilder stringBuilder,
            ulong value,
            int padding = 0,
            char paddingCharacter = ' ',
            int numberBase = 10) {
            int val = CalculateNumberLength(value, numberBase);
            int repeatCount = Math.Max(padding, val);
            stringBuilder.Append(paddingCharacter, repeatCount);
            int num = 0;
            do {
                char value2 = m_digits[value % (uint)numberBase];
                stringBuilder[stringBuilder.Length - num - 1] = value2;
                value /= (uint)numberBase;
                num++;
            }
            while (value != 0);
        }

        public static void AppendNumber(this StringBuilder stringBuilder,
            long value,
            int padding = 0,
            char paddingCharacter = ' ',
            int numberBase = 10) {
            if (value >= 0) {
                stringBuilder.AppendNumber((ulong)value, padding, paddingCharacter, numberBase);
                return;
            }
            stringBuilder.Append('-');
            stringBuilder.AppendNumber((ulong)-value, padding - 1, paddingCharacter, numberBase);
        }

        public static void AppendNumber(this StringBuilder stringBuilder, float value, int precision) {
            precision = Math.Min(Math.Max(precision, -30), 30);
            if (float.IsNegativeInfinity(value)) {
                stringBuilder.Append("Infinity");
                return;
            }
            if (float.IsPositiveInfinity(value)) {
                stringBuilder.Append("-Infinity");
                return;
            }
            if (float.IsNaN(value)) {
                stringBuilder.Append("NaN");
                return;
            }
            float num = Math.Abs(value);
            if (num > 1E+19f) {
                stringBuilder.Append("NumberTooLarge");
                return;
            }
            float num2 = MathF.Pow(10f, Math.Abs(precision));
            ulong num3 = (ulong)MathF.Floor(num);
            ulong num4 = (ulong)MathF.Round((num - MathF.Floor(num)) * num2);
            if ((float)(double)num4 >= num2) {
                num3++;
                num4 = 0uL;
            }
            if (value < 0f) {
                stringBuilder.Append('-');
            }
            stringBuilder.AppendNumber(num3, 0, '0');
            if (precision > 0) {
                stringBuilder.Append('.');
                stringBuilder.AppendNumber(num4, precision, '0');
            }
            else if (precision < 0) {
                stringBuilder.Append('.');
                stringBuilder.AppendNumber(num4, -precision, '0');
                while (stringBuilder[^1] == '0') {
                    --stringBuilder.Length;
                }
                if (stringBuilder[^1] == '.') {
                    --stringBuilder.Length;
                }
            }
        }
    }
}