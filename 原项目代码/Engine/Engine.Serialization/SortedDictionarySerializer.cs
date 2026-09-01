namespace Engine.Serialization {
    public class SortedDictionarySerializer<K, V> : ISerializer<SortedDictionary<K, V>> {
        public void Serialize(InputArchive archive, ref SortedDictionary<K, V> value) {
            value = [];
            archive.SerializeDictionary(null, value);
        }

        public void Serialize(OutputArchive archive, SortedDictionary<K, V> value) {
            archive.SerializeDictionary(null, value);
        }
    }
}