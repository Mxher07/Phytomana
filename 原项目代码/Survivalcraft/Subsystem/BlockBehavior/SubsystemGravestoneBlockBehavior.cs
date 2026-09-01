namespace Game {
    public class SubsystemGravestoneBlockBehavior : SubsystemBlockBehavior {
        public override int[] HandledBlocks => [189];

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z);
            if (BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].IsNonAttachable(cellValue)) {
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
    }
}