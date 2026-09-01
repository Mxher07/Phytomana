using Engine;
using Engine.Graphics;
using Engine.Media;

namespace Game.IContentReader {
    /// <summary>
    ///     位图字体读取器<br/>
    ///     可使用此工具生成 https://github.com/XiaofengdiZhu/SC-Chinese
    /// </summary>
    public class BitmapFontReader : IContentReader {
        public override string Type => "Engine.Media.BitmapFont";
        public override string[] DefaultSuffix => ["lst", "astc", "astcsrgb", "webp", "png"];

        public override object Get(ContentInfo[] contents) {
            if (contents.Length != 2) {
                throw new Exception("not matches content count");
            }
            ContentInfo glyphs;
            ContentInfo texture;
            if (contents[0].ContentSuffix == ".lst") {
                glyphs = contents[0];
                texture = contents[1];
            }
            else {
                glyphs = contents[1];
                texture = contents[0];
            }
            return BitmapFont.Initialize(ContentManager.Get<Texture2D>(texture.ContentPath, texture.ContentSuffix), glyphs.Duplicate(), Vector2.Zero);
        }
    }
}