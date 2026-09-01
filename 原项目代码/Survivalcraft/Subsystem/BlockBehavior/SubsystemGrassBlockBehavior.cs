using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemGrassBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable {
        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemTime m_subsystemTime;

        public Dictionary<Point3, int> m_toUpdate = [];

        public Random m_random = new();

        public override int[] HandledBlocks => [8];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void OnPoll(int value, int x, int y, int z, int pollPass) {
            if (Terrain.ExtractData(value) != 0
                || m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != 0) {
                return;
            }
            int num = Terrain.ExtractLight(SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z));
            if (num == 0) {
                m_toUpdate[new Point3(x, y, z)] = Terrain.ReplaceContents(value, 8);
            }
            if (num < 13) {
                return;
            }
            for (int i = x - 1; i <= x + 1; i++) {
                for (int j = z - 1; j <= z + 1; j++) {
                    for (int k = y - 2; k <= y + 1; k++) {
                        int cellValue = SubsystemTerrain.Terrain.GetCellValue(i, k, j);
                        if (Terrain.ExtractContents(cellValue) != 2) {
                            continue;
                        }
                        int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(i, k + 1, j);
                        if (KillsGrassIfOnTopOfIt(cellValue2)
                            || Terrain.ExtractLight(cellValue2) < 13
                            || !(m_random.Float(0f, 1f) < 0.1f)) {
                            continue;
                        }
                        int num2 = Terrain.ReplaceContents(cellValue, 8);
                        m_toUpdate[new Point3(i, k, j)] = num2;
                        if (Terrain.ExtractContents(cellValue2) == 0) {
                            int temperature = SubsystemTerrain.Terrain.GetTemperature(i, j);
                            int humidity = SubsystemTerrain.Terrain.GetHumidity(i, j);
                            int num3 = PlantsManager.GenerateRandomPlantValue(m_random, num2, temperature, humidity, k + 1);
                            if (num3 != 0) {
                                m_toUpdate[new Point3(i, k + 1, j)] = num3;
                            }
                        }
                    }
                }
            }
        }

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
            if (Terrain.ExtractContents(cellValue) == 61) {
                int cellValueFast = SubsystemTerrain.Terrain.GetCellValueFast(x, y, z);
                cellValueFast = Terrain.ReplaceData(cellValueFast, 1);
                SubsystemTerrain.ChangeCell(x, y, z, cellValueFast);
            }
            else {
                int cellValueFast2 = SubsystemTerrain.Terrain.GetCellValueFast(x, y, z);
                cellValueFast2 = Terrain.ReplaceData(cellValueFast2, 0);
                SubsystemTerrain.ChangeCell(x, y, z, cellValueFast2);
            }
            if (KillsGrassIfOnTopOfIt(cellValue)) {
                SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(2, 0, 0));
            }
        }

        public override void OnExplosion(int value, int x, int y, int z, float damage) {
            if (damage > BlocksManager.Blocks[8].ExplosionResilience * m_random.Float(0f, 1f)) {
                SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(2, 0, 0));
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            base.Load(valuesDictionary);
        }

        public virtual void Update(float dt) {
            if (m_subsystemTime.PeriodicGameTimeEvent(60.0, 0.0)) {
                foreach (KeyValuePair<Point3, int> item in m_toUpdate) {
                    if (Terrain.ExtractContents(item.Value) == 8) {
                        if (SubsystemTerrain.Terrain.GetCellContents(item.Key.X, item.Key.Y, item.Key.Z) != 2) {
                            continue;
                        }
                    }
                    else {
                        int cellContents = SubsystemTerrain.Terrain.GetCellContents(item.Key.X, item.Key.Y - 1, item.Key.Z);
                        if ((cellContents != 8 && cellContents != 2)
                            || SubsystemTerrain.Terrain.GetCellContents(item.Key.X, item.Key.Y, item.Key.Z) != 0) {
                            continue;
                        }
                    }
                    SubsystemTerrain.ChangeCell(item.Key.X, item.Key.Y, item.Key.Z, item.Value);
                }
                m_toUpdate.Clear();
            }
        }

        public bool KillsGrassIfOnTopOfIt(int value) {
            int num = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[num];
            if (!(block is FluidBlock)) {
                if (!block.IsFaceTransparent(SubsystemTerrain, 5, value)) {
                    return block.IsCollidable_(value);
                }
                return false;
            }
            return true;
        }
    }
}