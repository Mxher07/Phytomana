namespace Engine.Serialization {
    class Enum64Serializer<T> where T : unmanaged, Enum {
        public unsafe void Serialize(InputArchive archive, ref T value) {
            long value2 = 0L;
            archive.Serialize(null, ref value2);
            value = *(T*)(&value2);
        }

        public unsafe void Serialize(OutputArchive archive, T value) {
            archive.Serialize(null, *(long*)&value);
        }
    }
}