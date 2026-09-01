using Engine;

namespace Game {
    public class StoneChunkBlock : ChunkBlock {
        public static int Index = 79;

        public StoneChunkBlock() : base(
            Matrix.CreateScale(0.75f) * Matrix.CreateRotationX(0f) * Matrix.CreateRotationZ(1f),
            Matrix.CreateScale(0.75f) * Matrix.CreateTranslation(0.1875f, 0.0625f, 0f),
            new Color(255, 255, 255),
            true
        ) { }
    }
}