namespace Game {
    public class SubsystemFenceBlockBehavior : SubsystemBlockBehavior {
        public override int[] HandledBlocks => [];

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
            UpdateVariant(cellValue, x, y, z);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            UpdateVariant(value, x, y, z);
        }

        public virtual void UpdateVariant(int value, int x, int y, int z) {
            int num = Terrain.ExtractContents(value);
            if (BlocksManager.Blocks[num] is FenceBlock fenceBlock) {
                int cellValue = SubsystemTerrain.Terrain.GetCellValue(x + 1, y, z);
                int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x - 1, y, z);
                int cellValue3 = SubsystemTerrain.Terrain.GetCellValue(x, y, z + 1);
                int cellValue4 = SubsystemTerrain.Terrain.GetCellValue(x, y, z - 1);
                int num2 = 0;
                if (fenceBlock.ShouldConnectTo(cellValue)) {
                    num2++;
                }
                if (fenceBlock.ShouldConnectTo(cellValue2)) {
                    num2 += 2;
                }
                if (fenceBlock.ShouldConnectTo(cellValue3)) {
                    num2 += 4;
                }
                if (fenceBlock.ShouldConnectTo(cellValue4)) {
                    num2 += 8;
                }
                int data = Terrain.ExtractData(value);
                int value2 = Terrain.ReplaceData(value, FenceBlock.SetVariant(data, num2));
                SubsystemTerrain.ChangeCell(x, y, z, value2);
            }
        }
    }
}