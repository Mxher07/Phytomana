using Engine;

namespace Game {
    public struct Segment2 {
        public Vector2 Start;

        public Vector2 End;

        public Segment2(Vector2 start, Vector2 end) {
            Start = start;
            End = end;
        }

        public override string ToString() => $"{Start.X}, {Start.Y},  {End.X}, {End.Y}";

        public static float Distance(Segment2 s, Vector2 p) {
            Vector2 v = s.End - s.Start;
            Vector2 v2 = s.Start - p;
            Vector2 v3 = s.End - p;
            float num = Vector2.Dot(v2, v);
            if (num * Vector2.Dot(v3, v) <= 0f) {
                float num2 = v.LengthSquared();
                if (num2 == 0f) {
                    return Vector2.Distance(p, s.Start);
                }
                return MathF.Abs(Vector2.Cross(p - s.Start, v)) / MathF.Sqrt(num2);
            }
            if (!(num > 0f)) {
                return v3.Length();
            }
            return v2.Length();
        }

        public static bool Intersection(Segment2 s1, Segment2 s2, out Vector2 result) {
            Vector2 v = s1.End - s1.Start;
            Vector2 v2 = s2.End - s2.Start;
            float num = Vector2.Cross(v, v2);
            if (num == 0f) {
                result = Vector2.Zero;
                return false;
            }
            float num2 = 1f / num;
            float num3 = (v2.X * (s1.Start.Y - s2.Start.Y) - v2.Y * (s1.Start.X - s2.Start.X)) * num2;
            float num4 = (v.X * (s1.Start.Y - s2.Start.Y) - v.Y * (s1.Start.X - s2.Start.X)) * num2;
            if (num3 < 0f
                || num3 > 1f
                || num4 < 0f
                || num4 > 1f) {
                result = Vector2.Zero;
                return false;
            }
            result = new Vector2(s1.Start.X + num3 * v.X, s1.Start.Y + num3 * v.Y);
            return true;
        }

        public static Vector2 NearestPoint(Segment2 s, Vector2 p) {
            Vector2 v = s.End - s.Start;
            Vector2 v2 = s.Start - p;
            Vector2 v3 = s.End - p;
            float num = Vector2.Dot(v2, v);
            if (num * Vector2.Dot(v3, v) <= 0f) {
                float num2 = v.Length();
                if (num2 == 0f) {
                    return s.Start;
                }
                float num3 = MathF.Sqrt(v2.LengthSquared() - MathUtils.Sqr(MathF.Abs(Vector2.Cross(p - s.Start, v)) / num2));
                return Vector2.Lerp(s.Start, s.End, num3 / num2);
            }
            if (!(num > 0f)) {
                return s.End;
            }
            return s.Start;
        }
    }
}