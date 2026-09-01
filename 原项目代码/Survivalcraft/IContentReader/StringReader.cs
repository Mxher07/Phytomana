namespace Game.IContentReader {
    public class StringReader : IContentReader {
        public override string Type => "System.String";
        public override string[] DefaultSuffix => ["txt"];
        public override object Get(ContentInfo[] contents) => new StreamReader(contents[0].Duplicate()).ReadToEnd();
    }
}