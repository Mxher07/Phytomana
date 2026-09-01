using Engine;
namespace Game {
    public class BrickFenceBlock : FenceBlock {
        public static int Index = 164;

        public BrickFenceBlock() : base("Models/StoneFence", false, false, 39, new Color(212, 212, 212), Color.White) { }

        public override bool ShouldConnectTo(int value) {
            if (BlocksManager.Blocks[Terrain.ExtractContents(value)].IsNonAttachable(value)) {
                return base.ShouldConnectTo(value);
            }
            return true;
        }
    }
}