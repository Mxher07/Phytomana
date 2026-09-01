using Engine.Audio;

namespace Game.IContentReader {
    public class SoundBufferReader : IContentReader {
        public override string Type => "Engine.Audio.SoundBuffer";
        public override string[] DefaultSuffix => ["flac", "wav", "ogg", "mp3"];
        public override object Get(ContentInfo[] contents) => SoundBuffer.Load(contents[0].Duplicate());
    }
}