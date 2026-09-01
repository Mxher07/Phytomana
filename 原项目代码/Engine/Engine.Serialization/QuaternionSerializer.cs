namespace Engine.Serialization {
    public class QuaternionSerializer : ISerializer<Quaternion> {
        public void Serialize(InputArchive archive, ref Quaternion value) {
            archive.Serialize("X", ref value.X);
            archive.Serialize("Y", ref value.Y);
            archive.Serialize("Z", ref value.Z);
            archive.Serialize("W", ref value.W);
        }

        public void Serialize(OutputArchive archive, Quaternion value) {
            archive.Serialize("X", value.X);
            archive.Serialize("Y", value.Y);
            archive.Serialize("Z", value.Z);
            archive.Serialize("W", value.W);
        }
    }
}