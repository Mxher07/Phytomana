namespace Engine.Serialization {
    public class UInt32Serializer : ISerializer<uint> {
        public void Serialize(InputArchive archive, ref uint value) {
            archive.Serialize(null, ref value);
        }

        public void Serialize(OutputArchive archive, uint value) {
            archive.Serialize(null, value);
        }
    }
}