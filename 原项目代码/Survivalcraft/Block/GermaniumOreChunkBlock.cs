using Engine;

namespace Game {
    public class GermaniumOreChunkBlock : ChunkBlock {
        public static int Index = 250;

        public GermaniumOreChunkBlock() : base(
            Matrix.CreateRotationX(-1f) * Matrix.CreateRotationZ(1f),
            Matrix.CreateTranslation(0.0625f, 0.4375f, 0f),
            new Color(204, 181, 162),
            false
        ) { }
    }
}