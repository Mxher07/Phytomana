using Engine.Graphics;

namespace Game.IContentReader {
    public class CompressedTexture2DReader : IContentReader {
        public override string Type => "Engine.Graphics.CompressedTexture2D";
        public override string[] DefaultSuffix => ["astc", "astcsrgb"];

        public override object Get(ContentInfo[] contents) {
            ContentInfo contentInfo = contents[0];
            string suffix = contentInfo.ContentSuffix.ToLower();
            return suffix switch {
                ".astcsrgb" => CompressedTexture2D.Load(contentInfo.Duplicate(), false),
                _ => CompressedTexture2D.Load(contentInfo.Duplicate())
            };
        }
    }
}