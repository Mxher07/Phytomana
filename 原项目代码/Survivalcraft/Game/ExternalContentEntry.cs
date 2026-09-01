namespace Game {
    public class ExternalContentEntry {
        public ExternalContentType Type;

        public string Path;

        public long Size;

        public DateTime Time;

        public List<ExternalContentEntry> ChildEntries = [];
    }
}