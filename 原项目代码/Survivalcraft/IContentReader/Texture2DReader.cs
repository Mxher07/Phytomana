using Engine.Graphics;
using Engine.Media;

namespace Game.IContentReader {
    public class Texture2DReader : IContentReader {
        public override string Type => "Engine.Graphics.Texture2D";
        public override string[] DefaultSuffix => ["astc", "astcsrgb", "webp", "png", "jpg", "jpeg"];

        public override object Get(ContentInfo[] contents) {
            ContentInfo contentInfo = contents[0];
            if (contentInfo.ContentPath == "Fonts/Pericles") {
                return Texture2D.Load(ContentManager.Get<Image>(contentInfo.ContentPath, contentInfo.ContentSuffix), 3);
            }
            string suffix = contentInfo.ContentSuffix.ToLower();
            return suffix switch {
                ".astc" => CompressedTexture2D.Load(contentInfo.Duplicate()),
                ".astcsrgb" => CompressedTexture2D.Load(contentInfo.Duplicate(), false),
                _ => Texture2D.Load(ContentManager.Get<Image>(contentInfo.ContentPath, suffix))
            };
        }
    }
}