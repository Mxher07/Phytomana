using Engine.Graphics;
using Engine.Media;

namespace Game.IContentReader {
    public class DaeModelReader : IContentReader {
        public override string Type => "Engine.Graphics.Model";
        // 因为 ContentManager.ReaderList 的键是上面的 Type，所以 gltf、glb 也通过此 Reader 读取
        public override string[] DefaultSuffix => ["gltf", "glb", "dae"];

        public override object Get(ContentInfo[] contents) {
            var content = contents[0];
            if (content == null) {
                return null;
            }
            if (GltfLoader.IsGltfFile(content.ContentSuffix)) {
                ModelData modelData = GltfLoader.Load(content.Duplicate(), Path.GetDirectoryName(content.AbsolutePath));
                return Model.Load(modelData, true);
            }
            return Model.Load(content.Duplicate(), true);
        }
    }
}