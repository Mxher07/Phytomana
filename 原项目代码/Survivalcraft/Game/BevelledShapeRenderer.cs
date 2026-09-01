using Engine;
using Engine.Graphics;

namespace Game {
    public static class BevelledShapeRenderer {
        public struct Point : IEquatable<Point> {
            public Vector2 Position;

            public float RoundingRadius;

            public int RoundingCount;

            public bool Equals(Point other) {
                if (Position == other.Position
                    && RoundingRadius == other.RoundingRadius) {
                    return RoundingCount == other.RoundingCount;
                }
                return false;
            }
        }

        static FlatBatch2D TmpBatch = new();

        static DynamicArray<Point> TmpQuadPoints = new();

        static DynamicArray<Point> TmpPoints = new();

        static DynamicArray<Vector2> TmpPositions = new();

        static DynamicArray<Vector2> TmpPositions2 = new();

        static DynamicArray<Vector2> TmpNormals = new();

        static DynamicArray<int> TmpIndices = new();

        static DynamicArray<int> TmpIndicesTriangulation = new();

        static DynamicArray<PathRenderer.Point> TmpPathPoints = new();

        public static void QueueShape(FlatBatch2D batch,
            IEnumerable<Point> points,
            float pixelsPerUnit,
            float antialiasSize,
            float bevelSize,
            bool flatShading,
            Color centerColor,
            Color bevelColor,
            float directional,
            float ambient) {
            if ((bevelColor == Color.Transparent || bevelSize == 0f)
                && centerColor == Color.Transparent) {
                return;
            }
            TmpPoints.Count = 0;
            TmpPoints.AddRange(points);
            TmpPositions.Count = 0;
            RoundCorners(TmpPoints, true, pixelsPerUnit, MathF.Abs(bevelSize), TmpPositions);
            RemoveDuplicates(TmpPositions);
            TmpNormals.Count = 0;
            PathRenderer.GeneratePathNormals(TmpPositions, null, true, TmpNormals);
            if (bevelColor != Color.Transparent
                && bevelSize != 0f) {
                float num = MathF.Abs(bevelSize);
                Vector2 directionToLight = Vector2.Normalize(bevelSize > 0f ? new Vector2(-1f, -2f) : -new Vector2(-1f, -2f));
                TmpPathPoints.Count = 0;
                GeneratePathPoints(
                    TmpPositions,
                    0f,
                    0f,
                    0f,
                    num,
                    bevelColor,
                    bevelColor,
                    bevelColor,
                    bevelColor,
                    0f,
                    float.PositiveInfinity,
                    TmpPathPoints
                );
                LightPoints(TmpPathPoints, true, flatShading, directionToLight, directional, ambient);
                PathRenderer.QueuePath(batch, TmpPathPoints, TmpNormals, null, true, flatShading);
                TmpPositions2.Count = 0;
                for (int i = 0; i < TmpNormals.Count; i++) {
                    TmpPositions2.Add(TmpPathPoints[i].Position + TmpNormals[i] * num);
                }
                TmpIndices.Count = 0;
                Triangulate(TmpPositions2, TmpIndices);
                int count = batch.TriangleVertices.Count;
                for (int j = 0; j < TmpPositions2.Count; j++) {
                    batch.TriangleVertices.Add(new VertexPositionColor(new Vector3(TmpPositions2[j], 0f), centerColor));
                }
                for (int k = 0; k < TmpIndices.Count; k++) {
                    batch.TriangleIndices.Add(TmpIndices[k] + count);
                }
                if (antialiasSize > 0f) {
                    TmpPathPoints.Count = 0;
                    float outerRadiusR = num + antialiasSize;
                    GeneratePathPoints(
                        TmpPositions,
                        0f,
                        num,
                        0f,
                        outerRadiusR,
                        bevelColor,
                        bevelColor,
                        Color.Transparent,
                        Color.Transparent,
                        0f,
                        float.PositiveInfinity,
                        TmpPathPoints
                    );
                    LightPoints(TmpPathPoints, true, flatShading, directionToLight, directional, ambient);
                    PathRenderer.QueuePath(batch, TmpPathPoints, TmpNormals, null, true, flatShading);
                    TmpPathPoints.Count = 0;
                    GeneratePathPoints(
                        TmpPositions,
                        0f,
                        0f,
                        antialiasSize,
                        0f,
                        bevelColor,
                        bevelColor,
                        Color.Transparent,
                        Color.Transparent,
                        0f,
                        float.PositiveInfinity,
                        TmpPathPoints
                    );
                    LightPoints(TmpPathPoints, true, flatShading, directionToLight, directional, ambient);
                    PathRenderer.QueuePath(batch, TmpPathPoints, TmpNormals, null, true, flatShading);
                }
            }
            else if (centerColor != Color.Transparent) {
                TmpIndices.Count = 0;
                Triangulate(TmpPositions, TmpIndices);
                int count2 = batch.TriangleVertices.Count;
                for (int l = 0; l < TmpPositions.Count; l++) {
                    batch.TriangleVertices.Add(new VertexPositionColor(new Vector3(TmpPositions[l], 0f), centerColor));
                }
                for (int m = 0; m < TmpIndices.Count; m++) {
                    batch.TriangleIndices.Add(TmpIndices[m] + count2);
                }
                if (antialiasSize > 0f) {
                    TmpPathPoints.Count = 0;
                    GeneratePathPoints(
                        TmpPositions,
                        0f,
                        0f,
                        antialiasSize,
                        0f,
                        centerColor,
                        centerColor,
                        Color.Transparent,
                        Color.Transparent,
                        0f,
                        float.PositiveInfinity,
                        TmpPathPoints
                    );
                    PathRenderer.QueuePath(batch, TmpPathPoints, TmpNormals, null, true, flatShading);
                }
            }
        }

