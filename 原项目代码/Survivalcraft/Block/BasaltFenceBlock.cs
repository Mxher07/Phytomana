using Engine;
namespace Game {
    public class BasaltFenceBlock : FenceBlock {
        public static int Index = 163;

        public BasaltFenceBlock() : base("Models/StoneFence", false, false, 40, new Color(212, 212, 212), Color.White) { }

        public override bool ShouldConnectTo(int value) {
            if (BlocksManager.Blocks[Terrain.ExtractContents(value)].IsNonAttachable(value)) {
                return base.ShouldConnectTo(value);
            }
            return true;
        }
    }
}