namespace Engine.Serialization {
    public class ArraySerializer<T> {
        public void Serialize(InputArchive archive, ref T[] value) {
            List<T> list = new();
            archive.SerializeCollection(null, list);
            if (value == null) {
                value = list.ToArray();
                return;
            }
            if (list.Count != value.Length) {
                throw new InvalidOperationException("Serializing into an existing array with invalid length.");
            }
            list.CopyTo(value);
        }

        public void Serialize(OutputArchive archive, T[] value) {
            archive.SerializeCollection(null, null, value);
        }
    }
}