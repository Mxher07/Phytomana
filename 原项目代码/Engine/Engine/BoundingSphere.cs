namespace Engine {
    public struct BoundingSphere : IEquatable<BoundingSphere> {
        public Vector3 Center;

        public float Radius;

        public BoundingSphere(Vector3 center, float radius) {
            Center = center;
            Radius = radius;
        }

        public override bool Equals(object obj) => obj is BoundingSphere sphere && Equals(sphere);

        public override int GetHashCode() => Center.GetHashCode() + Radius.GetHashCode();

        public bool Equals(BoundingSphere other) => Center == other.Center && Radius == other.Radius;

        public override string ToString() => $"{Center},{Radius}";

        public static bool operator ==(BoundingSphere s1, BoundingSphere s2) => s1.Equals(s2);

        public static bool operator !=(BoundingSphere s1, BoundingSphere s2) => !s1.Equals(s2);
    }
}