        public static void QueueShapeShadow(FlatBatch2D batch, IEnumerable<Point> points, float pixelsPerUnit, float shadowSize, Color shadowColor) {
            TmpPoints.Count = 0;
            TmpPoints.AddRange(points);
            TmpPositions.Count = 0;
            RoundCorners(TmpPoints, true, pixelsPerUnit, shadowSize, TmpPositions);
            RemoveDuplicates(TmpPositions);
            TmpNormals.Count = 0;
            PathRenderer.GeneratePathNormals(TmpPositions, null, true, TmpNormals);
            TmpPathPoints.Count = 0;
            GeneratePathPoints(
                TmpPositions,
                0f,
                0f,
                shadowSize,
                0f,
                shadowColor,
                Color.Transparent,
                Color.Transparent,
                Color.Transparent,
                0f,
                float.PositiveInfinity,
                TmpPathPoints
            );
            PathRenderer.QueuePath(batch, TmpPathPoints, TmpNormals, null, true, true);
            TmpIndices.Count = 0;
            Triangulate(TmpPositions, TmpIndices);
            int count = batch.TriangleVertices.Count;
            for (int i = 0; i < TmpPositions.Count; i++) {
                batch.TriangleVertices.Add(new VertexPositionColor(new Vector3(TmpPositions[i], 0f), shadowColor));
            }
            for (int j = 0; j < TmpIndices.Count; j++) {
                batch.TriangleIndices.Add(TmpIndices[j] + count);
            }
        }

        public static void QueueShape(TexturedBatch2D batch,
            IEnumerable<Point> points,
            Vector2 textureScale,
            Vector2 textureOffset,
            float pixelsPerUnit,
            float antialiasSize,
            float bevelSize,
            bool flatShading,
            Color centerColor,
            Color bevelColor,
            float directional,
            float ambient) {
            TmpBatch.Clear();
            QueueShape(
                TmpBatch,
                points,
                pixelsPerUnit,
                antialiasSize,
                bevelSize,
                flatShading,
                centerColor,
                bevelColor,
                directional,
                ambient
            );
            Vector2 vector = 1f / (textureScale * new Vector2(batch.Texture.Width, batch.Texture.Height));
            int count = batch.TriangleVertices.Count;
            foreach (VertexPositionColor triangleVertex in TmpBatch.TriangleVertices) {
                DynamicArray<VertexPositionColorTexture> triangleVertices = batch.TriangleVertices;
                VertexPositionColorTexture item = new() { Position = triangleVertex.Position, Color = triangleVertex.Color };
                Vector3 position = triangleVertex.Position;
                item.TexCoord = position.XY * vector + textureOffset;
                triangleVertices.Add(item);
            }
            foreach (int triangleIndex in TmpBatch.TriangleIndices) {
                batch.TriangleIndices.Add(triangleIndex + count);
            }
        }

        public static void QueueQuad(FlatBatch2D batch,
            Vector2 p1,
            Vector2 p2,
            float pixelsPerUnit,
            float antialiasSize,
            float bevelSize,
            float roundingRadius,
            int roundingCount,
            bool flatShading,
            Color centerColor,
            Color bevelColor,
            float directional,
            float ambient) {
            TmpQuadPoints.Count = 0;
            TmpQuadPoints.Add(new Point { Position = new Vector2(p1.X, p1.Y), RoundingRadius = roundingRadius, RoundingCount = roundingCount });
            TmpQuadPoints.Add(new Point { Position = new Vector2(p2.X, p1.Y), RoundingRadius = roundingRadius, RoundingCount = roundingCount });
            TmpQuadPoints.Add(new Point { Position = new Vector2(p2.X, p2.Y), RoundingRadius = roundingRadius, RoundingCount = roundingCount });
            TmpQuadPoints.Add(new Point { Position = new Vector2(p1.X, p2.Y), RoundingRadius = roundingRadius, RoundingCount = roundingCount });
            QueueShape(
                batch,
                TmpQuadPoints,
                pixelsPerUnit,
                antialiasSize,
                bevelSize,
                flatShading,
                centerColor,
                bevelColor,
                directional,
                ambient
            );
        }

