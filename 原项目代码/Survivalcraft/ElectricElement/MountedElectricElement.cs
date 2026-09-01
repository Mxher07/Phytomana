using Engine;

namespace Game {
    public abstract class MountedElectricElement : ElectricElement {
        public MountedElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) { }

        public override void OnNeighborBlockChanged(CellFace cellFace, int neighborX, int neighborY, int neighborZ) {
            Point3 point = CellFace.FaceToPoint3(cellFace.Face);
            int x = cellFace.X - point.X;
            int y = cellFace.Y - point.Y;
            int z = cellFace.Z - point.Z;
            if (SubsystemElectricity.SubsystemTerrain.Terrain.IsCellValid(x, y, z)) {
                int cellValue = SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(x, y, z);
                int elementCellValue = SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
                if (block.IsFaceNonAttachable(SubsystemElectricity.SubsystemTerrain, cellFace.Face, cellValue, elementCellValue)
                    && (cellFace.Face != 4 || !(block is FenceBlock))) {
                    SubsystemElectricity.SubsystemTerrain.DestroyCell(
                        0,
                        cellFace.X,
                        cellFace.Y,
                        cellFace.Z,
                        0,
                        false,
                        false
                    );
                }
            }
        }
    }
}