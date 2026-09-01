namespace Engine.Serialization {
    public class Int64Serializer : ISerializer<long> {
        public void Serialize(InputArchive archive, ref long value) {
            archive.Serialize(null, ref value);
        }

        public void Serialize(OutputArchive archive, long value) {
            archive.Serialize(null, value);
        }
    }
}