        public static void QueueQuad(TexturedBatch2D batch,
            Vector2 p1,
            Vector2 p2,
            Vector2 textureScale,
            Vector2 textureOffset,
            float pixelsPerUnit,
            float antialiasSize,
            float bevelSize,
            float roundingRadius,
            int roundingCount,
            bool flatShading,
            Color centerColor,
            Color bevelColor,
            float directional,
            float ambient) {
            TmpQuadPoints.Count = 0;
            TmpQuadPoints.Add(new Point { Position = new Vector2(p1.X, p1.Y), RoundingRadius = roundingRadius, RoundingCount = roundingCount });
            TmpQuadPoints.Add(new Point { Position = new Vector2(p2.X, p1.Y), RoundingRadius = roundingRadius, RoundingCount = roundingCount });
            TmpQuadPoints.Add(new Point { Position = new Vector2(p2.X, p2.Y), RoundingRadius = roundingRadius, RoundingCount = roundingCount });
            TmpQuadPoints.Add(new Point { Position = new Vector2(p1.X, p2.Y), RoundingRadius = roundingRadius, RoundingCount = roundingCount });
            QueueShape(
                batch,
                TmpQuadPoints,
                textureScale,
                textureOffset,
                pixelsPerUnit,
                antialiasSize,
                bevelSize,
                flatShading,
                centerColor,
                bevelColor,
                directional,
                ambient
            );
        }

        static void RemoveDuplicates(DynamicArray<Vector2> positions) {
            int count = 0;
            Vector2? vector = null;
            for (int i = 0; i < positions.Count; i++) {
                Vector2 value = positions[i];
                if (value != vector) {
                    positions[count++] = value;
                    vector = value;
                }
            }
            positions.Count = count;
        }

        static void Triangulate(DynamicArray<Vector2> source, DynamicArray<int> destination) {
            TmpIndicesTriangulation.Count = source.Count;
            for (int i = 0; i < source.Count; i++) {
                TmpIndicesTriangulation.Array[i] = i;
            }
            while (true) {
                int num = TmpIndicesTriangulation.Count - 1;
                int num2;
                int num3;
                int num4;
                while (true) {
                    if (num >= 3) {
                        num2 = TmpIndicesTriangulation[(num - 1 + TmpIndicesTriangulation.Count) % TmpIndicesTriangulation.Count];
                        num3 = TmpIndicesTriangulation[num];
                        num4 = TmpIndicesTriangulation[(num + 1) % TmpIndicesTriangulation.Count];
                        Vector2 vector = source[num2];
                        Vector2 vector2 = source[num3];
                        Vector2 vector3 = source[num4];
                        if (Vector2.Cross(vector2 - vector, vector3 - vector2) >= 0f) {
                            break;
                        }
                        num--;
                        continue;
                    }
                    if (TmpIndicesTriangulation.Count == 3) {
                        destination.AddRange(TmpIndicesTriangulation);
                    }
                    return;
                }
                destination.Add(num2);
                destination.Add(num3);
                destination.Add(num4);
                TmpIndicesTriangulation.RemoveAt(num);
            }
        }

        static void LightPoints(DynamicArray<PathRenderer.Point> points,
            bool loop,
            bool flatShading,
            Vector2 directionToLight,
            float directional,
            float ambient) {
            if (flatShading) {
                for (int i = 0; i < points.Count; i++) {
                    int index = i;
                    int index2 = loop ? (i + 1) % points.Count : MathUtils.Min(i + 1, points.Count - 1);
                    PathRenderer.Point value = points[index];
                    Vector2 v = -Vector2.Perpendicular(Vector2.Normalize(points[index2].Position - value.Position));
                    float num = directional * Vector2.Dot(v, directionToLight);
                    Color color = new(new Vector3(num + ambient));
                    value.InnerColorL *= color;
                    value.InnerColorR *= color;
                    value.OuterColorL *= color;
                    value.OuterColorR *= color;
                    points[index] = value;
                }
                return;
            }
            for (int j = 0; j < points.Count; j++) {
                int index3 = loop ? (j - 1 + points.Count) % points.Count : MathUtils.Max(j - 1, 0);
                int index4 = j;
                int index5 = loop ? (j + 1) % points.Count : MathUtils.Min(j + 1, points.Count - 1);
                PathRenderer.Point point = points[index3];
                PathRenderer.Point value2 = points[index4];
                PathRenderer.Point point2 = points[index5];
                Vector2 obj = value2.Position != point.Position ? Vector2.Normalize(value2.Position - point.Position) : Vector2.Zero;
                Vector2 vector = point2.Position != value2.Position ? Vector2.Normalize(point2.Position - value2.Position) : Vector2.Zero;
                Vector2 v2 = -Vector2.Perpendicular(Vector2.Normalize(obj + vector));
                Color color2 = new(new Vector3(directional * Vector2.Dot(v2, directionToLight) + ambient));
                value2.InnerColorL *= color2;
                value2.InnerColorR *= color2;
                value2.OuterColorL *= color2;
                value2.OuterColorR *= color2;
                points[index4] = value2;
            }
        }

