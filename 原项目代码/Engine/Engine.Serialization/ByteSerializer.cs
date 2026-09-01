namespace Engine.Serialization {
    public class ByteSerializer : ISerializer<byte> {
        public void Serialize(InputArchive archive, ref byte value) {
            archive.Serialize(null, ref value);
        }

        public void Serialize(OutputArchive archive, byte value) {
            archive.Serialize(null, value);
        }
    }
}