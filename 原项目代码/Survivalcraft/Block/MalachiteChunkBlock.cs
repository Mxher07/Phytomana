using Engine;

namespace Game {
    public class MalachiteChunkBlock : ChunkBlock {
        public static int Index = 43;

        public MalachiteChunkBlock() : base(
            Matrix.CreateRotationX(2f) * Matrix.CreateRotationZ(3f),
            Matrix.CreateTranslation(0.1875f, 0.6875f, 0f),
            new Color(255, 255, 255),
            false
        ) { }
    }
}