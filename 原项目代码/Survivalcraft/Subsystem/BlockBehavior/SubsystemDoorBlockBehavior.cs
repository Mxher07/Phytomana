using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemDoorBlockBehavior : SubsystemBlockBehavior {
        public SubsystemElectricity m_subsystemElectricity;

        public static Random m_random = new();

        public override int[] HandledBlocks => [56, 57, 58];

        public bool OpenCloseDoor(int x, int y, int z, bool open) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
            int num = Terrain.ExtractContents(cellValue);
            if (BlocksManager.Blocks[num] is DoorBlock) {
                int data = DoorBlock.SetOpen(Terrain.ExtractData(cellValue), open);
                int value = Terrain.ReplaceData(cellValue, data);
                SubsystemTerrain.ChangeCell(x, y, z, value);
                string name = open ? "Audio/Doors/DoorOpen" : "Audio/Doors/DoorClose";
                SubsystemTerrain.Project.FindSubsystem<SubsystemAudio>(true)
                    .PlaySound(name, 0.7f, m_random.Float(-0.1f, 0.1f), new Vector3(x, y, z), 4f, true);
                return true;
            }
            return false;
        }

        public bool IsDoorElectricallyConnected(int x, int y, int z) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
            int num = Terrain.ExtractContents(cellValue);
            int data = Terrain.ExtractData(cellValue);
            if (BlocksManager.Blocks[num] is DoorBlock) {
                int num2 = DoorBlock.IsBottomPart(SubsystemTerrain.Terrain, x, y, z) ? y : y - 1;
                for (int i = num2; i <= num2 + 1; i++) {
                    ElectricElement electricElement = m_subsystemElectricity.GetElectricElement(x, i, z, DoorBlock.GetHingeFace(data));
                    if (electricElement != null
                        && electricElement.Connections.Count > 0) {
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            CellFace cellFace = raycastResult.CellFace;
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
            int num = Terrain.ExtractContents(cellValue);
            int data = Terrain.ExtractData(cellValue);
            if (num == 56
                || !IsDoorElectricallyConnected(cellFace.X, cellFace.Y, cellFace.Z)) {
                bool open = DoorBlock.GetOpen(data);
                return OpenCloseDoor(cellFace.X, cellFace.Y, cellFace.Z, !open);
            }
            return true;
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z);
            int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
            if (!BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].IsNonAttachable(cellValue)
                && Terrain.ExtractContents(cellValue2) == 0) {
                SubsystemTerrain.ChangeCell(x, y + 1, z, value);
            }
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            if (DoorBlock.IsTopPart(SubsystemTerrain.Terrain, x, y, z)) {
                SubsystemTerrain.ChangeCell(x, y - 1, z, 0);
            }
            if (DoorBlock.IsBottomPart(SubsystemTerrain.Terrain, x, y, z)) {
                SubsystemTerrain.ChangeCell(x, y + 1, z, 0);
            }
        }

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
            int num = Terrain.ExtractContents(cellValue);
            Block obj = BlocksManager.Blocks[num];
            int data = Terrain.ExtractData(cellValue);
            if (!(obj is DoorBlock)) {
                return;
            }
            if (neighborX == x
                && neighborY == y
                && neighborZ == z) {
                if (DoorBlock.IsBottomPart(SubsystemTerrain.Terrain, x, y, z)) {
                    int value = Terrain.ReplaceData(SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z), data);
                    SubsystemTerrain.ChangeCell(x, y + 1, z, value);
                }
                if (DoorBlock.IsTopPart(SubsystemTerrain.Terrain, x, y, z)) {
                    int value2 = Terrain.ReplaceData(SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z), data);
                    SubsystemTerrain.ChangeCell(x, y - 1, z, value2);
                }
            }
            if (DoorBlock.IsBottomPart(SubsystemTerrain.Terrain, x, y, z)) {
                int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z);
                if (BlocksManager.Blocks[Terrain.ExtractContents(cellValue2)].IsNonAttachable(cellValue2)) {
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
            if (!DoorBlock.IsBottomPart(SubsystemTerrain.Terrain, x, y, z)
                && !DoorBlock.IsTopPart(SubsystemTerrain.Terrain, x, y, z)) {
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

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemElectricity = Project.FindSubsystem<SubsystemElectricity>(true);
        }
    }
}