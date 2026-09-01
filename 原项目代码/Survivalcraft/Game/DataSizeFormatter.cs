using Engine;

namespace Game {
    public static class DataSizeFormatter {
        public static string Format(long bytes, int significantDigits = 3) {
            if (bytes < 1024) {
                return "1KB";
            }
            if (bytes < 1048576) {
                float num = bytes / 1024f;
                return string.Format(PrepareFormatString(num, "KB", 0), num);
            }
            if (bytes < 1073741824) {
                float num2 = bytes / 1024f / 1024f;
                return string.Format(PrepareFormatString(num2, "MB", significantDigits), num2);
            }
            float num3 = bytes / 1024f / 1024f / 1024f;
            return string.Format(PrepareFormatString(num3, "GB", significantDigits), num3);
        }

        public static string PrepareFormatString(float value, string unit, int significantDigits) {
            int num = (int)(MathF.Log10(value) + 1f);
            int num2 = MathUtils.Max(significantDigits - num, 0);
            if (num2 > 0) {
                return $"{{0:0.{new string('#', num2)}}}{unit}";
            }
            return $"{{0:0}}{unit}";
        }
    }
}