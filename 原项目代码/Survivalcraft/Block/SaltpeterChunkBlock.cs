using Engine;

namespace Game {
    public class SaltpeterChunkBlock : ChunkBlock {
        public static int Index = 102;

        public SaltpeterChunkBlock() : base(
            Matrix.CreateRotationX(1f) * Matrix.CreateRotationZ(0f),
            Matrix.CreateTranslation(0.0625f, 0.4375f, 0f),
            new Color(255, 255, 255),
            false
        ) { }
    }
}