namespace Engine.Serialization {
    public class Vector4Serializer : ISerializer<Vector4> {
        public void Serialize(InputArchive archive, ref Vector4 value) {
            archive.Serialize("X", ref value.X);
            archive.Serialize("Y", ref value.Y);
            archive.Serialize("Z", ref value.Z);
            archive.Serialize("W", ref value.W);
        }

        public void Serialize(OutputArchive archive, Vector4 value) {
            archive.Serialize("X", value.X);
            archive.Serialize("Y", value.Y);
            archive.Serialize("Z", value.Z);
            archive.Serialize("W", value.W);
        }
    }
}