namespace Game {
    public class FramedGlassBlock : AlphaTestCubeBlock {
        public static int Index = 44;
        public bool IsCollapseSupportBlock_ = false;

        public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain,
            int face,
            int value,
            int neighborValue,
            int x,
            int y,
            int z) {
            if (Terrain.ExtractContents(neighborValue) == BlockIndex) {
                return false;
            }
            return base.ShouldGenerateFace(
                subsystemTerrain,
                face,
                value,
                neighborValue,
                x,
                y,
                z
            );
        }

        public override bool IsNonAttachable(int value) => false;

        public override bool IsCollapseSupportBlock(SubsystemTerrain subsystemTerrain, int value) => IsCollapseSupportBlock_;
    }
}