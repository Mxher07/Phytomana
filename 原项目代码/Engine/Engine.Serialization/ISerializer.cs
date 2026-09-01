namespace Engine.Serialization {
    public interface ISerializer<T> {
        void Serialize(InputArchive archive, ref T value);

        void Serialize(OutputArchive archive, T value);
    }
}