        static void GeneratePathPoints(DynamicArray<Vector2> positions,
            float innerRadiusL,
            float innerRadiusR,
            float outerRadiusL,
            float outerRadiusR,
            Color innerColorL,
            Color innerColorR,
            Color outerColorL,
            Color outerColorR,
            float lengthScale,
            float miterLimit,
            DynamicArray<PathRenderer.Point> result) {
            foreach (Vector2 position in positions) {
                result.Add(
                    new PathRenderer.Point {
                        Position = position,
                        InnerRadiusL = innerRadiusL,
                        InnerRadiusR = innerRadiusR,
                        OuterRadiusL = outerRadiusL,
                        OuterRadiusR = outerRadiusR,
                        InnerColorL = innerColorL,
                        InnerColorR = innerColorR,
                        OuterColorL = outerColorL,
                        OuterColorR = outerColorR,
                        LengthScale = lengthScale,
                        MiterLimit = miterLimit
                    }
                );
            }
        }

        static void RoundCorners(DynamicArray<Point> points, bool loop, float pixelsPerUnit, float bevelSize, DynamicArray<Vector2> result) {
            int num = loop ? points.Count : points.Count - 2;
            for (int i = 0; i < num; i++) {
                Point point = points[i];
                Point point2 = points[(i + 1) % points.Count];
                Point point3 = points[(i + 2) % points.Count];
                RoundCorner(
                    point.Position,
                    point2.Position,
                    point3.Position,
                    point2.RoundingRadius,
                    point2.RoundingCount,
                    pixelsPerUnit,
                    bevelSize,
                    result
                );
            }
        }

        static void RoundCorner(Vector2 p0,
            Vector2 p1,
            Vector2 p2,
            float radius,
            int count,
            float pixelsPerUnit,
            float bevelSize,
            DynamicArray<Vector2> result) {
            Vector2 vector = p0 - p1;
            Vector2 vector2 = p2 - p1;
            float num = vector.Length();
            float num2 = vector2.Length();
            float num3 = MathUtils.Min(num * 0.49f, num2 * 0.49f, radius);
            if (num3 > 0f) {
                Vector2 vector3 = vector / num;
                Vector2 vector4 = vector2 / num2;
                Vector2 vector5 = p1 + vector3 * num3;
                Vector2 vector6 = p1 + vector4 * num3;
                Vector2 v = p1 - vector5;
                Vector2 v2 = p1 - vector6;
                Line2 l = new(vector5, vector5 + Vector2.Perpendicular(v));
                Line2 l2 = new(vector6, vector6 + Vector2.Perpendicular(v2));
                Vector2? vector7 = Line2.Intersection(l, l2);
                if (vector7.HasValue) {
                    float num4 = Vector2.Distance(vector7.Value, vector5);
                    if (count < 0) {
                        float num5 = 0.25f;
                        float num6 = (num4 + bevelSize) * pixelsPerUnit;
                        float num7 = MathF.Acos(1f - num5 / num6);
                        count = !float.IsNaN(num7) ? (int)Math.Clamp(MathF.Ceiling(((float)Math.PI / num7 - 4f) / 4f), 0f, 50f) : 0;
                    }
                    float num8 = Vector2.Angle(vector5 - vector7.Value, Vector2.UnitY);
                    float num9 = MathUtils.NormalizeAngle(Vector2.Angle(vector6 - vector7.Value, Vector2.UnitY) - num8);
                    result.Add(vector5);
                    for (int i = 1; i <= count; i++) {
                        float x = num8 + num9 * i / (count + 1);
                        Vector2 item = vector7.Value + new Vector2(num4 * MathF.Sin(x), num4 * MathF.Cos(x));
                        result.Add(item);
                    }
                    result.Add(vector6);
                    return;
                }
            }
            result.Add(p1);
        }
    }
}