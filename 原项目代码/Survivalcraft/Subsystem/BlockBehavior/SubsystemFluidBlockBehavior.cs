using Engine;
using TemplatesDatabase;

namespace Game {
    public abstract class SubsystemFluidBlockBehavior : SubsystemBlockBehavior {
        public static Point2[] m_sideNeighbors = [new(-1, 0), new(1, 0), new(0, -1), new(0, 1)];

        public FluidBlock m_fluidBlock;

        public Dictionary<Point3, bool> m_toUpdate = [];

        public Dictionary<Point3, int> m_toSet = [];

        public Dictionary<Point3, int> m_visited = [];

        public Dictionary<Point3, Vector2> m_fluidRandomFlowDirections = [];

        public bool m_generateSources;

        public SubsystemTime SubsystemTime { get; set; }

        public SubsystemAudio SubsystemAudio { get; set; }

        public SubsystemAmbientSounds SubsystemAmbientSounds { get; set; }

        public SubsystemFluidBlockBehavior(FluidBlock fluidBlock, bool generateSources) {
            m_fluidBlock = fluidBlock;
            m_generateSources = generateSources;
        }

        public virtual void UpdateIsTop(int value, int x, int y, int z) {
            Terrain terrain = SubsystemTerrain.Terrain;
            if (y < 255) {
                TerrainChunk chunkAtCell = terrain.GetChunkAtCell(x, z);
                if (chunkAtCell != null) {
                    int num = TerrainChunk.CalculateCellIndex(x & 0xF, y, z & 0xF);
                    int contents = Terrain.ExtractContents(chunkAtCell.GetCellValueFast(num + 1));
                    int data = Terrain.ExtractData(value);
                    bool isTop = !m_fluidBlock.IsTheSameFluid(contents);
                    chunkAtCell.SetCellValueFast(num, Terrain.ReplaceData(value, FluidBlock.SetIsTop(data, isTop)));
                }
            }
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            UpdateIsTop(value, x, y, z);
        }

        public override void OnBlockModified(int value, int oldValue, int x, int y, int z) {
            UpdateIsTop(value, x, y, z);
        }

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            m_toUpdate[new Point3 { X = x, Y = y, Z = z }] = true;
            if (neighborY == y + 1) {
                UpdateIsTop(SubsystemTerrain.Terrain.GetCellValueFast(x, y, z), x, y, z);
            }
        }

        public override void OnItemHarvested(int x, int y, int z, int blockValue, ref BlockDropValue dropValue, ref int newBlockValue) {
            newBlockValue = Terrain.MakeBlockValue(m_fluidBlock.BlockIndex);
            dropValue.Value = 0;
            dropValue.Count = 0;
        }

        public float? GetSurfaceHeight(int x, int y, int z, out FluidBlock surfaceFluidBlock) {
            if (y >= 0
                && y < 255) {
                TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(x, z);
                if (chunkAtCell != null) {
                    int num = TerrainChunk.CalculateCellIndex(x & 0xF, 0, z & 0xF);
                    while (y < 255) {
                        int num2 = Terrain.ExtractContents(chunkAtCell.GetCellValueFast(num + y + 1));
                        if (BlocksManager.FluidBlocks[num2] == null) {
                            int cellValueFast = chunkAtCell.GetCellValueFast(num + y);
                            int num3 = Terrain.ExtractContents(cellValueFast);
                            FluidBlock fluidBlock = BlocksManager.FluidBlocks[num3];
                            if (fluidBlock != null) {
                                surfaceFluidBlock = fluidBlock;
                                int level = FluidBlock.GetLevel(Terrain.ExtractData(cellValueFast));
                                return y + surfaceFluidBlock.GetLevelHeight(level);
                            }
                            surfaceFluidBlock = null;
                            return null;
                        }
                        y++;
                    }
                }
            }
            surfaceFluidBlock = null;
            return null;
        }

        public float? GetSurfaceHeight(int x, int y, int z) => GetSurfaceHeight(x, y, z, out FluidBlock _);

