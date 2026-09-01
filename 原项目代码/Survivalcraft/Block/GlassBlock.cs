namespace Game {
    public class GlassBlock : AlphaTestCubeBlock {
        public static int Index = 15;

        public bool IsCollapseSupportBlock_ = false;

        public GlassBlock() => CanBeBuiltIntoFurniture = true;

        public override GeometrySubsetType GetFurnitureSubsetType(int value) => GeometrySubsetType.AlphaTest;

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