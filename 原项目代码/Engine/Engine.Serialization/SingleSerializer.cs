namespace Engine.Serialization {
    public class SingleSerializer : ISerializer<float> {
        public void Serialize(InputArchive archive, ref float value) {
            archive.Serialize(null, ref value);
        }

        public void Serialize(OutputArchive archive, float value) {
            archive.Serialize(null, value);
        }
    }
}