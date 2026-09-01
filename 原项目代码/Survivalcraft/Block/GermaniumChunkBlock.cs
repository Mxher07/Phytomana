using Engine;

namespace Game {
    public class GermaniumChunkBlock : ChunkBlock {
        public static int Index = 149;

        public GermaniumChunkBlock() : base(
            Matrix.CreateRotationX(3f) * Matrix.CreateRotationZ(2f),
            Matrix.CreateTranslation(0.875f, 0.25f, 0f),
            new Color(255, 255, 255),
            false
        ) { }
    }
}