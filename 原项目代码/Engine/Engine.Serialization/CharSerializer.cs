namespace Engine.Serialization {
    public class CharSerializer : ISerializer<char> {
        public void Serialize(InputArchive archive, ref char value) {
            archive.Serialize(null, ref value);
        }

        public void Serialize(OutputArchive archive, char value) {
            archive.Serialize(null, value);
        }
    }
}