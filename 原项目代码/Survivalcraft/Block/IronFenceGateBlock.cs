using Engine;
namespace Game {
    public class IronFenceGateBlock : FenceGateBlock {
        public static int Index = 194;

        public IronFenceGateBlock() : base(
            "Models/IronFenceGate",
            0.0443f,
            true,
            true,
            58,
            new Color(192, 192, 192),
            new Color(80, 80, 80)
        ) { }
    }
}