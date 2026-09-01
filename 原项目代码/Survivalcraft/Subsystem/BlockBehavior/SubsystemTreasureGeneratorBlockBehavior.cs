using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemTreasureGeneratorBlockBehavior : SubsystemBlockBehavior {
        public struct TreasureData {
            public TreasureData() { }
            public int Value;

            public float Probability;

            public int MaxCount;

            /// <summary>
            ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
            /// </summary>
            public ValuesDictionary ValuesDictionaryForMods = new();
        }

        public SubsystemPickables m_subsystemPickables;

        public Random m_random = new();

        public static List<TreasureData> m_treasureData;

        public override int[] HandledBlocks => [190];

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int cellContents = SubsystemTerrain.Terrain.GetCellContents(neighborX, neighborY, neighborZ);
            if (cellContents != 0
                && cellContents != 18) {
                return;
            }
            SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(0));
            if (!m_random.Bool(0.25f)) {
                return;
            }
            int num = 0;
            int num2 = 0;
            float max = m_treasureData.Sum(t => t.Probability);
            float num3 = m_random.Float(0f, max);
            for (int i = 0; i < m_treasureData.Count; i++) {
                TreasureData treasureData2 = m_treasureData[i];
                num3 -= treasureData2.Probability;
                if (num3 <= 0f) {
                    num = treasureData2.Value;
                    num2 = m_random.Int(1, treasureData2.MaxCount);
                    break;
                }
            }
            ModsManager.HookAction(
                "OnTreasureGenerate",
                modLoader => {
                    modLoader.OnTreasureGenerate(
                        SubsystemTerrain,
                        x,
                        y,
                        z,
                        neighborX,
                        neighborY,
                        neighborZ,
                        ref num,
                        ref num2,
                        out bool IsGenerate
                    );
                    return IsGenerate;
                }
            );
            if (num != 0
                && num2 > 0) {
                for (int j = 0; j < num2; j++) {
                    m_subsystemPickables.AddPickable(
                        num,
                        1,
                        new Vector3(x, y, z) + m_random.Vector3(0.1f, 0.4f) + new Vector3(0.5f),
                        Vector3.Zero,
                        null
                    );
                }
                int num4 = m_random.Int(3, 6);
                for (int k = 0; k < num4; k++) {
                    m_subsystemPickables.AddPickable(
                        248,
                        1,
                        new Vector3(x, y, z) + m_random.Vector3(0.1f, 0.4f) + new Vector3(0.5f),
                        Vector3.Zero,
                        null
                    );
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
        }

        static SubsystemTreasureGeneratorBlockBehavior() {
            TreasureData[] array = new TreasureData[61];
            TreasureData treasureData = new() { Value = 79, Probability = 4f, MaxCount = 4 };
            array[0] = treasureData;
            treasureData = new TreasureData { Value = 111, Probability = 1f, MaxCount = 1 };
            array[1] = treasureData;
            treasureData = new TreasureData { Value = 43, Probability = 4f, MaxCount = 4 };
            array[2] = treasureData;
            treasureData = new TreasureData { Value = 40, Probability = 2f, MaxCount = 3 };
            array[3] = treasureData;
            treasureData = new TreasureData { Value = 42, Probability = 4f, MaxCount = 3 };
            array[4] = treasureData;
            treasureData = new TreasureData { Value = 22, Probability = 4f, MaxCount = 4 };
            array[5] = treasureData;
            treasureData = new TreasureData { Value = 103, Probability = 2f, MaxCount = 4 };
            array[6] = treasureData;
            treasureData = new TreasureData { Value = 150, Probability = 1f, MaxCount = 1 };
            array[7] = treasureData;
            treasureData = new TreasureData { Value = 21, Probability = 2f, MaxCount = 16 };
            array[8] = treasureData;
            treasureData = new TreasureData { Value = 159, Probability = 2f, MaxCount = 4 };
            array[9] = treasureData;
            treasureData = new TreasureData { Value = 207, Probability = 2f, MaxCount = 4 };
            array[10] = treasureData;
            treasureData = new TreasureData { Value = 17, Probability = 2f, MaxCount = 2 };
            array[11] = treasureData;
            treasureData = new TreasureData { Value = 31, Probability = 4f, MaxCount = 4 };
            array[12] = treasureData;
            treasureData = new TreasureData { Value = 108, Probability = 4f, MaxCount = 8 };
            array[13] = treasureData;
            treasureData = new TreasureData { Value = 109, Probability = 2f, MaxCount = 4 };
            array[14] = treasureData;
            treasureData = new TreasureData { Value = 105, Probability = 1f, MaxCount = 4 };
            array[15] = treasureData;
            treasureData = new TreasureData { Value = 106, Probability = 1f, MaxCount = 2 };
            array[16] = treasureData;
            treasureData = new TreasureData { Value = 107, Probability = 1f, MaxCount = 1 };
            array[17] = treasureData;
            treasureData = new TreasureData { Value = 234, Probability = 1f, MaxCount = 4 };
            array[18] = treasureData;
            treasureData = new TreasureData { Value = 235, Probability = 1f, MaxCount = 2 };
            array[19] = treasureData;
            treasureData = new TreasureData { Value = 236, Probability = 1f, MaxCount = 1 };
            array[20] = treasureData;
            treasureData = new TreasureData { Value = 132, Probability = 2f, MaxCount = 2 };
            array[21] = treasureData;
            treasureData = new TreasureData { Value = Terrain.MakeBlockValue(173, 0, 6), Probability = 2f, MaxCount = 8 };
            array[22] = treasureData;
            treasureData = new TreasureData { Value = Terrain.MakeBlockValue(173, 0, 7), Probability = 8f, MaxCount = 8 };
            array[23] = treasureData;
            treasureData = new TreasureData { Value = Terrain.MakeBlockValue(173, 0, 5), Probability = 8f, MaxCount = 8 };
            array[24] = treasureData;
            treasureData = new TreasureData { Value = Terrain.MakeBlockValue(173, 0, 6), Probability = 2f, MaxCount = 8 };
            array[25] = treasureData;
            treasureData = new TreasureData { Value = Terrain.MakeBlockValue(119, 0, 0), Probability = 2f, MaxCount = 8 };
            array[26] = treasureData;
            treasureData = new TreasureData { Value = Terrain.MakeBlockValue(119, 0, 1), Probability = 2f, MaxCount = 8 };
            array[27] = treasureData;
            treasureData = new TreasureData { Value = Terrain.MakeBlockValue(119, 0, 2), Probability = 2f, MaxCount = 8 };
            array[28] = treasureData;
            treasureData = new TreasureData { Value = Terrain.MakeBlockValue(119, 0, 3), Probability = 2f, MaxCount = 8 };
            array[29] = treasureData;
            treasureData = new TreasureData { Value = Terrain.MakeBlockValue(119, 0, 4), Probability = 2f, MaxCount = 8 };
            array[30] = treasureData;
            treasureData = new TreasureData { Value = 191, Probability = 4f, MaxCount = 1 };
            array[31] = treasureData;
            treasureData = new TreasureData {
                Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.CopperArrow)), Probability = 2f, MaxCount = 2
            };
            array[32] = treasureData;
            treasureData = new TreasureData {
                Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.IronArrow)), Probability = 2f, MaxCount = 2
            };
            array[33] = treasureData;
            treasureData = new TreasureData {
                Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.DiamondArrow)), Probability = 1f, MaxCount = 2
            };
            array[34] = treasureData;
            treasureData = new TreasureData {
                Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.FireArrow)), Probability = 2f, MaxCount = 2
            };
            array[35] = treasureData;
            treasureData = new TreasureData { Value = 200, Probability = 1f, MaxCount = 1 };
            array[36] = treasureData;
            treasureData = new TreasureData {
                Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.IronBolt)), Probability = 2f, MaxCount = 2
            };
            array[37] = treasureData;
            treasureData = new TreasureData {
                Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.DiamondBolt)), Probability = 1f, MaxCount = 2
            };
            array[38] = treasureData;
            treasureData = new TreasureData {
                Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.ExplosiveBolt)), Probability = 1f, MaxCount = 2
            };
            array[39] = treasureData;
            treasureData = new TreasureData { Value = 212, Probability = 1f, MaxCount = 1 };
            array[40] = treasureData;
            treasureData = new TreasureData { Value = 124, Probability = 1f, MaxCount = 1 };
            array[41] = treasureData;
            treasureData = new TreasureData { Value = 125, Probability = 1f, MaxCount = 1 };
            array[42] = treasureData;
            treasureData = new TreasureData { Value = 82, Probability = 1f, MaxCount = 1 };
            array[43] = treasureData;
            treasureData = new TreasureData { Value = 116, Probability = 1f, MaxCount = 1 };
            array[44] = treasureData;
            treasureData = new TreasureData { Value = 36, Probability = 1f, MaxCount = 1 };
            array[45] = treasureData;
            treasureData = new TreasureData { Value = 113, Probability = 1f, MaxCount = 1 };
            array[46] = treasureData;
            treasureData = new TreasureData { Value = 38, Probability = 1f, MaxCount = 1 };
            array[47] = treasureData;
            treasureData = new TreasureData { Value = 115, Probability = 1f, MaxCount = 1 };
            array[48] = treasureData;
            treasureData = new TreasureData { Value = 37, Probability = 1f, MaxCount = 1 };
            array[49] = treasureData;
            treasureData = new TreasureData { Value = 114, Probability = 1f, MaxCount = 1 };
            array[50] = treasureData;
            treasureData = new TreasureData { Value = 171, Probability = 1f, MaxCount = 1 };
            array[51] = treasureData;
            treasureData = new TreasureData { Value = 172, Probability = 1f, MaxCount = 1 };
            array[52] = treasureData;
            treasureData = new TreasureData { Value = 90, Probability = 1f, MaxCount = 1 };
            array[53] = treasureData;
            treasureData = new TreasureData { Value = 160, Probability = 1f, MaxCount = 1 };
            array[54] = treasureData;
            treasureData = new TreasureData { Value = 158, Probability = 2f, MaxCount = 1 };
            array[55] = treasureData;
            treasureData = new TreasureData { Value = 133, Probability = 1f, MaxCount = 10 };
            array[56] = treasureData;
            treasureData = new TreasureData { Value = 179, Probability = 1f, MaxCount = 2 };
            array[57] = treasureData;
            treasureData = new TreasureData { Value = 142, Probability = 1f, MaxCount = 2 };
            array[58] = treasureData;
            treasureData = new TreasureData { Value = 141, Probability = 1f, MaxCount = 2 };
            array[59] = treasureData;
            treasureData = new TreasureData { Value = 237, Probability = 1f, MaxCount = 2 };
            array[60] = treasureData;
            m_treasureData = new List<TreasureData>(array);
        }
    }
}