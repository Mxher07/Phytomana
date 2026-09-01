using System.Diagnostics;
using Engine.Media;

namespace Game.IContentReader {
    public class ImageReader : IContentReader {
        public override string Type => "Engine.Media.Image";
        public override string[] DefaultSuffix => ["webp", "png", "jpg", "jpeg"];

        public override object Get(ContentInfo[] contents) {
            ContentInfo contentInfo = contents[0];
            Image result;
            if (contentInfo.InUse.TryEnter()) {
                result = Image.Load(contentInfo.Duplicate());
                contentInfo.InUse.Exit();
            }
            else {
                while (!contentInfo.InUse.TryEnter()) {
                    Thread.Sleep(10);
                }
                result = ContentManager.Get<Image>(contentInfo.ContentPath, contentInfo.ContentSuffix);
            }
            return result;
        }
    }
}