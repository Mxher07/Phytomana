namespace Engine.Serialization {
    public class NullableSerializer<T> : ISerializer<T?> where T : struct {
        public void Serialize(InputArchive archive, ref T? value) {
            bool value2 = false;
            archive.Serialize("HasValue", ref value2);
            if (value2) {
                T value3 = default;
                archive.Serialize("Value", ref value3);
                value = value3;
            }
        }

        public void Serialize(OutputArchive archive, T? value) {
            if (value.HasValue) {
                archive.Serialize("HasValue", true);
                archive.Serialize("Value", value.Value);
            }
            else {
                archive.Serialize("HasValue", false);
            }
        }
    }
}