using System.Diagnostics;
using Engine;

namespace Game {
    public class Random {
        public static int m_counter = (int)(Stopwatch.GetTimestamp() + DateTime.Now.Ticks);

        public uint m_s0;

        public uint m_s1;

        public ulong State {
            get => m_s0 + ((ulong)m_s1 << 32);
            set {
                m_s0 = (uint)value;
                m_s1 = (uint)(value >> 32);
            }
        }

        public Random() {
            Seed();
        }

        public Random(int seed) {
            Seed(seed);
        }

        public void Seed() {
            Seed(m_counter++);
        }

        public void Seed(int seed) {
            m_s0 = MathUtils.Hash((uint)seed);
            m_s1 = MathUtils.Hash((uint)(seed + 1));
        }

        public int Sign() => Int() % 2 * 2 - 1;

        public bool Bool() => (Int() & 1) != 0;

        public bool Bool(float probability) => Int() / 2.147484E+09f < probability;

        public uint UInt() {
            uint s = m_s0;
            uint s2 = m_s1;
            s2 ^= s;
            m_s0 = RotateLeft(s, 26) ^ s2 ^ (s2 << 9);
            m_s1 = RotateLeft(s2, 13);
            return RotateLeft((uint)((int)s * -1640531525), 5) * 5;
        }

        public int Int() => (int)(UInt() & 0x7FFFFFFF);

        public int Int(int bound) => (int)(Int() * (long)bound / 2147483648L);

        public int Int(int min, int max) => (int)(min + Int() * (long)(max - min + 1) / 2147483648L);

        public float Float() => Int() / 2.147484E+09f;

        public float Float(float min, float max) => min + Float() * (max - min);

        public float NormalFloat(float mean, float stddev) {
            float num = Float();
            if (num < 0.5) {
                float num2 = MathF.Sqrt(-2f * MathF.Log(num));
                float num3 = 0.322232425f + num2 * (1f + num2 * (0.3422421f + num2 * (0.0204231218f + num2 * 4.536422E-05f)));
                float num4 = 0.09934846f + num2 * (0.588581562f + num2 * (0.5311035f + num2 * (0.103537753f + num2 * 0.00385607f)));
                return mean + stddev * (num3 / num4 - num2);
            }
            float num5 = MathF.Sqrt(-2f * MathF.Log(1f - num));
            float num6 = 0.322232425f + num5 * (1f + num5 * (0.3422421f + num5 * (0.0204231218f + num5 * 4.536422E-05f)));
            float num7 = 0.09934846f + num5 * (0.588581562f + num5 * (0.5311035f + num5 * (0.103537753f + num5 * 0.00385607f)));
            return mean - stddev * (num6 / num7 - num5);
        }

        public Vector2 Vector2() {
            float num;
            float num2;
            float num3;
            float num4;
            float num5;
            do {
                num = 2f * Float() - 1f;
                num2 = 2f * Float() - 1f;
                num3 = num * num;
                num4 = num2 * num2;
                num5 = num3 + num4;
            }
            while (!(num5 < 1f));
            float num6 = 1f / num5;
            return new Vector2((num3 - num4) * num6, 2f * num * num2 * num6);
        }

        public Vector2 Vector2(float length) => Engine.Vector2.Normalize(Vector2()) * length;

        public Vector2 Vector2(float minLength, float maxLength) => Engine.Vector2.Normalize(Vector2()) * Float(minLength, maxLength);

        public Vector3 Vector3() {
            float num;
            float num2;
            float num3;
            do {
                num = 2f * Float() - 1f;
                num2 = 2f * Float() - 1f;
                num3 = num * num + num2 * num2;
            }
            while (!(num3 < 1f));
            float num4 = MathF.Sqrt(1f - num3);
            return new Vector3(2f * num * num4, 2f * num2 * num4, 1f - 2f * num3);
        }

        public Vector3 Vector3(float length) => Engine.Vector3.Normalize(Vector3()) * length;

        public Vector3 Vector3(float minLength, float maxLength) => Engine.Vector3.Normalize(Vector3()) * Float(minLength, maxLength);

        public static uint RotateLeft(uint x, int k) => (x << k) | (x >> (32 - k));
    }
}