namespace Engine.Serialization {
    class Enum32Serializer<T> where T : unmanaged, Enum {
        public unsafe void Serialize(InputArchive archive, ref T value) {
            int value2 = 0;
            archive.Serialize(null, ref value2);
            value = *(T*)(&value2);
        }

        public unsafe void Serialize(OutputArchive archive, T value) {
            archive.Serialize(null, *(int*)&value);
        }
    }
}