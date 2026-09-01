using Engine;
using Engine.Graphics;

namespace Game {
    static class PathRenderer {
        public struct Point : IEquatable<Point> {
            public Vector2 Position;

            public float InnerRadiusL;

            public float InnerRadiusR;

            public float OuterRadiusL;

            public float OuterRadiusR;

            public Color InnerColorL;

            public Color InnerColorR;

            public Color OuterColorL;

            public Color OuterColorR;

            public float LengthScale;

            public float MiterLimit;

            public float InnerRadius {
                set => InnerRadiusL = InnerRadiusR = value;
            }

            public float OuterRadius {
                set => OuterRadiusL = OuterRadiusR = value;
            }

            public Color InnerColor {
                set => InnerColorL = InnerColorR = value;
            }

            public Color OuterColor {
                set => OuterColorL = OuterColorR = value;
            }

            public Color Color {
                set {
                    Color innerColor = OuterColor = value;
                    InnerColor = innerColor;
                }
            }

            bool IEquatable<Point>.Equals(Point other) {
                if (Position == other.Position
                    && InnerRadiusL == other.InnerRadiusL
                    && InnerRadiusR == other.InnerRadiusR
                    && OuterRadiusL == other.OuterRadiusL
                    && OuterRadiusR == other.OuterRadiusR
                    && InnerColorL == other.InnerColorL
                    && InnerColorR == other.InnerColorR
                    && OuterColorL == other.OuterColorL
                    && OuterColorR == other.OuterColorR
                    && LengthScale == other.LengthScale) {
                    return MiterLimit == other.MiterLimit;
                }
                return false;
            }
        }

        static DynamicArray<Point> TmpPoints = new();

        static DynamicArray<Vector2> TmpPositions = new();

        static DynamicArray<Vector2> TmpNormals = new();

        public static int TrimPathStart(DynamicArray<Vector2> positions, BoundingRectangle rectangle) {
            int count = positions.Count;
            int num = -1;
            for (int i = 0; i < positions.Count; i++) {
                if (!rectangle.Contains(positions[i])) {
                    num = i;
                    break;
                }
            }
            if (num < 0) {
                positions.Count = 0;
            }
            else if (num > 0) {
                positions.RemoveRange(0, num - 1);
                Ray2 ray = new(positions[1], Vector2.Normalize(positions[0] - positions[1]));
                float num2 = ray.Intersection(rectangle) ?? 0f;
                positions[0] = ray.Position + ray.Direction * num2;
            }
            return count - positions.Count;
        }

        public static int TrimPathEnd(DynamicArray<Vector2> positions, BoundingRectangle rectangle) {
            positions.Reverse();
            int result = TrimPathStart(positions, rectangle);
            positions.Reverse();
            return result;
        }

        public static int TrimPathStart(DynamicArray<Vector2> positions, BoundingCircle circle) {
            int count = positions.Count;
            int num = -1;
            for (int i = 0; i < positions.Count; i++) {
                if (!circle.Contains(positions[i])) {
                    num = i;
                    break;
                }
            }
            if (num < 0) {
                positions.Count = 0;
            }
            else if (num > 0) {
                positions.RemoveRange(0, num - 1);
                Ray2 ray = new(positions[1], Vector2.Normalize(positions[0] - positions[1]));
                float num2 = ray.Intersection(circle) ?? 0f;
                positions[0] = ray.Position + ray.Direction * num2;
            }
            return count - positions.Count;
        }

        public static int TrimPathEnd(DynamicArray<Vector2> positions, BoundingCircle circle) {
            positions.Reverse();
            int result = TrimPathStart(positions, circle);
            positions.Reverse();
            return result;
        }

