namespace Engine.Serialization {
    public class GuidSerializer : ISerializer<Guid> {
        public void Serialize(InputArchive archive, ref Guid value) {
            byte[] value2 = null;
            archive.Serialize(null, 16, ref value2);
            value = new Guid(value2);
        }

        public void Serialize(OutputArchive archive, Guid value) {
            archive.Serialize(null, 16, value.ToByteArray());
        }
    }
}