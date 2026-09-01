using Engine;

namespace Game {
    public class GunpowderBlock : ChunkBlock {
        public static int Index = 109;

        public GunpowderBlock() : base(
            Matrix.CreateScale(0.75f) * Matrix.CreateRotationX(4f) * Matrix.CreateRotationZ(3f),
            Matrix.CreateScale(1f) * Matrix.CreateTranslation(0.0625f, 0.875f, 0f),
            new Color(255, 255, 255),
            false
        ) { }
    }
}