        public static void GeneratePathNormals(DynamicArray<Vector2> positions,
            DynamicArray<bool> visibility,
            bool loop,
            DynamicArray<Vector2> normals) {
            if (positions.Count == 0) {
                return;
            }
            Vector2[] array = positions.Array;
            bool[] array2 = visibility?.Array;
            if (array2 != null) {
                if (loop) {
                    for (int i = 0; i < positions.Count; i++) {
                        int num = (i - 1 + positions.Count) % positions.Count;
                        int num2 = i;
                        int num3 = (i + 1) % positions.Count;
                        normals.Add(array2[num] || array2[num2] ? Normal2(array[num2] - array[num], array[num3] - array[num2]) : default);
                    }
                    return;
                }
                normals.Add(array2[0] ? Normal1(array[1] - array[0]) : default);
                for (int j = 1; j < positions.Count - 1; j++) {
                    normals.Add(array2[j - 1] || array2[j] ? Normal2(array[j] - array[j - 1], array[j + 1] - array[j]) : default);
                }
                if (positions.Count >= 2) {
                    normals.Add(array2[positions.Count - 2] ? Normal1(array[positions.Count - 1] - array[positions.Count - 2]) : default);
                }
            }
            else if (loop) {
                for (int k = 0; k < positions.Count; k++) {
                    int num4 = (k - 1 + positions.Count) % positions.Count;
                    int num5 = k;
                    int num6 = (k + 1) % positions.Count;
                    normals.Add(Normal2(array[num5] - array[num4], array[num6] - array[num5]));
                }
            }
            else {
                normals.Add(Normal1(array[1] - array[0]));
                for (int l = 1; l < positions.Count - 1; l++) {
                    normals.Add(Normal2(array[l] - array[l - 1], array[l + 1] - array[l]));
                }
                if (positions.Count >= 2) {
                    normals.Add(Normal1(array[positions.Count - 1] - array[positions.Count - 2]));
                }
            }
        }

        public static void QueuePath(FlatBatch2D batch,
            DynamicArray<Point> points,
            DynamicArray<bool> visibility,
            bool loop,
            bool flatShading,
            float depth = 0f) {
            TmpPositions.Count = 0;
            foreach (Point point in points) {
                TmpPositions.Add(point.Position);
            }
            TmpNormals.Count = 0;
            GeneratePathNormals(TmpPositions, visibility, loop, TmpNormals);
            QueuePathInternal(
                batch,
                points,
                TmpNormals,
                visibility,
                loop,
                flatShading,
                depth
            );
        }

        public static void QueuePath(FlatBatch2D batch,
            DynamicArray<Point> points,
            DynamicArray<Vector2> normals,
            DynamicArray<bool> visibility,
            bool loop,
            bool flatShading,
            float depth = 0f) {
            QueuePathInternal(
                batch,
                points,
                normals,
                visibility,
                loop,
                flatShading,
                depth
            );
        }

        public static void QueuePath(FlatBatch2D batch,
            DynamicArray<Vector2> positions,
            DynamicArray<bool> visibility,
            bool loop,
            bool flatShading,
            float innerRadius,
            float outerRadius,
            Color innerColor,
            Color outerColor,
            float miterLimit,
            float depth = 0f) {
            QueuePath(
                batch,
                positions,
                visibility,
                loop,
                flatShading,
                innerRadius,
                innerRadius,
                outerRadius,
                outerRadius,
                innerColor,
                innerColor,
                outerColor,
                outerColor,
                miterLimit,
                depth
            );
        }

        public static void QueuePath(FlatBatch2D batch,
            DynamicArray<Vector2> positions,
            DynamicArray<bool> visibility,
            bool loop,
            bool flatShading,
            float innerRadiusL,
            float innerRadiusR,
            float outerRadiusL,
            float outerRadiusR,
            Color innerColorL,
            Color innerColorR,
            Color outerColorL,
            Color outerColorR,
            float miterLimit,
            float depth = 0f) {
            Point point = default;
            point.InnerRadiusL = innerRadiusL;
            point.InnerRadiusR = innerRadiusR;
            point.OuterRadiusL = outerRadiusL;
            point.OuterRadiusR = outerRadiusR;
            point.InnerColorL = innerColorL;
            point.InnerColorR = innerColorR;
            point.OuterColorL = outerColorL;
            point.OuterColorR = outerColorR;
            point.MiterLimit = miterLimit;
            Point value = point;
            TmpPoints.Count = positions.Count;
            for (int i = 0; i < positions.Count; i++) {
                value.Position = positions[i];
                TmpPoints[i] = value;
            }
            TmpNormals.Count = 0;
            GeneratePathNormals(positions, visibility, loop, TmpNormals);
            QueuePathInternal(
                batch,
                TmpPoints,
                TmpNormals,
                visibility,
                loop,
                flatShading,
                depth
            );
        }

