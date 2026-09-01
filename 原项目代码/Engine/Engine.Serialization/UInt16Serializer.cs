namespace Engine.Serialization {
    public class UInt16Serializer : ISerializer<ushort> {
        public void Serialize(InputArchive archive, ref ushort value) {
            archive.Serialize(null, ref value);
        }

        public void Serialize(OutputArchive archive, ushort value) {
            archive.Serialize(null, value);
        }
    }
}