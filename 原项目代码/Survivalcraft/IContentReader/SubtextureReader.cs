using Engine;
using Engine.Graphics;

namespace Game.IContentReader {
    public class SubtextureReader : IContentReader {
        public override string[] DefaultSuffix => ["astc", "astcsrgb", "webp", "txt", "png"];
        public override string Type => "Game.Subtexture";

        public override object Get(ContentInfo[] contents) {
            if (contents[0].ContentPath.Contains("Textures/Atlas/")) {
                return TextureAtlasManager.GetSubtexture(contents[0].ContentPath);
            }
            return new Subtexture(ContentManager.Get<Texture2D>(contents[0].ContentPath), Vector2.Zero, Vector2.One);
        }
    }
}