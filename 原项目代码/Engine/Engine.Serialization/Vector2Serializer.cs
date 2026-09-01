namespace Engine.Serialization {
    public class Vector2Serializer : ISerializer<Vector2> {
        public void Serialize(InputArchive archive, ref Vector2 value) {
            archive.Serialize("X", ref value.X);
            archive.Serialize("Y", ref value.Y);
        }

        public void Serialize(OutputArchive archive, Vector2 value) {
            archive.Serialize("X", value.X);
            archive.Serialize("Y", value.Y);
        }
    }
}