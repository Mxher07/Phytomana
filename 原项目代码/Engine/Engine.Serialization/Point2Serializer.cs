namespace Engine.Serialization {
    public class Point2Serializer : ISerializer<Point2> {
        public void Serialize(InputArchive archive, ref Point2 value) {
            archive.Serialize("X", ref value.X);
            archive.Serialize("Y", ref value.Y);
        }

        public void Serialize(OutputArchive archive, Point2 value) {
            archive.Serialize("X", value.X);
            archive.Serialize("Y", value.Y);
        }
    }
}