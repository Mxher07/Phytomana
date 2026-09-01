namespace Engine {
    public static class MathUtils {
        public const float PI = (float)Math.PI;

        public const float E = (float)Math.E;

        public static int Min(int x1, int x2) {
            if (x1 >= x2) {
                return x2;
            }
            return x1;
        }

        public static int Min(int x1, int x2, int x3) => Min(Min(x1, x2), x3);

        public static int Min(int x1, int x2, int x3, int x4) => Min(Min(Min(x1, x2), x3), x4);

        public static int Max(int x1, int x2) {
            if (x1 <= x2) {
                return x2;
            }
            return x1;
        }

        public static int Max(int x1, int x2, int x3) => Max(Max(x1, x2), x3);

        public static int Max(int x1, int x2, int x3, int x4) => Max(Max(Max(x1, x2), x3), x4);

        public static int Clamp(int x, int min, int max) {
            if (x >= min) {
                if (x <= max) {
                    return x;
                }
                return max;
            }
            return min;
        }

        public static int Sign(int x) => Math.Sign(x);

        public static int Abs(int x) => Math.Abs(x);

        public static int Sqr(int x) => x * x;

        public static bool IsPowerOf2(uint x) {
            if (x != 0) {
                return (x & (x - 1)) == 0;
            }
            return false;
        }

        public static uint NextPowerOf2(uint x) {
            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x++;
            return x;
        }

        public static int Hash(int key) => (int)Hash((uint)key);

        public static uint Hash(uint key) {
            key ^= key >> 16;
            key *= 2146121005;
            key ^= key >> 15;
            key *= 2221713035u;
            key ^= key >> 16;
            return key;
        }

        public static long Min(long x1, long x2) {
            if (x1 >= x2) {
                return x2;
            }
            return x1;
        }

        public static long Min(long x1, long x2, long x3) => Min(Min(x1, x2), x3);

        public static long Min(long x1, long x2, long x3, long x4) => Min(Min(Min(x1, x2), x3), x4);

        public static long Max(long x1, long x2) {
            if (x1 <= x2) {
                return x2;
            }
            return x1;
        }

        public static long Max(long x1, long x2, long x3) => Max(Max(x1, x2), x3);

        public static long Max(long x1, long x2, long x3, long x4) => Max(Max(Max(x1, x2), x3), x4);

        public static long Clamp(long x, long min, long max) {
            if (x >= min) {
                if (x <= max) {
                    return x;
                }
                return max;
            }
            return min;
        }

        public static long Sign(long x) => Math.Sign(x);

        public static long Abs(long x) => Math.Abs(x);

        public static long Sqr(long x) => x * x;

        public static bool IsPowerOf2(long x) {
            if (x > 0) {
                return (x & (x - 1)) == 0;
            }
            return false;
        }

        public static ulong NextPowerOf2(ulong x) {
            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x |= x >> 32;
            x++;
            return x;
        }

        public static float Min(float x1, float x2) {
            if (!(x1 < x2)) {
                return x2;
            }
            return x1;
        }

        public static float Min(float x1, float x2, float x3) => Min(Min(x1, x2), x3);

        public static float Min(float x1, float x2, float x3, float x4) => Min(Min(Min(x1, x2), x3), x4);

        public static float Max(float x1, float x2) {
            if (!(x1 > x2)) {
                return x2;
            }
            return x1;
        }

        public static float Max(float x1, float x2, float x3) => Max(Max(x1, x2), x3);

        public static float Max(float x1, float x2, float x3, float x4) => Max(Max(Max(x1, x2), x3), x4);

        public static float Clamp(float x, float min, float max) {
            if (!(x < min)) {
                if (!(x > max)) {
                    return x;
                }
                return max;
            }
            return min;
        }

        public static float Saturate(float x) {
            if (!(x < 0f)) {
                if (!(x > 1f)) {
                    return x;
                }
                return 1f;
            }
            return 0f;
        }

        public static float Sign(float x) => Math.Sign(x);

        public static float Abs(float x) => Math.Abs(x);

        public static float Floor(float x) => (float)Math.Floor(x);

        public static float Ceiling(float x) => (float)Math.Ceiling(x);

        public static float Round(float x) => (float)Math.Round(x);

        public static float Remainder(float x, float y) => x - Floor(x / y) * y;

        public static float Sqr(float x) => x * x;

        public static float Sqrt(float x) => (float)Math.Sqrt(x);

        public static float Sin(float x) => (float)Math.Sin(x);

        public static float Cos(float x) => (float)Math.Cos(x);

        public static float Tan(float x) => (float)Math.Tan(x);

        public static float Asin(float x) => (float)Math.Asin(x);

        public static float Acos(float x) => (float)Math.Acos(x);

        public static float Atan(float x) => (float)Math.Atan(x);

        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);

        public static float Exp(float n) => (float)Math.Exp(n);

        public static float Log(float x) => (float)Math.Log(x);

        public static float Log10(float x) => (float)Math.Log10(x);

        public static float Pow(float x, float n) => (float)Math.Pow(x, n);

        public static float PowSign(float x, float n) => Sign(x) * Pow(Abs(x), n);

        public static float Lerp(float x1, float x2, float f) => x1 + (x2 - x1) * f;

        public static float SmoothStep(float min, float max, float x) {
            x = Clamp((x - min) / (max - min), 0f, 1f);
            return x * x * (3f - 2f * x);
        }

        public static float CatmullRom(float v1, float v2, float v3, float v4, float f) {
            float num = f * f;
            float num2 = num * f;
            return 0.5f * (2f * v2 + (v3 - v1) * f + (2f * v1 - 5f * v2 + 4f * v3 - v4) * num + (3f * v2 - v1 - 3f * v3 + v4) * num2);
        }

        public static float NormalizeAngle(float angle) {
            angle = (float)Math.IEEERemainder(angle, 6.2831854820251465);
            if (angle > (float)Math.PI) {
                angle -= (float)Math.PI * 2f;
            }
            else if (angle <= -(float)Math.PI) {
                angle += (float)Math.PI * 2f;
            }
            return angle;
        }

        public static float Sigmoid(float x, float steepness) {
            if (x <= 0f) {
                return 0f;
            }
            if (x >= 1f) {
                return 1f;
            }
            float num = Exp(steepness);
            float num2 = Exp(2f * steepness * x);
            return num * (num2 - 1f) / ((num - 1f) * (num2 + num));
        }

        public static float DegToRad(float degrees) => degrees / 180f * (float)Math.PI;

        public static float RadToDeg(float radians) => radians * 180f / (float)Math.PI;

        public static double Min(double x1, double x2) {
            if (!(x1 < x2)) {
                return x2;
            }
            return x1;
        }

        public static double Min(double x1, double x2, double x3) => Min(Min(x1, x2), x3);

        public static double Min(double x1, double x2, double x3, double x4) => Min(Min(Min(x1, x2), x3), x4);

        public static double Max(double x1, double x2) {
            if (!(x1 > x2)) {
                return x2;
            }
            return x1;
        }

        public static double Max(double x1, double x2, double x3) => Max(Max(x1, x2), x3);

        public static double Max(double x1, double x2, double x3, double x4) => Max(Max(Max(x1, x2), x3), x4);

        public static double Clamp(double x, double min, double max) {
            if (!(x < min)) {
                if (!(x > max)) {
                    return x;
                }
                return max;
            }
            return min;
        }

        public static double Saturate(double x) {
            if (!(x < 0.0)) {
                if (!(x > 1.0)) {
                    return x;
                }
                return 1.0;
            }
            return 0.0;
        }

        public static double Sign(double x) => Math.Sign(x);

        public static double Abs(double x) => Math.Abs(x);

        public static double Floor(double x) => Math.Floor(x);

        public static double Ceiling(double x) => Math.Ceiling(x);

        public static double Round(double x) => Math.Round(x);

        public static double Remainder(double x, double y) => x - Floor(x / y) * y;

        public static double Sqr(double x) => x * x;

        public static double Sqrt(double x) => Math.Sqrt(x);

        public static double Sin(double x) => Math.Sin(x);

        public static double Cos(double x) => Math.Cos(x);

        public static double Tan(double x) => Math.Tan(x);

        public static double Asin(double x) => Math.Asin(x);

        public static double Acos(double x) => Math.Acos(x);

        public static double Atan(double x) => Math.Atan(x);

        public static double Atan2(double y, double x) => Math.Atan2(y, x);

        public static double Exp(double n) => Math.Exp(n);

        public static double Log(double x) => Math.Log(x);

        public static double Log10(double x) => Math.Log10(x);

        public static double Pow(double x, double n) => Math.Pow(x, n);

        public static double PowSign(double x, double n) => Sign(x) * Pow(Abs(x), n);

        public static double Lerp(double x1, double x2, double f) => x1 + (x2 - x1) * f;

        public static double SmoothStep(double min, double max, double x) {
            x = Clamp((x - min) / (max - min), 0.0, 1.0);
            return x * x * (3.0 - 2.0 * x);
        }

        public static double CatmullRom(double v1, double v2, double v3, double v4, double f) {
            double num = f * f;
            double num2 = num * f;
            return 0.5 * (2.0 * v2 + (v3 - v1) * f + (2.0 * v1 - 5.0 * v2 + 4.0 * v3 - v4) * num + (3.0 * v2 - v1 - 3.0 * v3 + v4) * num2);
        }

        public static double NormalizeAngle(double angle) {
            angle = Math.IEEERemainder(angle, Math.PI * 2.0);
            if (angle > 3.1415927410125732) {
                angle -= Math.PI * 2.0;
            }
            else if (angle <= -Math.PI) {
                angle += Math.PI * 2.0;
            }
            return angle;
        }

        public static double DegToRad(double degrees) => degrees / 180.0 * Math.PI;

        public static double RadToDeg(double radians) => radians * 180.0 / Math.PI;

        public static float LinearStep(float zero, float one, float f) => Saturate((f - zero) / (one - zero));
    }
}