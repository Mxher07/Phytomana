namespace Engine.Serialization {
    public class Int16Serializer : ISerializer<short> {
        public void Serialize(InputArchive archive, ref short value) {
            archive.Serialize(null, ref value);
        }

        public void Serialize(OutputArchive archive, short value) {
            archive.Serialize(null, value);
        }
    }
}