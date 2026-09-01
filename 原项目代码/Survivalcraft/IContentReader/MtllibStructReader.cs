namespace Game.IContentReader {
    public class MtllibStructReader : IContentReader {
        public override string Type => "Game.MtllibStruct";
        public override string[] DefaultSuffix => ["mtl"];
        public override object Get(ContentInfo[] contents) => MtllibStruct.Load(contents[0].Duplicate());
    }
}