        public static void QueuePath(TexturedBatch2D batch,
            DynamicArray<Point> points,
            DynamicArray<bool> visibility,
            bool loop,
            bool flatShading,
            float lengthOffset,
            float depth = 0f) {
            TmpPositions.Count = 0;
            foreach (Point point in points) {
                TmpPositions.Add(point.Position);
            }
            TmpNormals.Count = 0;
            GeneratePathNormals(TmpPositions, visibility, loop, TmpNormals);
            QueuePathInternal(
                batch,
                points,
                TmpNormals,
                visibility,
                loop,
                flatShading,
                lengthOffset,
                depth
            );
        }

        public static void QueuePath(TexturedBatch2D batch,
            DynamicArray<Point> points,
            DynamicArray<Vector2> normals,
            DynamicArray<bool> visibility,
            bool loop,
            bool flatShading,
            float lengthOffset,
            float depth = 0f) {
            QueuePathInternal(
                batch,
                points,
                normals,
                visibility,
                loop,
                flatShading,
                lengthOffset,
                depth
            );
        }

        public static void QueuePath(TexturedBatch2D batch,
            DynamicArray<Vector2> positions,
            DynamicArray<bool> visibility,
            bool loop,
            bool flatShading,
            float innerRadius,
            float outerRadius,
            Color innerColor,
            Color outerColor,
            float lengthScale,
            float lengthOffset,
            float miterLimit,
            float depth = 0f) {
            QueuePath(
                batch,
                positions,
                visibility,
                loop,
                flatShading,
                innerRadius,
                innerRadius,
                outerRadius,
                outerRadius,
                innerColor,
                innerColor,
                outerColor,
                outerColor,
                lengthScale,
                lengthOffset,
                miterLimit,
                depth
            );
        }

        public static void QueuePath(TexturedBatch2D batch,
            DynamicArray<Vector2> positions,
            DynamicArray<bool> visibility,
            bool loop,
            bool flatShading,
            float innerRadiusL,
            float innerRadiusR,
            float outerRadiusL,
            float outerRadiusR,
            Color innerColorL,
            Color innerColorR,
            Color outerColorL,
            Color outerColorR,
            float lengthScale,
            float lengthOffset,
            float miterLimit,
            float depth = 0f) {
            Point point = default;
            point.InnerRadiusL = innerRadiusL;
            point.InnerRadiusR = innerRadiusR;
            point.OuterRadiusL = outerRadiusL;
            point.OuterRadiusR = outerRadiusR;
            point.InnerColorL = innerColorL;
            point.InnerColorR = innerColorR;
            point.OuterColorL = outerColorL;
            point.OuterColorR = outerColorR;
            point.LengthScale = lengthScale;
            point.MiterLimit = miterLimit;
            Point value = point;
            TmpPoints.Count = positions.Count;
            for (int i = 0; i < positions.Count; i++) {
                value.Position = positions[i];
                TmpPoints[i] = value;
            }
            TmpNormals.Count = 0;
            GeneratePathNormals(positions, visibility, loop, TmpNormals);
            QueuePathInternal(
                batch,
                TmpPoints,
                TmpNormals,
                visibility,
                loop,
                flatShading,
                lengthOffset,
                depth
            );
        }

