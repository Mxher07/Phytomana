using Engine;

namespace Game {
    public class SubsystemLadderBlockBehavior : SubsystemBlockBehavior {
        public override int[] HandledBlocks => [59, 213];

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int ladderBlockValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
            int face = LadderBlock.GetFace(Terrain.ExtractData(ladderBlockValue));
            Point3 point = CellFace.FaceToPoint3(face);
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x - point.X, y - point.Y, z - point.Z);
            int num = Terrain.ExtractContents(cellValue);
            if (BlocksManager.Blocks[num].IsFaceNonAttachable(SubsystemTerrain, face, cellValue, ladderBlockValue)) {
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