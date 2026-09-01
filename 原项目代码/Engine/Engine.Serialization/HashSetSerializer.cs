namespace Engine.Serialization {
    public class HashSetSerializer<T> : ISerializer<HashSet<T>> {
        public void Serialize(InputArchive archive, ref HashSet<T> value) {
            value = [];
            archive.SerializeCollection(null, value);
        }

        public void Serialize(OutputArchive archive, HashSet<T> value) {
            archive.SerializeCollection(null, null, value);
        }
    }
}