using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemRotBlockBehavior : SubsystemPollableBlockBehavior {
        public const int MaxRot = 1;

        public SubsystemItemsScanner m_subsystemItemsScanner;

        public SubsystemGameInfo m_subsystemGameInfo;

        public double m_lastRotTime;

        public int m_rotStep;

        public const float m_rotPeriod = 60f;

        public bool m_isRotEnabled;

        public override int[] HandledBlocks => [];

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemItemsScanner = Project.FindSubsystem<SubsystemItemsScanner>(true);
            m_lastRotTime = valuesDictionary.GetValue<double>("LastRotTime");
            m_rotStep = valuesDictionary.GetValue<int>("RotStep");
            m_subsystemItemsScanner.ItemsScanned += ItemsScanned;
            m_isRotEnabled = m_subsystemGameInfo.WorldSettings.GameMode != 0 && m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Adventure;
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            base.Save(valuesDictionary);
            valuesDictionary.SetValue("LastRotTime", m_lastRotTime);
            valuesDictionary.SetValue("RotStep", m_rotStep);
        }

        public override void OnPoll(int value, int x, int y, int z, int pollPass) {
            if (m_isRotEnabled) {
                int num = Terrain.ExtractContents(value);
                Block block = BlocksManager.Blocks[num];
                int rotPeriod = block.GetRotPeriod(value);
                if (rotPeriod > 0
                    && pollPass % rotPeriod == 0) {
                    int num2 = block.GetDamage(value) + 1;
                    value = num2 > 1 ? block.GetDamageDestructionValue(value) : block.SetDamage(value, num2);
                    SubsystemTerrain.ChangeCell(x, y, z, value);
                }
            }
        }

        public void ItemsScanned(ReadOnlyList<ScannedItemData> items) {
            int num = (int)((m_subsystemGameInfo.TotalElapsedGameTime - m_lastRotTime) / 60.0);
            if (num > 0) {
                if (m_isRotEnabled) {
                    foreach (ScannedItemData item in items) {
                        int num2 = Terrain.ExtractContents(item.Value);
                        Block block = BlocksManager.Blocks[num2];
                        int rotPeriod = block.GetRotPeriod(item.Value);
                        if (rotPeriod > 0) {
                            int num3 = block.GetDamage(item.Value);
                            for (int i = 0; i < num; i++) {
                                if (num3 > 1) {
                                    break;
                                }
                                if ((i + m_rotStep) % rotPeriod == 0) {
                                    num3++;
                                }
                            }
                            if (num3 <= 1) {
                                m_subsystemItemsScanner.TryModifyItem(item, block.SetDamage(item.Value, num3));
                            }
                            else {
                                m_subsystemItemsScanner.TryModifyItem(item, block.GetDamageDestructionValue(item.Value));
                            }
                        }
                    }
                }
                m_rotStep += num;
                m_lastRotTime += num * 60f;
            }
        }
    }
}