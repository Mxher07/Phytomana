using Engine;

namespace Game {
    public abstract class BottomSuckerBlock : WaterBlock {
        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            Point3 point = raycastResult.CellFace.Point + CellFace.FaceToPoint3(raycastResult.CellFace.Face);
            int cellValue = subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
            int num = Terrain.ExtractContents(cellValue);
            int data = Terrain.ExtractData(cellValue);
            Block obj = BlocksManager.Blocks[num];
            int face = Time.FrameIndex % 4;
            BlockPlacementData result;
            if (obj is WaterBlock) {
                result = default;
                result.CellFace = raycastResult.CellFace;
                result.Value = Terrain.MakeBlockValue(BlockIndex, 0, SetSubvariant(SetFace(data, raycastResult.CellFace.Face), face));
                return result;
            }
            result = default;
            return result;
        }

        public static int GetFace(int data) => (data >> 8) & 7;

        public static int SetFace(int data, int face) => (data & -1793) | ((face & 7) << 8);

        public static int GetSubvariant(int data) => (data >> 11) & 3;

        public static int SetSubvariant(int data, int face) => (data & -6145) | ((face & 3) << 11);

        public override bool IsMovableByPiston(int value, int pistonFace, int y, out bool isEnd) {
            isEnd = false;
            return false;
        }
    }
}