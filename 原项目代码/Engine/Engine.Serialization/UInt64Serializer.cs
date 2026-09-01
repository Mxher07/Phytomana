namespace Engine.Serialization {
    public class UInt64Serializer : ISerializer<ulong> {
        public void Serialize(InputArchive archive, ref ulong value) {
            archive.Serialize(null, ref value);
        }

        public void Serialize(OutputArchive archive, ulong value) {
            archive.Serialize(null, value);
        }
    }
}