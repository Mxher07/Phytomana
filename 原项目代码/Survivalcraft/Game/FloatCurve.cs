using Engine;
using Engine.Serialization;

namespace Game {
    public struct FloatCurve {
        [HumanReadableConverter(typeof(FloatCurve))]
        public class HumanReadableConverter : IHumanReadableConverter {
            public string ConvertToString(object value) =>
                Engine.Serialization.HumanReadableConverter.ValuesListToString('|', ((FloatCurve)value).Points ?? []);

            public object ConvertFromString(Type type, string data) =>
                new FloatCurve(Engine.Serialization.HumanReadableConverter.ValuesListFromString<Vector2>('|', data));
        }

        public Vector2[] Points;

        public FloatCurve(params Vector2[] points) => Points = points.ToArray();

        public float Sample(float x) {
            if (Points == null
                || Points.Length == 0) {
                return 0f;
            }
            int num = -1;
            for (int i = 0; i < Points.Length; i++) {
                if (Points[i].X > x) {
                    num = i;
                    break;
                }
            }
            if (num < 0) {
                return Points[Points.Length - 1].Y;
            }
            if (num == 0) {
                return Points[0].Y;
            }
            Vector2 vector = Points[num - 1];
            Vector2 vector2 = Points[num];
            return MathUtils.Lerp(vector.Y, vector2.Y, MathUtils.LinearStep(vector.X, vector2.X, x));
        }
    }
}