namespace Engine.Serialization {
    public class KeyValuePairSerializer<K, V> : ISerializer<KeyValuePair<K, V>> {
        public void Serialize(InputArchive archive, ref KeyValuePair<K, V> value) {
            K value2 = default;
            V value3 = default;
            archive.Serialize("K", ref value2);
            archive.Serialize("V", ref value3);
            value = new KeyValuePair<K, V>(value2, value3);
        }

        public void Serialize(OutputArchive archive, KeyValuePair<K, V> value) {
            archive.Serialize("K", value.Key);
            archive.Serialize("V", value.Value);
        }
    }
}