        public Vector2? CalculateFlowSpeed(int x, int y, int z, out FluidBlock surfaceBlock, out float? surfaceHeight) {
            surfaceHeight = GetSurfaceHeight(x, y, z, out surfaceBlock);
            if (surfaceHeight.HasValue) {
                y = (int)surfaceHeight.Value;
                int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
                int num = Terrain.ExtractContents(cellValue);
                if (BlocksManager.Blocks[num] is FluidBlock) {
                    int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x - 1, y, z);
                    int cellValue3 = SubsystemTerrain.Terrain.GetCellValue(x + 1, y, z);
                    int cellValue4 = SubsystemTerrain.Terrain.GetCellValue(x, y, z - 1);
                    int cellValue5 = SubsystemTerrain.Terrain.GetCellValue(x, y, z + 1);
                    int num2 = Terrain.ExtractContents(cellValue2);
                    int num3 = Terrain.ExtractContents(cellValue3);
                    int num4 = Terrain.ExtractContents(cellValue4);
                    int num5 = Terrain.ExtractContents(cellValue5);
                    int level = FluidBlock.GetLevel(Terrain.ExtractData(cellValue));
                    int num6 = num2 == num ? FluidBlock.GetLevel(Terrain.ExtractData(cellValue2)) : level;
                    int num7 = num3 == num ? FluidBlock.GetLevel(Terrain.ExtractData(cellValue3)) : level;
                    int num8 = num4 == num ? FluidBlock.GetLevel(Terrain.ExtractData(cellValue4)) : level;
                    int num9 = num5 == num ? FluidBlock.GetLevel(Terrain.ExtractData(cellValue5)) : level;
                    Vector2 vector = default;
                    vector.X = Math.Sign(level - num6) - Math.Sign(level - num7);
                    vector.Y = Math.Sign(level - num8) - Math.Sign(level - num9);
                    Vector2 v = vector;
                    if (v.LengthSquared() > 1f) {
                        v = Vector2.Normalize(v);
                    }
                    if (!m_fluidRandomFlowDirections.TryGetValue(new Point3(x, y, z), out Vector2 value)) {
                        value.X = 0.05f * (2f * SimplexNoise.OctavedNoise(x + 0.2f * (float)SubsystemTime.GameTime, z, 0.1f, 1, 1f, 1f) - 1f);
                        value.Y = 0.05f * (2f * SimplexNoise.OctavedNoise(x + 0.2f * (float)SubsystemTime.GameTime + 100f, z, 0.1f, 1, 1f, 1f) - 1f);
                        if (m_fluidRandomFlowDirections.Count < 1000) {
                            m_fluidRandomFlowDirections[new Point3(x, y, z)] = value;
                        }
                        else {
                            m_fluidRandomFlowDirections.Clear();
                        }
                    }
                    v += value;
                    return v * 2f;
                }
            }
            return null;
        }

        public Vector2? CalculateFlowSpeed(int x, int y, int z) => CalculateFlowSpeed(x, y, z, out FluidBlock _, out float? _);

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            SubsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            SubsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            SubsystemAmbientSounds = Project.FindSubsystem<SubsystemAmbientSounds>(true);
        }

        public void SpreadFluid() {
            for (int i = 0; i < 2; i++) {
                foreach (Point3 key in m_toUpdate.Keys) {
                    int x = key.X;
                    int y = key.Y;
                    int z = key.Z;
                    int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
                    int contents = Terrain.ExtractContents(cellValue);
                    int data = Terrain.ExtractData(cellValue);
                    int level = FluidBlock.GetLevel(data);
                    if (m_fluidBlock.IsTheSameFluid(contents)) {
                        int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z);
                        int contents2 = Terrain.ExtractContents(cellValue2);
                        int data2 = Terrain.ExtractData(cellValue2);
                        int level2 = FluidBlock.GetLevel(data2);
                        int num = m_fluidBlock.MaxLevel + 1;
                        int num2 = 0;
                        for (int j = 0; j < 4; j++) {
                            int cellValue3 = SubsystemTerrain.Terrain.GetCellValue(x + m_sideNeighbors[j].X, y, z + m_sideNeighbors[j].Y);
                            int contents3 = Terrain.ExtractContents(cellValue3);
                            if (m_fluidBlock.IsTheSameFluid(contents3)) {
                                int level3 = FluidBlock.GetLevel(Terrain.ExtractData(cellValue3));
                                num = MathUtils.Min(num, level3);
                                if (level3 == 0) {
                                    num2++;
                                }
                            }
                        }
                        if (level != 0
                            && level <= num) {
                            int contents4 = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z));
                            if (!m_fluidBlock.IsTheSameFluid(contents4)) {
                                Set(
                                    x,
                                    y,
                                    z,
                                    num + 1 > m_fluidBlock.MaxLevel ? 0 : Terrain.MakeBlockValue(contents, 0, FluidBlock.SetLevel(data, num + 1))
                                );
                                continue;
                            }
                        }
                        if (m_generateSources
                            && level != 0
                            && num2 >= 2) {
                            Set(x, y, z, Terrain.MakeBlockValue(contents, 0, FluidBlock.SetLevel(data, 0)));
                        }
                        else if (m_fluidBlock.IsTheSameFluid(contents2)) {
                            if (level2 > 1) {
                                Set(x, y - 1, z, Terrain.MakeBlockValue(contents2, 0, FluidBlock.SetLevel(data2, 1)));
                            }
                        }
                        else if (!OnFluidInteract(
                                cellValue2,
                                x,
                                y - 1,
                                z,
                                Terrain.MakeBlockValue(m_fluidBlock.BlockIndex, 0, FluidBlock.SetLevel(0, 1))
                            )
                            && level < m_fluidBlock.MaxLevel) {
                            m_visited.Clear();
                            int num3 = LevelAtNearestFall(x + 1, y, z, level + 1, m_visited);
                            int num4 = LevelAtNearestFall(x - 1, y, z, level + 1, m_visited);
                            int num5 = LevelAtNearestFall(x, y, z + 1, level + 1, m_visited);
                            int num6 = LevelAtNearestFall(x, y, z - 1, level + 1, m_visited);
                            int num7 = MathUtils.Min(num3, num4, num5, num6);
                            if (num3 == num7) {
                                FlowTo(x + 1, y, z, level + 1);
                                FlowTo(x, y, z - 1, m_fluidBlock.MaxLevel);
                                FlowTo(x, y, z + 1, m_fluidBlock.MaxLevel);
                            }
                            if (num4 == num7) {
                                FlowTo(x - 1, y, z, level + 1);
                                FlowTo(x, y, z - 1, m_fluidBlock.MaxLevel);
                                FlowTo(x, y, z + 1, m_fluidBlock.MaxLevel);
                            }
                            if (num5 == num7) {
                                FlowTo(x, y, z + 1, level + 1);
                                FlowTo(x - 1, y, z, m_fluidBlock.MaxLevel);
                                FlowTo(x + 1, y, z, m_fluidBlock.MaxLevel);
                            }
                            if (num6 == num7) {
                                FlowTo(x, y, z - 1, level + 1);
                                FlowTo(x - 1, y, z, m_fluidBlock.MaxLevel);
                                FlowTo(x + 1, y, z, m_fluidBlock.MaxLevel);
                            }
                        }
                    }
                }
                m_toUpdate.Clear();
                foreach (KeyValuePair<Point3, int> item in m_toSet) {
                    int x2 = item.Key.X;
                    int y2 = item.Key.Y;
                    int z2 = item.Key.Z;
                    int value = item.Value;
                    int contents5 = Terrain.ExtractContents(item.Value);
                    int cellContents = SubsystemTerrain.Terrain.GetCellContents(x2, y2, z2);
                    FluidBlock fluidBlock = BlocksManager.FluidBlocks[cellContents];
                    if (fluidBlock != null
                        && !fluidBlock.IsTheSameFluid(contents5)) {
                        SubsystemTerrain.DestroyCell(
                            0,
                            x2,
                            y2,
                            z2,
                            value,
                            false,
                            false
                        );
                    }
                    else {
                        SubsystemTerrain.ChangeCell(x2, y2, z2, value);
                    }
                }
                m_toSet.Clear();
                SubsystemTerrain.ProcessModifiedCells();
            }
        }

        public virtual bool OnFluidInteract(int interactValue, int x, int y, int z, int fluidValue) {
            if (!BlocksManager.Blocks[Terrain.ExtractContents(interactValue)].IsFluidBlocker_(interactValue)) {
                SubsystemTerrain.DestroyCell(
                    0,
                    x,
                    y,
                    z,
                    0,
                    false,
                    false
                );
                Set(x, y, z, fluidValue);
                return true;
            }
            return false;
        }

        public float? CalculateDistanceToFluid(Vector3 p, int radius, bool flowingFluidOnly) {
            float num = float.MaxValue;
            Terrain terrain = SubsystemTerrain.Terrain;
            int num2 = Terrain.ToCell(p.X) - radius;
            int num3 = Terrain.ToCell(p.X) + radius;
            int num4 = Math.Clamp(Terrain.ToCell(p.Y) - radius, 0, 254);
            int num5 = Math.Clamp(Terrain.ToCell(p.Y) + radius, 0, 254);
            int num6 = Terrain.ToCell(p.Z) - radius;
            int num7 = Terrain.ToCell(p.Z) + radius;
            for (int i = num6; i <= num7; i++) {
                for (int j = num2; j <= num3; j++) {
                    TerrainChunk chunkAtCell = terrain.GetChunkAtCell(j, i);
                    if (chunkAtCell == null) {
                        continue;
                    }
                    int k = TerrainChunk.CalculateCellIndex(j & 0xF, num4, i & 0xF);
                    for (int l = num4; l <= num5; l++, k++) {
                        int cellValueFast = chunkAtCell.GetCellValueFast(k);
                        int contents = Terrain.ExtractContents(cellValueFast);
                        if (!m_fluidBlock.IsTheSameFluid(contents)) {
                            continue;
                        }
                        if (flowingFluidOnly) {
                            if (FluidBlock.GetLevel(Terrain.ExtractData(cellValueFast)) == 0) {
                                continue;
                            }
                            int contents2 = Terrain.ExtractContents(chunkAtCell.GetCellValueFast(k + 1));
                            if (m_fluidBlock.IsTheSameFluid(contents2)) {
                                continue;
                            }
                        }
                        float num8 = p.X - (j + 0.5f);
                        float num9 = p.Y - (l + 1f);
                        float num10 = p.Z - (i + 0.5f);
                        float num11 = num8 * num8 + num9 * num9 + num10 * num10;
                        if (num11 < num) {
                            num = num11;
                        }
                    }
                }
            }
            if (num == float.MaxValue) {
                return null;
            }
            return MathF.Sqrt(num);
        }

        public void Set(int x, int y, int z, int value) {
            Point3 key = new(x, y, z);
            m_toSet.TryAdd(key, value);
        }

        public void FlowTo(int x, int y, int z, int level) {
            if (level > m_fluidBlock.MaxLevel) {
                return;
            }
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
            int contents = Terrain.ExtractContents(cellValue);
            int data = Terrain.ExtractData(cellValue);
            if (m_fluidBlock.IsTheSameFluid(contents)) {
                int level2 = FluidBlock.GetLevel(data);
                if (level < level2) {
                    Set(x, y, z, Terrain.MakeBlockValue(contents, 0, FluidBlock.SetLevel(data, level)));
                }
            }
            else {
                OnFluidInteract(cellValue, x, y, z, Terrain.MakeBlockValue(m_fluidBlock.BlockIndex, 0, FluidBlock.SetLevel(0, level)));
            }
        }

        public int LevelAtNearestFall(int x, int y, int z, int level, Dictionary<Point3, int> levels) {
            if (level > m_fluidBlock.MaxLevel) {
                return int.MaxValue;
            }
            if (!levels.TryGetValue(new Point3(x, y, z), out int value)) {
                value = int.MaxValue;
            }
            if (level >= value) {
                return int.MaxValue;
            }
            levels[new Point3(x, y, z)] = level;
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
            int num = Terrain.ExtractContents(cellValue);
            if (m_fluidBlock.IsTheSameFluid(num)) {
                if (FluidBlock.GetLevel(Terrain.ExtractData(cellValue)) < level) {
                    return int.MaxValue;
                }
            }
            else if (BlocksManager.Blocks[num].IsFluidBlocker_(cellValue)) {
                return int.MaxValue;
            }
            int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z);
            int num2 = Terrain.ExtractContents(cellValue2);
            Block block = BlocksManager.Blocks[num2];
            if (m_fluidBlock.IsTheSameFluid(num2)
                || !block.IsFluidBlocker_(cellValue2)) {
                return level;
            }
            int x2 = LevelAtNearestFall(x - 1, y, z, level + 1, levels);
            int x3 = LevelAtNearestFall(x + 1, y, z, level + 1, levels);
            int x4 = LevelAtNearestFall(x, y, z - 1, level + 1, levels);
            int x5 = LevelAtNearestFall(x, y, z + 1, level + 1, levels);
            return MathUtils.Min(x2, x3, x4, x5);
        }
    }
}