        // ReSharper disable UnusedParameter.Local
        static void QueuePathInternal(FlatBatch2D batch,
                DynamicArray<Point> points,
                DynamicArray<Vector2> normals,
                DynamicArray<bool> visibility,
                bool loop,
                bool flatShading,
                float depth)
            // ReSharper restore UnusedParameter.Local
        {
            int num = loop ? normals.Count : normals.Count - 1;
            for (int i = 0; i < num; i++) {
                if (visibility != null
                    && !visibility[i]) {
                    continue;
                }
                int index = i;
                int index2 = (i + 1) % normals.Count;
                Point point = points[index];
                Point point2 = points[index2];
                Vector2 position = point.Position;
                Vector2 position2 = point2.Position;
                Vector2 vector = normals[index];
                Vector2 vector2 = normals[index2];
                if (vector.LengthSquared() > point.MiterLimit) {
                    vector = Normal1(position2 - position);
                }
                if (vector2.LengthSquared() > point2.MiterLimit) {
                    vector2 = Normal1(position2 - position);
                }
                if (point.OuterRadiusL != 0f
                    || point2.OuterRadiusL != 0f) {
                    Vector2 vector3 = vector * (0f - point.InnerRadiusL);
                    Vector2 vector4 = vector * (0f - point.OuterRadiusL);
                    Vector2 vector5 = vector2 * (0f - point2.InnerRadiusL);
                    Vector2 vector6 = vector2 * (0f - point2.OuterRadiusL);
                    if (flatShading) {
                        batch.QueueQuad(
                            position + vector4,
                            position + vector3,
                            position2 + vector5,
                            position2 + vector6,
                            0f,
                            point.OuterColorR,
                            point.InnerColorR,
                            point.InnerColorR,
                            point.OuterColorR
                        );
                    }
                    else {
                        batch.QueueQuad(
                            position + vector4,
                            position + vector3,
                            position2 + vector5,
                            position2 + vector6,
                            0f,
                            point.OuterColorR,
                            point.InnerColorR,
                            point2.InnerColorR,
                            point2.OuterColorR
                        );
                    }
                }
                if (point.OuterRadiusR != 0f
                    || point2.OuterRadiusR != 0f) {
                    Vector2 vector7 = vector * point.InnerRadiusR;
                    Vector2 vector8 = vector * point.OuterRadiusR;
                    Vector2 vector9 = vector2 * point2.InnerRadiusR;
                    Vector2 vector10 = vector2 * point2.OuterRadiusR;
                    if (flatShading) {
                        batch.QueueQuad(
                            position + vector8,
                            position + vector7,
                            position2 + vector9,
                            position2 + vector10,
                            0f,
                            point.OuterColorR,
                            point.InnerColorR,
                            point.InnerColorR,
                            point.OuterColorR
                        );
                    }
                    else {
                        batch.QueueQuad(
                            position + vector8,
                            position + vector7,
                            position2 + vector9,
                            position2 + vector10,
                            0f,
                            point.OuterColorR,
                            point.InnerColorR,
                            point2.InnerColorR,
                            point2.OuterColorR
                        );
                    }
                }
            }
        }

