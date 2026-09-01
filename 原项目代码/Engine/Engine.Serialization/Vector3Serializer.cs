namespace Engine.Serialization {
    public class Vector3Serializer : ISerializer<Vector3> {
        public void Serialize(InputArchive archive, ref Vector3 value) {
            archive.Serialize("X", ref value.X);
            archive.Serialize("Y", ref value.Y);
            archive.Serialize("Z", ref value.Z);
        }

        public void Serialize(OutputArchive archive, Vector3 value) {
            archive.Serialize("X", value.X);
            archive.Serialize("Y", value.Y);
            archive.Serialize("Z", value.Z);
        }
    }
}