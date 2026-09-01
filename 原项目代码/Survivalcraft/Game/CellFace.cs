using Engine;

namespace Game {
    public struct CellFace : IEquatable<CellFace> {
        public int X;

        public int Y;

        public int Z;

        public int Face;

        public static readonly int[] m_oppositeFaces = [2, 3, 0, 1, 5, 4];

        public static readonly Point3[] m_faceToPoint3 = [new(0, 0, 1), new(1, 0, 0), new(0, 0, -1), new(-1, 0, 0), new(0, 1, 0), new(0, -1, 0)];

        public static readonly Vector3[] m_faceToVector3 = [
            new(0f, 0f, 1f), new(1f, 0f, 0f), new(0f, 0f, -1f), new(-1f, 0f, 0f), new(0f, 1f, 0f), new(0f, -1f, 0f)
        ];

        public static readonly int[][] m_faceToTangents = [[1, 4, 3, 5], [4, 0, 5, 2], [4, 1, 5, 3], [0, 4, 2, 5], [0, 1, 2, 3], [1, 0, 3, 2]];

        public Point3 Point {
            get => new(X, Y, Z);
            set {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        public CellFace(int x, int y, int z, int face) {
            X = x;
            Y = y;
            Z = z;
            Face = face;
        }

        public CellFace(Point3 point, int face) {
            X = point.X;
            Y = point.Y;
            Z = point.Z;
            Face = face;
        }

        public static int OppositeFace(int face) => m_oppositeFaces[face];

        public static Point3 FaceToPoint3(int face) => m_faceToPoint3[face];

        public static Vector3 FaceToVector3(int face) => m_faceToVector3[face];

        public static int[] FaceToTangents(int face) => m_faceToTangents[face];

        public static int Point3ToFace(Point3 p, int maxFace = 5) {
            for (int i = 0; i < maxFace; i++) {
                if (m_faceToPoint3[i] == p) {
                    return i;
                }
            }
            throw new InvalidOperationException("Invalid Point3.");
        }

        public static int Vector3ToFace(Vector3 v, int maxFace = 5) {
            float num = -1f / 0f;
            int result = 0;
            for (int i = 0; i <= maxFace; i++) {
                float num2 = Vector3.Dot(m_faceToVector3[i], v);
                if (num2 > num) {
                    result = i;
                    num = num2;
                }
            }
            return result;
        }

        public static CellFace FromAxisAndDirection(int x, int y, int z, int axis, float direction) {
            CellFace result = default;
            result.X = x;
            result.Y = y;
            result.Z = z;
            switch (axis) {
                case 0: result.Face = direction > 0f ? 1 : 3; break;
                case 1: result.Face = direction > 0f ? 4 : 5; break;
                case 2: result.Face = !(direction > 0f) ? 2 : 0; break;
            }
            return result;
        }

        public Plane CalculatePlane() {
            switch (Face) {
                case 0: return new Plane(new Vector3(0f, 0f, 1f), -(Z + 1));
                case 1: return new Plane(new Vector3(-1f, 0f, 0f), X + 1);
                case 2: return new Plane(new Vector3(0f, 0f, -1f), Z);
                case 3: return new Plane(new Vector3(1f, 0f, 0f), -X);
                case 4: return new Plane(new Vector3(0f, 1f, 0f), -(Y + 1));
                default: return new Plane(new Vector3(0f, -1f, 0f), Y);
            }
        }

        public Vector3 GetFaceCenter(float offset = 0f) {
            return new Vector3(X + 0.5f, Y + 0.5f, Z + 0.5f) + FaceToVector3(Face) * (0.5f + offset);
        }

        public Vector3[] GetFourVertices(float size = 1f, float offset = 0f) {
            float halfSize = size * 0.5f;
            Vector3 center = GetFaceCenter(offset);
            int[] tangents = FaceToTangents(Face);
            Vector3 tangent1 = FaceToVector3(tangents[0]) * halfSize;
            Vector3 tangent2 = FaceToVector3(tangents[1]) * halfSize;
            Vector3[] result = new Vector3[4];
            result[0] = center - tangent1 - tangent2;
            result[1] = center + tangent1 - tangent2;
            result[2] = center + tangent1 + tangent2;
            result[3] = center - tangent1 + tangent2;
            return result;
        }

        public Vector3[] GetSixVertices(float size = 1f, float offset = 0f) {
            float halfSize = size * 0.5f;
            Vector3 center = GetFaceCenter(offset);
            int[] tangents = FaceToTangents(Face);
            Vector3 tangent1 = FaceToVector3(tangents[0]) * halfSize;
            Vector3 tangent2 = FaceToVector3(tangents[1]) * halfSize;
            Vector3 v0 = center - tangent1 - tangent2;
            Vector3 v1 = center + tangent1 - tangent2;
            Vector3 v2 = center + tangent1 + tangent2;
            Vector3 v3 = center - tangent1 + tangent2;
            return [
                // Triangle 1
                v0,
                v1,
                v2,
                // Triangle 2
                v2,
                v3,
                v0
            ];
        }

        public override int GetHashCode() => (X << 11) + (Y << 7) + (Z << 3) + Face;

        public override bool Equals(object obj) {
            if (!(obj is CellFace)) {
                return false;
            }
            return Equals((CellFace)obj);
        }

        public bool Equals(CellFace other) {
            if (other.X == X
                && other.Y == Y
                && other.Z == Z) {
                return other.Face == Face;
            }
            return false;
        }

        public override string ToString() => $"{X}, {Y}, {Z}, face {Face}";

        public static bool operator ==(CellFace c1, CellFace c2) => c1.Equals(c2);

        public static bool operator !=(CellFace c1, CellFace c2) => !c1.Equals(c2);
    }
}