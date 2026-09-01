namespace Game {
    public static class IntervalUtils {
        public static float Normalize(float t) => t - MathF.Floor(t);

        public static float Add(float t, float interval) => Normalize(t + interval);

        public static float Interval(float t1, float t2) => Normalize(t2 - t1);

        public static float Distance(float t1, float t2) => MathF.Min(Interval(t1, t2), Interval(t2, t1));

        public static float Midpoint(float t1, float t2, float factor = 0.5f) => Add(t1, Interval(t1, t2) * factor);

        public static bool IsBetween(float t, float t1, float t2) => Interval(t1, t) < Interval(t1, t2);
    }
}