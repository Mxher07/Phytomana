namespace Game.IContentReader {
    public abstract class IContentReader {
        public abstract string Type { get; }
        public abstract string[] DefaultSuffix { get; }
        public abstract object Get(ContentInfo[] contents);
    }
}