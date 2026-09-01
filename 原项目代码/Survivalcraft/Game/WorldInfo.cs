namespace Game {
    public class WorldInfo {
        public string DirectoryName = string.Empty;

        public long Size;

        public DateTime LastSaveTime;

        public string SerializationVersion = string.Empty;

        public string APIVersion = string.Empty;

        public WorldSettings WorldSettings = new();

        public List<PlayerInfo> PlayerInfos = [];
    }
}