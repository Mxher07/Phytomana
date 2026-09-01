using Engine;

namespace Game {
    public class CoalChunkBlock : ChunkBlock {
        public static int Index = 22;

        public CoalChunkBlock() : base(
            Matrix.CreateRotationX(1f) * Matrix.CreateRotationZ(2f),
            Matrix.CreateTranslation(0.875f, 0.1875f, 0f),
            new Color(255, 255, 255),
            false
        ) { }
    }
}