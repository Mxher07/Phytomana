namespace Engine {
    public struct Point2 : IEquatable<Point2> {
        public int X;

        public int Y;

        public static readonly Point2 Zero = default;

        public static readonly Point2 One = new(1, 1);

        public static readonly Point2 UnitX = new(1, 0);

        public static readonly Point2 UnitY = new(0, 1);

        public Point2(int v) {
            X = v;
            Y = v;
        }

        public Point2(int x, int y) {
            X = x;
            Y = y;
        }

        public static implicit operator Point2((int X, int Y) v) => new(v.X, v.Y);

        public override int GetHashCode() => X + Y;

        public override bool Equals(object obj) => obj is Point2 && Equals((Point2)obj);

        public bool Equals(Point2 other) => other.X == X && other.Y == Y;

        public override string ToString() => $"{X},{Y}";

        public static int Dot(Point2 p1, Point2 p2) => p1.X * p2.X + p1.Y * p2.Y;

        public static int Cross(Point2 p1, Point2 p2) => p1.X * p2.Y - p1.Y * p2.X;

        public static Point2 Perpendicular(Point2 p) => new(-p.Y, p.X);

        public static Point2 Min(Point2 p, int v) => new(MathUtils.Min(p.X, v), MathUtils.Min(p.Y, v));

        public static Point2 Min(Point2 p1, Point2 p2) => new(MathUtils.Min(p1.X, p2.X), MathUtils.Min(p1.Y, p2.Y));

        public static Point2 Max(Point2 p, int v) => new(MathUtils.Max(p.X, v), MathUtils.Max(p.Y, v));

        public static Point2 Max(Point2 p1, Point2 p2) => new(MathUtils.Max(p1.X, p2.X), MathUtils.Max(p1.Y, p2.Y));

        public static int MinElement(Point2 p) => MathUtils.Min(p.X, p.Y);

        public static int MaxElement(Point2 p) => MathUtils.Max(p.X, p.Y);

        public static bool operator ==(Point2 p1, Point2 p2) => p1.Equals(p2);

        public static bool operator !=(Point2 p1, Point2 p2) => !p1.Equals(p2);

        public static Point2 operator +(Point2 p) => p;

        public static Point2 operator -(Point2 p) => new(-p.X, -p.Y);

        public static Point2 operator +(Point2 p1, Point2 p2) => new(p1.X + p2.X, p1.Y + p2.Y);

        public static Point2 operator -(Point2 p1, Point2 p2) => new(p1.X - p2.X, p1.Y - p2.Y);

        public static Point2 operator *(int n, Point2 p) => new(p.X * n, p.Y * n);

        public static Point2 operator *(Point2 p, int n) => new(p.X * n, p.Y * n);

        public static Point2 operator *(Point2 p1, Point2 p2) => new(p1.X * p2.X, p1.Y * p2.Y);

        public static Point2 operator /(Point2 p, int n) => new(p.X / n, p.Y / n);

        public static Point2 operator /(Point2 p1, Point2 p2) => new(p1.X / p2.X, p1.Y / p2.Y);

        public unsafe Span<int> AsSpan() {
            fixed (int* ptr = &X) {
                return new Span<int>(ptr, 2);
            }
        }

        public unsafe int* AsPointer() {
            fixed (int* ptr = &X) {
                return ptr;
            }
        }

        public static implicit operator Vector2(Point2 p) => new(p.X, p.Y);

        public static Point2 Round(Vector2 v) => new((int)MathF.Round(v.X), (int)MathF.Round(v.Y));

        public static Point2 Round(float x, float y) => new((int)MathF.Round(x), (int)MathF.Round(y));

        public static Point2 Ceiling(Vector2 v) => new((int)MathF.Ceiling(v.X), (int)MathF.Ceiling(v.Y));

        public static Point2 Ceiling(float x, float y) => new((int)MathF.Ceiling(x), (int)MathF.Ceiling(y));

        public static Point2 Floor(Vector2 v) => new((int)MathF.Floor(v.X), (int)MathF.Floor(v.Y));

        public static Point2 Floor(float x, float y) => new((int)MathF.Floor(x), (int)MathF.Floor(y));
    }
}