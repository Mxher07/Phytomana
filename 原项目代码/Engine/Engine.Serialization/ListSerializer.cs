namespace Engine.Serialization {
    public class ListSerializer<T> : ISerializer<List<T>> {
        public void Serialize(InputArchive archive, ref List<T> value) {
            value = [];
            archive.SerializeCollection(null, value);
        }

        public void Serialize(OutputArchive archive, List<T> value) {
            archive.SerializeCollection(null, null, value);
        }
    }
}