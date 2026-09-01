namespace Engine.Serialization {
    public class Point3Serializer : ISerializer<Point3> {
        public void Serialize(InputArchive archive, ref Point3 value) {
            archive.Serialize("X", ref value.X);
            archive.Serialize("Y", ref value.Y);
            archive.Serialize("Z", ref value.Z);
        }

        public void Serialize(OutputArchive archive, Point3 value) {
            archive.Serialize("X", value.X);
            archive.Serialize("Y", value.Y);
            archive.Serialize("Z", value.Z);
        }
    }
}