        // ReSharper disable UnusedParameter.Local
        static void QueuePathInternal(TexturedBatch2D batch,
                DynamicArray<Point> points,
                DynamicArray<Vector2> normals,
                DynamicArray<bool> visibility,
                bool loop,
                bool flatShading,
                float lengthOffset,
                float depth)
            // ReSharper restore UnusedParameter.Local
        {
            float num = lengthOffset;
            int num2 = loop ? normals.Count : normals.Count - 1;
            for (int i = 0; i < num2; i++) {
                if (visibility != null
                    && !visibility[i]) {
                    continue;
                }
                int index = i;
                int index2 = (i + 1) % normals.Count;
                Point point = points[index];
                Point point2 = points[index2];
                Vector2 position = point.Position;
                Vector2 position2 = point2.Position;
                float num3 = 1f / point.LengthScale;
                Vector2 vector = position2 - position;
                float num4 = vector.Length();
                Vector2 v = vector / num4 * num3;
                Vector2 vector2 = normals[index];
                Vector2 vector3 = normals[index2];
                if (vector2.LengthSquared() > point.MiterLimit) {
                    vector2 = Normal1(position2 - position);
                }
                if (vector3.LengthSquared() > point2.MiterLimit) {
                    vector3 = Normal1(position2 - position);
                }
                Vector2 vector4 = new(num, 0.5f);
                num += num4 * num3;
                Vector2 vector5 = new(num, 0.5f);
                if (point.OuterRadiusL > 0f
                    || point2.OuterRadiusL > 0f) {
                    Vector2 vector6 = vector2 * (0f - point.InnerRadiusL);
                    Vector2 vector7 = vector2 * (0f - point.OuterRadiusL);
                    Vector2 vector8 = vector3 * (0f - point2.InnerRadiusL);
                    Vector2 vector9 = vector3 * (0f - point2.OuterRadiusL);
                    Vector2 vector10 = new(Vector2.Dot(vector6, v), -0.5f * point.InnerRadiusL / point.OuterRadiusL);
                    Vector2 vector11 = new(Vector2.Dot(vector7, v), -0.5f);
                    Vector2 vector12 = new(Vector2.Dot(vector8, v), -0.5f * point2.InnerRadiusL / point2.OuterRadiusL);
                    Vector2 vector13 = new(Vector2.Dot(vector9, v), -0.5f);
                    if (flatShading) {
                        batch.QueueQuad(
                            position + vector7,
                            position + vector6,
                            position2 + vector8,
                            position2 + vector9,
                            0f,
                            vector4 + vector11,
                            vector4 + vector10,
                            vector5 + vector12,
                            vector5 + vector13,
                            point.OuterColorR,
                            point.InnerColorR,
                            point.InnerColorR,
                            point.OuterColorR
                        );
                    }
                    else {
                        batch.QueueQuad(
                            position + vector7,
                            position + vector6,
                            position2 + vector8,
                            position2 + vector9,
                            0f,
                            vector4 + vector11,
                            vector4 + vector10,
                            vector5 + vector12,
                            vector5 + vector13,
                            point.OuterColorR,
                            point.InnerColorR,
                            point2.InnerColorR,
                            point2.OuterColorR
                        );
                    }
                }
                if (point.OuterRadiusR != 0f
                    || point2.OuterRadiusR != 0f) {
                    Vector2 vector14 = vector2 * point.InnerRadiusR;
                    Vector2 vector15 = vector2 * point.OuterRadiusR;
                    Vector2 vector16 = vector3 * point2.InnerRadiusR;
                    Vector2 vector17 = vector3 * point2.OuterRadiusR;
                    Vector2 vector18 = new(Vector2.Dot(vector14, v), 0.5f * point.InnerRadiusR / point.OuterRadiusR);
                    Vector2 vector19 = new(Vector2.Dot(vector15, v), 0.5f);
                    Vector2 vector20 = new(Vector2.Dot(vector16, v), 0.5f * point2.InnerRadiusR / point2.OuterRadiusR);
                    Vector2 vector21 = new(Vector2.Dot(vector17, v), 0.5f);
                    if (flatShading) {
                        batch.QueueQuad(
                            position + vector15,
                            position + vector14,
                            position2 + vector16,
                            position2 + vector17,
                            0f,
                            vector4 + vector19,
                            vector4 + vector18,
                            vector5 + vector20,
                            vector5 + vector21,
                            point.OuterColorR,
                            point.InnerColorR,
                            point.InnerColorR,
                            point.OuterColorR
                        );
                    }
                    else {
                        batch.QueueQuad(
                            position + vector15,
                            position + vector14,
                            position2 + vector16,
                            position2 + vector17,
                            0f,
                            vector4 + vector19,
                            vector4 + vector18,
                            vector5 + vector20,
                            vector5 + vector21,
                            point.OuterColorR,
                            point.InnerColorR,
                            point2.InnerColorR,
                            point2.OuterColorR
                        );
                    }
                }
            }
        }

        static Vector2 Normal1(Vector2 d) => Vector2.Perpendicular(Vector2.Normalize(d));

        static Vector2 Normal2(Vector2 d1, Vector2 d2) {
            d1 = Vector2.Normalize(d1);
            float num = MathF.Tan(((float)Math.PI - Vector2.Angle(d1, d2)) / 2f);
            return Vector2.Perpendicular(d1) - d1 / num;
        }
    }
}