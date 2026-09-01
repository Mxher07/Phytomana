namespace Engine {
    public struct BoundingCircle : IEquatable<BoundingCircle> {
        public Vector2 Center;

        public float Radius;

        public BoundingCircle(Vector2 center, float radius) {
            Center = center;
            Radius = radius;
        }

        public override bool Equals(object obj) => obj is BoundingCircle circle && Equals(circle);

        public override int GetHashCode() => Center.GetHashCode() + Radius.GetHashCode();

        public bool Equals(BoundingCircle other) => Center == other.Center && Radius == other.Radius;

        public override string ToString() => $"{Center},{Radius}";

        public bool Contains(Vector2 p) => Vector2.DistanceSquared(Center, p) <= Radius * Radius;

        public static bool operator ==(BoundingCircle c1, BoundingCircle c2) => c1.Equals(c2);

        public static bool operator !=(BoundingCircle c1, BoundingCircle c2) => !c1.Equals(c2);
    }
}