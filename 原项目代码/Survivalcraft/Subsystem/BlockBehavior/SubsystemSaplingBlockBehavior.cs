using System.Globalization;
using System.Text;
using Engine;
using Engine.Serialization;
using TemplatesDatabase;

namespace Game {
    public class SubsystemSaplingBlockBehavior : SubsystemBlockBehavior, IUpdateable {
        public class SaplingData {
            public Point3 Point;

            public TreeType Type;

            public double MatureTime;
        }

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemTimeOfDay m_subsystemTimeOfDay;

        public Dictionary<Point3, SaplingData> m_saplings = [];

        public Dictionary<Point3, SaplingData>.ValueCollection.Enumerator m_enumerator;

        public Random m_random = new();

        public StringBuilder m_stringBuilder = new();

        public override int[] HandledBlocks => [119];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

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

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            float num = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative ? m_random.Float(8f, 12f) : m_random.Float(480f, 600f);
            AddSapling(
                new SaplingData {
                    Point = new Point3(x, y, z),
                    Type = (TreeType)Terrain.ExtractData(value),
                    MatureTime = m_subsystemGameInfo.TotalElapsedGameTime + num
                }
            );
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            RemoveSapling(new Point3(x, y, z));
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(true);
            m_enumerator = m_saplings.Values.GetEnumerator();
            foreach (string value in valuesDictionary.GetValue<ValuesDictionary>("Saplings").Values) {
                AddSapling(LoadSaplingData(value));
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            ValuesDictionary valuesDictionary2 = new();
            valuesDictionary.SetValue("Saplings", valuesDictionary2);
            int num = 0;
            foreach (SaplingData value in m_saplings.Values) {
                valuesDictionary2.SetValue(num++.ToString(CultureInfo.InvariantCulture), SaveSaplingData(value));
            }
        }

        public virtual void Update(float dt) {
            int num = 0;
            while (true) {
                if (num < 10) {
                    if (!m_enumerator.MoveNext()) {
                        break;
                    }
                    MatureSapling(m_enumerator.Current);
                    num++;
                    continue;
                }
                return;
            }
            m_enumerator = m_saplings.Values.GetEnumerator();
        }

        public SaplingData LoadSaplingData(string data) {
            string[] array = data.Split([";"], StringSplitOptions.None);
            if (array.Length != 3) {
                throw new InvalidOperationException("Invalid sapling data string.");
            }
            return new SaplingData {
                Point = HumanReadableConverter.ConvertFromString<Point3>(array[0]),
                Type = HumanReadableConverter.ConvertFromString<TreeType>(array[1]),
                MatureTime = HumanReadableConverter.ConvertFromString<double>(array[2])
            };
        }

        public string SaveSaplingData(SaplingData saplingData) {
            m_stringBuilder.Length = 0;
            m_stringBuilder.Append(HumanReadableConverter.ConvertToString(saplingData.Point));
            m_stringBuilder.Append(';');
            m_stringBuilder.Append(HumanReadableConverter.ConvertToString(saplingData.Type));
            m_stringBuilder.Append(';');
            m_stringBuilder.Append(HumanReadableConverter.ConvertToString(saplingData.MatureTime));
            return m_stringBuilder.ToString();
        }

        public void MatureSapling(SaplingData saplingData) {
            if (!(m_subsystemGameInfo.TotalElapsedGameTime >= saplingData.MatureTime)) {
                return;
            }
            int x = saplingData.Point.X;
            int y = saplingData.Point.Y;
            int z = saplingData.Point.Z;
            TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(x - 6, z - 6);
            TerrainChunk chunkAtCell2 = SubsystemTerrain.Terrain.GetChunkAtCell(x - 6, z + 6);
            TerrainChunk chunkAtCell3 = SubsystemTerrain.Terrain.GetChunkAtCell(x + 6, z - 6);
            TerrainChunk chunkAtCell4 = SubsystemTerrain.Terrain.GetChunkAtCell(x + 6, z + 6);
            if (chunkAtCell != null
                && chunkAtCell.State == TerrainChunkState.Valid
                && chunkAtCell2 != null
                && chunkAtCell2.State == TerrainChunkState.Valid
                && chunkAtCell3 != null
                && chunkAtCell3.State == TerrainChunkState.Valid
                && chunkAtCell4 != null
                && chunkAtCell4.State == TerrainChunkState.Valid) {
                int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z);
                int saplingValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
                int cellContents = Terrain.ExtractContents(cellValue);
                Block cellBlock = BlocksManager.Blocks[cellContents];
                if (cellBlock.IsSuitableForPlants(cellValue, saplingValue)) {
                    if (SubsystemTerrain.Terrain.GetCellLight(x, y + 1, z) >= 9) {
                        bool flag = false;
                        for (int i = x - 1; i <= x + 1; i++) {
                            for (int j = z - 1; j <= z + 1; j++) {
                                int cellContents2 = SubsystemTerrain.Terrain.GetCellContents(i, y - 1, j);
                                if (BlocksManager.Blocks[cellContents2] is WaterBlock) {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        float num;
                        if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) {
                            num = 1f;
                        }
                        else {
                            int num2 = SubsystemTerrain.Terrain.GetTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
                            int num3 = SubsystemTerrain.Terrain.GetHumidity(x, z);
                            if (flag) {
                                num2 = (num2 + 10) / 2;
                                num3 = MathUtils.Max(num3, 12);
                            }
                            num = 2f * PlantsManager.CalculateTreeProbability(saplingData.Type, num2, num3, y);
                        }
                        if (m_random.Bool(num)) {
                            SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(0, 0, 0));
                            if (!GrowTree(x, y, z, saplingData.Type)) {
                                SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(28, 0, 0));
                            }
                        }
                        else {
                            SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(28, 0, 0));
                        }
                    }
                    else if (m_subsystemGameInfo.TotalElapsedGameTime > saplingData.MatureTime + m_subsystemTimeOfDay.DayDuration) {
                        SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(28, 0, 0));
                    }
                }
                else {
                    SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(28, 0, 0));
                }
            }
            else {
                saplingData.MatureTime = m_subsystemGameInfo.TotalElapsedGameTime;
            }
        }

        public bool GrowTree(int x, int y, int z, TreeType treeType) {
            ReadOnlyList<TerrainBrush> treeBrushes = PlantsManager.GetTreeBrushes(treeType);
            for (int i = 0; i < 20; i++) {
                TerrainBrush terrainBrush = treeBrushes[m_random.Int(0, treeBrushes.Count - 1)];
                bool flag = true;
                TerrainBrush.Cell[] cells = terrainBrush.Cells;
                for (int j = 0; j < cells.Length; j++) {
                    TerrainBrush.Cell cell = cells[j];
                    if (cell.Y >= 0
                        && (cell.X != 0 || cell.Y != 0 || cell.Z != 0)) {
                        int cellContents = SubsystemTerrain.Terrain.GetCellContents(cell.X + x, cell.Y + y, cell.Z + z);
                        if (cellContents != 0
                            && !(BlocksManager.Blocks[cellContents] is LeavesBlock)) {
                            flag = false;
                            break;
                        }
                    }
                }
                if (flag) {
                    terrainBrush.Paint(SubsystemTerrain, x, y, z);
                    return true;
                }
            }
            return false;
        }

        public void AddSapling(SaplingData saplingData) {
            m_saplings[saplingData.Point] = saplingData;
            m_enumerator = m_saplings.Values.GetEnumerator();
        }

        public void RemoveSapling(Point3 point) {
            m_saplings.Remove(point);
            m_enumerator = m_saplings.Values.GetEnumerator();
        }
    }
}