namespace Engine {
    public struct Point3 : IEquatable<Point3> {
        public int X;

        public int Y;

        public int Z;

        public static readonly Point3 Zero = default;

        public static readonly Point3 One = new(1, 1, 1);

        public static readonly Point3 UnitX = new(1, 0, 0);

        public static readonly Point3 UnitY = new(0, 1, 0);

        public static readonly Point3 UnitZ = new(0, 0, 1);

        public Point3(int v) {
            X = v;
            Y = v;
            Z = v;
        }

        public Point3(int x, int y, int z) {
            X = x;
            Y = y;
            Z = z;
        }

        public Point3(Vector3 v) {
            X = (int)v.X;
            Y = (int)v.Y;
            Z = (int)v.Z;
        }

        public static implicit operator Point3((int X, int Y, int Z) v) => new(v.X, v.Y, v.Z);

        public override int GetHashCode() => X + Y + Z;

        public override bool Equals(object obj) => obj is Point3 && Equals((Point3)obj);

        public bool Equals(Point3 other) => other.X == X && other.Y == Y && other.Z == Z;

        public override string ToString() => $"{X},{Y},{Z}";

        public static int Dot(Point3 p1, Point3 p2) => p1.X * p2.X + p1.Y * p2.Y + p1.Z * p2.Z;

        public static Point3 Cross(Point3 p1, Point3 p2) => new(p1.Y * p2.Z - p1.Z * p2.Y, p1.Z * p2.X - p1.X * p2.Z, p1.X * p2.Y - p1.Y * p2.X);

        public static Point3 Min(Point3 p, int v) => new(MathUtils.Min(p.X, v), MathUtils.Min(p.Y, v), MathUtils.Min(p.Z, v));

        public static Point3 Min(Point3 p1, Point3 p2) => new(MathUtils.Min(p1.X, p2.X), MathUtils.Min(p1.Y, p2.Y), MathUtils.Min(p1.Z, p2.Z));

        public static Point3 Max(Point3 p, int v) => new(MathUtils.Max(p.X, v), MathUtils.Max(p.Y, v), MathUtils.Max(p.Z, v));

        public static Point3 Max(Point3 p1, Point3 p2) => new(MathUtils.Max(p1.X, p2.X), MathUtils.Max(p1.Y, p2.Y), MathUtils.Max(p1.Z, p2.Z));

        public static int MinElement(Point3 p) => MathUtils.Min(p.X, p.Y, p.Z);

        public static int MaxElement(Point3 p) => MathUtils.Max(p.X, p.Y, p.Z);

        public static bool operator ==(Point3 p1, Point3 p2) => p1.Equals(p2);

        public static bool operator !=(Point3 p1, Point3 p2) => !p1.Equals(p2);

        public static Point3 operator +(Point3 p) => p;

        public static Point3 operator -(Point3 p) => new(-p.X, -p.Y, -p.Z);

        public static Point3 operator +(Point3 p1, Point3 p2) => new(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);

        public static Point3 operator -(Point3 p1, Point3 p2) => new(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);

        public static Point3 operator *(int n, Point3 p) => new(p.X * n, p.Y * n, p.Z * n);

        public static Point3 operator *(Point3 p, int n) => new(p.X * n, p.Y * n, p.Z * n);

        public static Point3 operator *(Point3 p1, Point3 p2) => new(p1.X * p2.X, p1.Y * p2.Y, p1.Z * p2.Z);

        public static Point3 operator /(Point3 p, int n) => new(p.X / n, p.Y / n, p.Z / n);

        public static Point3 operator /(Point3 p1, Point3 p2) => new(p1.X / p2.X, p1.Y / p2.Y, p1.Z / p2.Z);

        public unsafe Span<int> AsSpan() {
            fixed (int* ptr = &X) {
                return new Span<int>(ptr, 3);
            }
        }

        public unsafe int* AsPointer() {
            fixed (int* ptr = &X) {
                return ptr;
            }
        }

        public static implicit operator Vector3(Point3 p) => new(p.X, p.Y, p.Z);

        public static Point3 Round(Vector3 v) => new((int)MathF.Round(v.X), (int)MathF.Round(v.Y), (int)MathF.Round(v.Z));

        public static Point3 Round(float x, float y, float z) => new((int)MathF.Round(x), (int)MathF.Round(y), (int)MathF.Round(z));

        public static Point3 Ceiling(Vector3 v) => new((int)MathF.Ceiling(v.X), (int)MathF.Ceiling(v.Y), (int)MathF.Ceiling(v.Z));

        public static Point3 Ceiling(float x, float y, float z) => new((int)MathF.Ceiling(x), (int)MathF.Ceiling(y), (int)MathF.Ceiling(z));

        public static Point3 Floor(Vector3 v) => new((int)MathF.Floor(v.X), (int)MathF.Floor(v.Y), (int)MathF.Floor(v.Z));

        public static Point3 Floor(float x, float y, float z) => new((int)MathF.Floor(x), (int)MathF.Floor(y), (int)MathF.Floor(z));
    }
}