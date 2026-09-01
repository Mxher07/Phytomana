using Engine;
namespace Game {
    public class StoneFenceBlock : FenceBlock {
        public static int Index = 202;

        public StoneFenceBlock() : base("Models/StoneFence", false, false, 24, new Color(212, 212, 212), Color.White) { }

        public override bool ShouldConnectTo(int value) {
            if (BlocksManager.Blocks[Terrain.ExtractContents(value)].IsNonAttachable(value)) {
                return base.ShouldConnectTo(value);
            }
            return true;
        }
    }
}