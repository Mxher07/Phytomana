using Engine;
namespace Game {
    public class IronFenceBlock : FenceBlock {
        public static int Index = 193;

        public IronFenceBlock() : base("Models/IronFence", true, true, 58, new Color(192, 192, 192), new Color(80, 80, 80)) { }
    }
}