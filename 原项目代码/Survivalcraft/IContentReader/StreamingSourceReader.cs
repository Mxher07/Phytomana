using Engine.Media;

namespace Game.IContentReader {
    public class StreamingSourceReader : IContentReader {
        public override string Type => "Engine.Media.StreamingSource";
        public override string[] DefaultSuffix => ["flac", "wav", "ogg", "mp3"];
        public override object Get(ContentInfo[] contents) => SoundData.Stream(contents[0].Duplicate());
    }
}