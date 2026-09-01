namespace Engine.Serialization {
    public interface ISerializable {
        void Serialize(InputArchive archive);

        void Serialize(OutputArchive archive);
    }
}