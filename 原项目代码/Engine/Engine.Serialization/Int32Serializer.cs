namespace Engine.Serialization {
    public class Int32Serializer : ISerializer<int> {
        public void Serialize(InputArchive archive, ref int value) {
            archive.Serialize(null, ref value);
        }

        public void Serialize(OutputArchive archive, int value) {
            archive.Serialize(null, value);
        }
    }
}