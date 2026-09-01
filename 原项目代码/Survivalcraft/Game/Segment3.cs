using Engine;

namespace Game {
    public struct Segment3 {
        public Vector3 Start;

        public Vector3 End;

        public Segment3(Vector3 start, Vector3 end) {
            Start = start;
            End = end;
        }

        public override string ToString() => $"{Start.X}, {Start.Y}, {Start.Z},  {End.X}, {End.Y}, {End.Z}";

        public static float Distance(Segment3 s, Vector3 p) {
            Vector3 v = s.End - s.Start;
            Vector3 v2 = s.Start - p;
            Vector3 v3 = s.End - p;
            float num = Vector3.Dot(v2, v);
            if (num * Vector3.Dot(v3, v) <= 0f) {
                float num2 = v.LengthSquared();
                if (num2 == 0f) {
                    return Vector3.Distance(p, s.Start);
                }
                return MathF.Sqrt(Vector3.Cross(p - s.Start, v).LengthSquared() / num2);
            }
            if (!(num > 0f)) {
                return v3.Length();
            }
            return v2.Length();
        }

        public static Vector3 NearestPoint(Segment3 s, Vector3 p) {
            Vector3 v = s.End - s.Start;
            Vector3 v2 = s.Start - p;
            Vector3 v3 = s.End - p;
            float num = Vector3.Dot(v2, v);
            if (num * Vector3.Dot(v3, v) <= 0f) {
                float num2 = v.LengthSquared();
                if (num2 == 0f) {
                    return s.Start;
                }
                float num3 = MathF.Sqrt(v2.LengthSquared() - Vector3.Cross(p - s.Start, v).LengthSquared() / num2);
                return Vector3.Lerp(s.Start, s.End, num3 / MathF.Sqrt(num2));
            }
            if (!(num > 0f)) {
                return s.End;
            }
            return s.Start;
        }
    }
}