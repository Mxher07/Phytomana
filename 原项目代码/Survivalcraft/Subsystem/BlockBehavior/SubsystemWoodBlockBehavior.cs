using Engine;
using Engine.Serialization;
using TemplatesDatabase;

namespace Game {
    public class SubsystemWoodBlockBehavior : SubsystemBlockBehavior, IUpdateable {
        public const int m_radius = 3;

        public const int m_maxLeavesToCheck = 5000;

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemTime m_subsystemTime;

        public HashSet<Point3> m_leavesToCheck = [];

        public override int[] HandledBlocks => [];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != 0
                || m_leavesToCheck.Count >= 5000
                || !(BlocksManager.Blocks[Terrain.ExtractContents(value)] is WoodBlock)) {
                return;
            }
            int num = x - 3;
            int num2 = MathUtils.Max(y - 3, 0);
            int num3 = z - 3;
            int num4 = x + 3;
            int num5 = MathUtils.Min(y + 3, 255);
            int num6 = z + 3;
            for (int i = num; i <= num4; i++) {
                for (int j = num3; j <= num6; j++) {
                    TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(i, j);
                    if (chunkAtCell == null) {
                        continue;
                    }
                    int num7 = TerrainChunk.CalculateCellIndex(i & 0xF, 0, j & 0xF);
                    for (int k = num2; k <= num5; k++) {
                        int num8 = Terrain.ExtractContents(chunkAtCell.GetCellValueFast(num7 + k));
                        if (num8 != 0
                            && BlocksManager.Blocks[num8] is LeavesBlock) {
                            m_leavesToCheck.Add(new Point3(i, k, j));
                        }
                    }
                }
            }
        }

        public override void OnChunkDiscarding(TerrainChunk chunk) {
            int num = chunk.Origin.X - 16;
            int num2 = chunk.Origin.Y - 16;
            int num3 = chunk.Origin.X + 32;
            int num4 = chunk.Origin.Y + 32;
            List<Point3> list = new();
            foreach (Point3 item in m_leavesToCheck) {
                if (item.X >= num
                    && item.X < num3
                    && item.Z >= num2
                    && item.Z < num4) {
                    list.Add(item);
                }
            }
            foreach (Point3 item2 in list) {
                DecayLeavesIfNeeded(item2);
            }
        }

        public virtual void Update(float dt) {
            if (m_leavesToCheck.Count <= 0
                || !m_subsystemTime.PeriodicGameTimeEvent(20.0, 0.0)) {
                return;
            }
            int num = MathUtils.Min(MathUtils.Max((int)(m_leavesToCheck.Count * 0.1f), 10), 200);
            for (int i = 0; i < num; i++) {
                if (m_leavesToCheck.Count <= 0) {
                    break;
                }
                DecayLeavesIfNeeded(m_leavesToCheck.First());
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            string value = valuesDictionary.GetValue<string>("LeavesToCheck");
            Point3[] array = HumanReadableConverter.ValuesListFromString<Point3>(';', value);
            foreach (Point3 item in array) {
                m_leavesToCheck.Add(item);
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            base.Save(valuesDictionary);
            string value = HumanReadableConverter.ValuesListToString(';', m_leavesToCheck.ToArray());
            valuesDictionary.SetValue("LeavesToCheck", value);
        }

        public void DecayLeavesIfNeeded(Point3 p) {
            m_leavesToCheck.Remove(p);
            if (!(BlocksManager.Blocks[SubsystemTerrain.Terrain.GetCellContents(p.X, p.Y, p.Z)] is LeavesBlock)) {
                return;
            }
            bool flag = false;
            int num = p.X - 3;
            int num2 = MathUtils.Max(p.Y - 3, 0);
            int num3 = p.Z - 3;
            int num4 = p.X + 3;
            int num5 = MathUtils.Min(p.Y + 3, 255);
            int num6 = p.Z + 3;
            for (int i = num; i <= num4; i++) {
                for (int j = num3; j <= num6; j++) {
                    TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(i, j);
                    if (chunkAtCell == null) {
                        continue;
                    }
                    int num7 = TerrainChunk.CalculateCellIndex(i & 0xF, 0, j & 0xF);
                    int num8 = num2;
                    while (num8 <= num5) {
                        int num9 = Terrain.ExtractContents(chunkAtCell.GetCellValueFast(num7 + num8));
                        if (num9 == 0
                            || !(BlocksManager.Blocks[num9] is WoodBlock)) {
                            num8++;
                            continue;
                        }
                        goto IL_00e8;
                    }
                }
                continue;
                IL_00e8:
                flag = true;
                break;
            }
            if (!flag) {
                SubsystemTerrain.ChangeCell(p.X, p.Y, p.Z, 0);
            }
        }
    }
}