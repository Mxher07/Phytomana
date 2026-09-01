namespace Engine.Serialization {
    public class SByteSerializer : ISerializer<sbyte> {
        public void Serialize(InputArchive archive, ref sbyte value) {
            archive.Serialize(null, ref value);
        }

        public void Serialize(OutputArchive archive, sbyte value) {
            archive.Serialize(null, value);
        }
    }
}