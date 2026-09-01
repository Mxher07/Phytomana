namespace Game {
    public class SubsystemSnowBlockBehavior : SubsystemBlockBehavior {
        public override int[] HandledBlocks => [61];

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            if (!CanSupportSnow(SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z))) {
                SubsystemTerrain.DestroyCell(
                    0,
                    x,
                    y,
                    z,
                    0,
                    false,
                    false
                );
            }
        }

        public static bool CanSupportSnow(int value) {
            int num = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[num];
            if (block.IsNonAttachable(value)) {
                return block is LeavesBlock;
            }
            return true;
        }

        public static bool CanBeReplacedBySnow(int value) {
            int num = Terrain.ExtractContents(value);
            return BlocksManager.Blocks[num] is FallenLeavesBlock;
        }
    }
}