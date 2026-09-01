namespace Game.IContentReader {
    public class ObjModelReader : IContentReader {
        public override string Type => "Game.ObjModel";
        public override string[] DefaultSuffix => ["obj"];
        public override object Get(ContentInfo[] contents) => Game.ObjModelReader.Load(contents[0].Duplicate());
    }
}