namespace Engine.Serialization {
    public class BooleanSerializer : ISerializer<bool> {
        public void Serialize(InputArchive archive, ref bool value) {
            archive.Serialize(null, ref value);
        }

        public void Serialize(OutputArchive archive, bool value) {
            archive.Serialize(null, value);
        }
    }
}