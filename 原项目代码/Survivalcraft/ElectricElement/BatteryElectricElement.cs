using Engine;

namespace Game {
    public class BatteryElectricElement : ElectricElement {
        public BatteryElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) { }

        public override float GetOutputVoltage(int face) {
            Point3 point = CellFaces[0].Point;
            return BatteryBlock.GetVoltageLevel(
                    Terrain.ExtractData(SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z))
                )
                / 15f;
        }

        public override void OnNeighborBlockChanged(CellFace cellFace, int neighborX, int neighborY, int neighborZ) {
            int cellValue = SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y - 1, cellFace.Z);
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
            if (!block.IsCollidable_(cellValue)
                || block.IsNonAttachable(cellValue)) {
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