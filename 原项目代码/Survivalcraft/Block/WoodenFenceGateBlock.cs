using Engine;
namespace Game {
    public class WoodenFenceGateBlock : FenceGateBlock {
        public static int Index = 166;

        public WoodenFenceGateBlock() : base(
            "Models/WoodenFenceGate",
            0.0625f,
            false,
            false,
            23,
            Color.White,
            Color.White
        ) { }
    }
}