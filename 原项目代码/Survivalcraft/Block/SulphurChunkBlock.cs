using Engine;

namespace Game {
    public class SulphurChunkBlock : ChunkBlock {
        public static int Index = 103;

        public SulphurChunkBlock() : base(
            Matrix.CreateRotationX(2f) * Matrix.CreateRotationZ(1f),
            Matrix.CreateTranslation(0.0625f, 0.4375f, 0f),
            new Color(255, 255, 140),
            true
        ) { }
    }
}