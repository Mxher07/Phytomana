using Engine;

namespace Game {
    public class SubsystemStairsBlockBehavior : SubsystemBlockBehavior {
        public int[] m_handledBlocks;

        public override int[] HandledBlocks => m_handledBlocks;

        public SubsystemStairsBlockBehavior() {
            List<int> list = new();
            list.AddRange(from b in BlocksManager.Blocks where b is StairsBlock select b.BlockIndex);
            m_handledBlocks = list.ToArray();
        }

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
            UpdateIsCorner(cellValue, x, y, z, true);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            UpdateIsCorner(value, x, y, z, false);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            UpdateIsCorner(value, x, y, z, true);
        }

        public virtual void UpdateIsCorner(int value, int x, int y, int z, bool updateModificationCounter) {
            int value2 = Terrain.ExtractContents(value);
            if (!HandledBlocks.Contains(value2)) {
                return;
            }
            int data = Terrain.ExtractData(value);
            if (StairsBlock.GetCornerType(data) != 0) {
                return;
            }
            int rotation = StairsBlock.GetRotation(data);
            bool isUpsideDown = StairsBlock.GetIsUpsideDown(data);
            Point3 point = StairsBlock.RotationToDirection(rotation);
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z);
            int num = Terrain.ExtractContents(cellValue);
            if (BlocksManager.Blocks[num] is StairsBlock) {
                int data2 = Terrain.ExtractData(cellValue);
                bool isUpsideDown2 = StairsBlock.GetIsUpsideDown(data2);
                StairsBlock.CornerType cornerType = StairsBlock.GetCornerType(data2);
                int num2 = -1;
                if (isUpsideDown2 == isUpsideDown) {
                    int rotation2 = StairsBlock.GetRotation(data2);
                    if (rotation == 0
                        && rotation2 == 1
                        && cornerType != StairsBlock.CornerType.ThreeQuarters) {
                        num2 = 1;
                    }
                    if (rotation == 0
                        && rotation2 == 3
                        && cornerType != StairsBlock.CornerType.ThreeQuarters) {
                        num2 = 0;
                    }
                    if (rotation == 1
                        && rotation2 == 0
                        && cornerType != StairsBlock.CornerType.ThreeQuarters) {
                        num2 = 1;
                    }
                    if (rotation == 1
                        && rotation2 == 2
                        && cornerType != StairsBlock.CornerType.ThreeQuarters) {
                        num2 = 2;
                    }
                    if (rotation == 2
                        && rotation2 == 1
                        && cornerType != StairsBlock.CornerType.ThreeQuarters) {
                        num2 = 2;
                    }
                    if (rotation == 2
                        && rotation2 == 3
                        && cornerType != StairsBlock.CornerType.ThreeQuarters) {
                        num2 = 3;
                    }
                    if (rotation == 3
                        && rotation2 == 0
                        && cornerType != StairsBlock.CornerType.ThreeQuarters) {
                        num2 = 0;
                    }
                    if (rotation == 3
                        && rotation2 == 2
                        && cornerType != StairsBlock.CornerType.ThreeQuarters) {
                        num2 = 3;
                    }
                }
                if (num2 >= 0) {
                    int data3 = StairsBlock.SetRotation(StairsBlock.SetCornerType(data, StairsBlock.CornerType.OneQuarter), num2);
                    int value3 = Terrain.ReplaceData(value, data3);
                    SubsystemTerrain.ChangeCell(x, y, z, value3, updateModificationCounter);
                }
                return;
            }
            cellValue = SubsystemTerrain.Terrain.GetCellValue(x - point.X, y - point.Y, z - point.Z);
            num = Terrain.ExtractContents(cellValue);
            if (!(BlocksManager.Blocks[num] is StairsBlock)) {
                return;
            }
            int data4 = Terrain.ExtractData(cellValue);
            bool isUpsideDown3 = StairsBlock.GetIsUpsideDown(data4);
            StairsBlock.CornerType cornerType2 = StairsBlock.GetCornerType(data4);
            int num3 = -1;
            if (isUpsideDown3 == isUpsideDown) {
                int rotation3 = StairsBlock.GetRotation(data4);
                if (rotation == 0
                    && rotation3 == 1
                    && cornerType2 == StairsBlock.CornerType.None) {
                    num3 = 1;
                }
                if (rotation == 0
                    && rotation3 == 3
                    && cornerType2 == StairsBlock.CornerType.None) {
                    num3 = 0;
                }
                if (rotation == 0
                    && rotation3 == 2
                    && cornerType2 == StairsBlock.CornerType.ThreeQuarters) {
                    num3 = 1;
                }
                if (rotation == 0
                    && rotation3 == 3
                    && cornerType2 == StairsBlock.CornerType.ThreeQuarters) {
                    num3 = 0;
                }
                if (rotation == 1
                    && rotation3 == 0
                    && cornerType2 == StairsBlock.CornerType.None) {
                    num3 = 1;
                }
                if (rotation == 1
                    && rotation3 == 2
                    && cornerType2 == StairsBlock.CornerType.None) {
                    num3 = 2;
                }
                if (rotation == 1
                    && rotation3 == 3
                    && cornerType2 == StairsBlock.CornerType.ThreeQuarters) {
                    num3 = 2;
                }
                if (rotation == 1
                    && rotation3 == 0
                    && cornerType2 == StairsBlock.CornerType.ThreeQuarters) {
                    num3 = 1;
                }
                if (rotation == 2
                    && rotation3 == 1
                    && cornerType2 == StairsBlock.CornerType.None) {
                    num3 = 2;
                }
                if (rotation == 2
                    && rotation3 == 3
                    && cornerType2 == StairsBlock.CornerType.None) {
                    num3 = 3;
                }
                if (rotation == 2
                    && rotation3 == 0
                    && cornerType2 == StairsBlock.CornerType.ThreeQuarters) {
                    num3 = 3;
                }
                if (rotation == 2
                    && rotation3 == 1
                    && cornerType2 == StairsBlock.CornerType.ThreeQuarters) {
                    num3 = 2;
                }
                if (rotation == 3
                    && rotation3 == 0
                    && cornerType2 == StairsBlock.CornerType.None) {
                    num3 = 0;
                }
                if (rotation == 3
                    && rotation3 == 2
                    && cornerType2 == StairsBlock.CornerType.None) {
                    num3 = 3;
                }
                if (rotation == 3
                    && rotation3 == 2
                    && cornerType2 == StairsBlock.CornerType.ThreeQuarters) {
                    num3 = 3;
                }
                if (rotation == 3
                    && rotation3 == 1
                    && cornerType2 == StairsBlock.CornerType.ThreeQuarters) {
                    num3 = 0;
                }
            }
            if (num3 >= 0) {
                int data5 = StairsBlock.SetRotation(StairsBlock.SetCornerType(data, StairsBlock.CornerType.ThreeQuarters), num3);
                int value4 = Terrain.ReplaceData(value, data5);
                SubsystemTerrain.ChangeCell(x, y, z, value4, updateModificationCounter);
            }
        }
    }
}