using Engine;

namespace Game {
    public static class PlantsManager {
        public static List<TerrainBrush>[] m_treeBrushesByType;

        public static int[] m_treeTrunksByType;

        public static int[] m_treeLeavesByType;

        static PlantsManager() {
            m_treeBrushesByType = new List<TerrainBrush>[EnumUtils.GetEnumValues<TreeType>().Max() + 1];
            m_treeTrunksByType = [9, 10, 11, 11, 255, 262];
            m_treeLeavesByType = [
                Terrain.MakeBlockValue(12, 0, DeciduousLeavesBlock.SetSeason(0, Season.Spring)),
                Terrain.MakeBlockValue(13, 0, DeciduousLeavesBlock.SetSeason(0, Season.Spring)),
                14,
                225,
                Terrain.MakeBlockValue(256, 0, DeciduousLeavesBlock.SetSeason(0, Season.Spring)),
                Terrain.MakeBlockValue(263, 0, DeciduousLeavesBlock.SetSeason(0, Season.Spring))
            ];
            Random random = new(33);
            m_treeBrushesByType[0] = [];
            for (int i = 0; i < 16; i++) {
                int[] array = [
                    5,
                    6,
                    7,
                    8,
                    9,
                    10,
                    11,
                    11,
                    12,
                    12,
                    13,
                    13,
                    14,
                    15,
                    16,
                    18
                ];
                int height4 = array[i];
                int branchesCount = (int)MathUtils.Lerp(10f, 22f, i / 16f);
                TerrainBrush item = CreateTreeBrush(
                    random,
                    GetTreeTrunkValue(TreeType.Oak),
                    GetTreeLeavesValue(TreeType.Oak),
                    height4,
                    branchesCount,
                    3,
                    delegate(int y, int _) {
                        float num7 = 0.4f;
                        if (y < 0.2f * height4) {
                            num7 = 0f;
                        }
                        else if (y >= 0.2f * height4
                            && y <= height4) {
                            num7 *= 1.5f;
                        }
                        return num7;
                    },
                    delegate(int y) {
                        if (y < height4 * 0.3f
                            || y > height4 * 0.9f) {
                            return 0f;
                        }
                        float num6 = y < height4 * 0.7f ? 0.5f * height4 : 0.35f * height4;
                        return random.Float(0.33f, 1f) * num6;
                    }
                );
                m_treeBrushesByType[0].Add(item);
            }
            m_treeBrushesByType[1] = [];
            for (int j = 0; j < 16; j++) {
                int[] array2 = [
                    4,
                    5,
                    6,
                    7,
                    7,
                    8,
                    8,
                    9,
                    9,
                    9,
                    10,
                    10,
                    10,
                    11,
                    11,
                    12
                ];
                int height3 = array2[j];
                int branchesCount2 = (int)MathUtils.Lerp(0f, 20f, j / 16f);
                TerrainBrush item2 = CreateTreeBrush(
                    random,
                    GetTreeTrunkValue(TreeType.Birch),
                    GetTreeLeavesValue(TreeType.Birch),
                    height3,
                    branchesCount2,
                    3,
                    delegate(int y, int _) {
                        float num5 = 0.66f;
                        if (y < height3 / 2 - 1) {
                            num5 = 0f;
                        }
                        else if (y > height3 / 2
                            && y <= height3) {
                            num5 *= 1.5f;
                        }
                        return num5;
                    },
                    y => y < height3 * 0.35f || y > height3 * 0.75f ? 0f : random.Float(0f, 0.33f * height3)
                );
                m_treeBrushesByType[1].Add(item2);
            }
            m_treeBrushesByType[2] = [];
            for (int k = 0; k < 16; k++) {
                int[] array3 = [
                    7,
                    8,
                    9,
                    10,
                    10,
                    11,
                    11,
                    12,
                    12,
                    13,
                    13,
                    14,
                    14,
                    15,
                    16,
                    17
                ];
                int height2 = array3[k];
                int branchesCount3 = height2 * 3;
                TerrainBrush item3 = CreateTreeBrush(
                    random,
                    GetTreeTrunkValue(TreeType.Spruce),
                    GetTreeLeavesValue(TreeType.Spruce),
                    height2,
                    branchesCount3,
                    3,
                    delegate(int y, int _) {
                        float num4 = MathUtils.Lerp(1.4f, 0.3f, y / (float)height2);
                        if (y < 3) {
                            num4 = 0f;
                        }
                        if (y % 2 == 0) {
                            num4 *= 0.3f;
                        }
                        return num4;
                    },
                    delegate(int y) {
                        if (y < 3
                            || y > height2 * 0.8f) {
                            return 0f;
                        }
                        return y % 2 == 0 ? 0f : MathUtils.Lerp(0.3f * height2, 0f, MathUtils.Saturate(y / (float)height2));
                    }
                );
                m_treeBrushesByType[2].Add(item3);
            }
            m_treeBrushesByType[3] = [];
            for (int l = 0; l < 16; l++) {
                int[] array4 = [
                    20,
                    21,
                    22,
                    23,
                    24,
                    24,
                    25,
                    25,
                    26,
                    26,
                    27,
                    27,
                    28,
                    28,
                    29,
                    30,
                    31,
                    32
                ];
                int height = array4[l];
                int branchesCount4 = height * 3;
                float startHeight = (0.3f + l % 4 * 0.05f) * height;
                TerrainBrush item4 = CreateTreeBrush(
                    random,
                    GetTreeTrunkValue(TreeType.TallSpruce),
                    GetTreeLeavesValue(TreeType.TallSpruce),
                    height,
                    branchesCount4,
                    3,
                    delegate(int y, int _) {
                        float num2 = MathUtils.Saturate(y / (float)height);
                        float num3 = MathUtils.Lerp(1.5f, 0f, MathUtils.Saturate((num2 - 0.6f) / 0.4f));
                        if (y < startHeight) {
                            num3 = 0f;
                        }
                        if (y % 3 != 0
                            && y < height - 4) {
                            num3 *= 0.2f;
                        }
                        return num3;
                    },
                    delegate(int y) {
                        float num = MathUtils.Saturate(y / (float)height);
                        if (y % 3 != 0) {
                            return 0f;
                        }
                        return y < startHeight
                            ? !(y < startHeight - 4f) ? 0.1f * height : 0f
                            : MathUtils.Lerp(0.18f * height, 0f, MathUtils.Saturate((num - 0.6f) / 0.4f));
                    }
                );
                m_treeBrushesByType[3].Add(item4);
            }
            m_treeBrushesByType[4] = [];
            for (int m = 0; m < 16; m++) {
                m_treeBrushesByType[4].Add(CreateMimosaBrush(random, MathUtils.Lerp(6f, 9f, m / 15f)));
            }
            m_treeBrushesByType[5] = [];
            for (int n = 0; n < 16; n++) {
                int[] array5 = [
                    10,
                    11,
                    11,
                    12,
                    12,
                    13,
                    13,
                    13,
                    14,
                    14,
                    14,
                    15,
                    15,
                    16,
                    16,
                    17,
                    17
                ];
                int height5 = array5[n];
                int branchesCount5 = height5 * 3;
                TerrainBrush item5 = CreateTreeBrush(
                    random,
                    GetTreeTrunkValue(TreeType.Poplar),
                    GetTreeLeavesValue(TreeType.Poplar),
                    height5,
                    branchesCount5,
                    2,
                    delegate(int y, int round) {
                        int num8 = height5 < 14 ? 1 : 2;
                        if (y < num8) {
                            return 0f;
                        }
                        if (round == 0) {
                            return 1f;
                        }
                        if (y == num8) {
                            return 0f;
                        }
                        return y == num8 + 1 ? 0.5f : MathUtils.LinearStep(height5 - 1, num8 + 2, y);
                    },
                    _ => 0f
                );
                m_treeBrushesByType[5].Add(item5);
            }
        }

        public static int GetTreeTrunkValue(TreeType treeType) => m_treeTrunksByType[(int)treeType];

        public static int GetTreeLeavesValue(TreeType treeType) => m_treeLeavesByType[(int)treeType];

        public static ReadOnlyList<TerrainBrush> GetTreeBrushes(TreeType treeType) => new(m_treeBrushesByType[(int)treeType]);

        public static int GenerateRandomPlantValue(Random random, int groundValue, int temperature, int humidity, int y) {
            switch (Terrain.ExtractContents(groundValue)) {
                case 2:
                case 8:
                    if (humidity >= 6) {
                        if (!(random.Float(0f, 1f) < humidity / 60f)) {
                            break;
                        }
                        int result = Terrain.MakeBlockValue(19, 0, TallGrassBlock.SetIsSmall(0, false));
                        if (!SubsystemWeather.IsPlaceFrozen(temperature, y)) {
                            float num = random.Float(0f, 1f);
                            if (num < 0.04f) {
                                result = Terrain.MakeBlockValue(20);
                            }
                            else if (num < 0.07f) {
                                result = Terrain.MakeBlockValue(24);
                            }
                            else if (num < 0.09f) {
                                result = Terrain.MakeBlockValue(25);
                            }
                            else if (num < 0.17f) {
                                result = Terrain.MakeBlockValue(174, 0, RyeBlock.SetIsWild(RyeBlock.SetSize(0, 7), true));
                            }
                            else if (num < 0.19f) {
                                result = Terrain.MakeBlockValue(204, 0, CottonBlock.SetIsWild(CottonBlock.SetSize(0, 2), true));
                            }
                        }
                        return result;
                    }
                    if (random.Float(0f, 1f) < 0.025f) {
                        if (random.Float(0f, 1f) < 0.2f) {
                            return Terrain.MakeBlockValue(99, 0, 0);
                        }
                        return Terrain.MakeBlockValue(28, 0, 0);
                    }
                    break;
                case 7:
                    if (humidity < 8
                        && random.Float(0f, 1f) < 0.01f) {
                        if (random.Float(0f, 1f) < 0.05f) {
                            return Terrain.MakeBlockValue(99, 0, 0);
                        }
                        return Terrain.MakeBlockValue(28, 0, 0);
                    }
                    break;
            }
            return 0;
        }

        public static TreeType? GenerateRandomTreeType(Random random, int temperature, int humidity, int y, float densityMultiplier = 1f) {
            TreeType? result = null;
            float num = random.Float() * CalculateTreeProbability(TreeType.Oak, temperature, humidity, y);
            float num2 = random.Float() * CalculateTreeProbability(TreeType.Birch, temperature, humidity, y);
            float num3 = random.Float() * CalculateTreeProbability(TreeType.Spruce, temperature, humidity, y);
            float num4 = random.Float() * CalculateTreeProbability(TreeType.TallSpruce, temperature, humidity, y);
            float num5 = random.Float() * CalculateTreeProbability(TreeType.Mimosa, temperature, humidity, y);
            float num7 = random.Float() * CalculateTreeProbability(TreeType.Poplar, temperature, humidity, y);
            float num6 = MathUtils.Max(MathUtils.Max(num, num2, num3, num4), num5, num7);
            if (num6 > 0f) {
                if (num6 == num) {
                    result = TreeType.Oak;
                }
                if (num6 == num2) {
                    result = TreeType.Birch;
                }
                if (num6 == num3) {
                    result = TreeType.Spruce;
                }
                if (num6 == num4) {
                    result = TreeType.TallSpruce;
                }
                if (num6 == num5) {
                    result = TreeType.Mimosa;
                }
                if (num6 == num7) {
                    result = TreeType.Poplar;
                }
            }
            if (result.HasValue
                && random.Bool(densityMultiplier * CalculateTreeDensity(result.Value, temperature, humidity, y))) {
                return result;
            }
            return null;
        }

        public static float CalculateTreeDensity(TreeType treeType, int temperature, int humidity, int y) {
            switch (treeType) {
                case TreeType.Oak: return RangeProbability(humidity, 4f, 15f, 15f, 15f);
                case TreeType.Birch: return RangeProbability(humidity, 4f, 15f, 15f, 15f);
                case TreeType.Spruce: return RangeProbability(humidity, 4f, 15f, 15f, 15f);
                case TreeType.TallSpruce: return RangeProbability(humidity, 4f, 15f, 15f, 15f);
                case TreeType.Mimosa: return 0.04f;
                case TreeType.Poplar:
                    return RangeProbability(temperature, 4f, 8f, 10f, 15f)
                        * RangeProbability(humidity, 3f, 15f, 15f, 15f)
                        * RangeProbability(y, 0f, 0f, 85f, 92f);
                default: return 0f;
            }
        }

        public static float CalculateTreeProbability(TreeType treeType, int temperature, int humidity, int y) {
            switch (treeType) {
                case TreeType.Oak:
                    return RangeProbability(temperature, 4f, 10f, 15f, 15f)
                        * RangeProbability(humidity, 6f, 8f, 15f, 15f)
                        * RangeProbability(y, 0f, 0f, 82f, 87f);
                case TreeType.Birch:
                    return RangeProbability(temperature, 5f, 9f, 11f, 15f)
                        * RangeProbability(humidity, 3f, 15f, 15f, 15f)
                        * RangeProbability(y, 0f, 0f, 82f, 87f);
                case TreeType.Spruce: return RangeProbability(temperature, 0f, 0f, 6f, 10f) * RangeProbability(humidity, 3f, 10f, 11f, 12f);
                case TreeType.TallSpruce:
                    return 0.25f
                        * RangeProbability(temperature, 0f, 0f, 6f, 10f)
                        * RangeProbability(humidity, 9f, 11f, 15f, 15f)
                        * RangeProbability(y, 0f, 0f, 95f, 100f);
                case TreeType.Mimosa: return RangeProbability(temperature, 2f, 4f, 12f, 14f) * RangeProbability(humidity, 0f, 0f, 4f, 6f);
                case TreeType.Poplar:
                    return RangeProbability(temperature, 4f, 8f, 12f, 15f)
                        * RangeProbability(humidity, 3f, 15f, 15f, 15f)
                        * RangeProbability(y, 0f, 0f, 85f, 92f);
                default: return 0f;
            }
        }

        public static float RangeProbability(float v, float a, float b, float c, float d) {
            if (v < a) {
                return 0f;
            }
            if (v < b) {
                return (v - a) / (b - a);
            }
            if (v <= c) {
                return 1f;
            }
            if (v <= d) {
                return 1f - (v - c) / (d - c);
            }
            return 0f;
        }

        public static TerrainBrush CreateTreeBrush(Random random,
            int woodIndex,
            int leavesIndex,
            int height,
            int branchesCount,
            int leavesRounds,
            Func<int, int, float> leavesProbability,
            Func<int, float> branchesLength) {
            TerrainBrush terrainBrush = new();
            terrainBrush.AddRay(
                0,
                -1,
                0,
                0,
                height,
                0,
                1,
                1,
                1,
                woodIndex
            );
            for (int i = 0; i < branchesCount; i++) {
                int x = 0;
                int num = random.Int(0, height);
                int z = 0;
                float s = branchesLength(num);
                Vector3 vector = Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(0f, 0.33f), random.Float(-1f, 1f))) * s;
                int x2 = (int)MathF.Round(vector.X);
                int y = num + (int)MathF.Round(vector.Y);
                int z2 = (int)MathF.Round(vector.Z);
                int cutFace = 0;
                if (MathF.Abs(vector.X) == MathUtils.Max(MathF.Abs(vector.X), MathF.Abs(vector.Y), MathF.Abs(vector.Z))) {
                    cutFace = 1;
                }
                else if (MathF.Abs(vector.Y) == MathUtils.Max(MathF.Abs(vector.X), MathF.Abs(vector.Y), MathF.Abs(vector.Z))) {
                    cutFace = 4;
                }
                terrainBrush.AddRay(
                    x,
                    num,
                    z,
                    x2,
                    y,
                    z2,
                    1,
                    1,
                    1,
                    (Func<int?, int?>)(v => v.HasValue ? null : new int?(Terrain.MakeBlockValue(woodIndex, 0, WoodBlock.SetCutFace(0, cutFace))))
                );
            }
            for (int j = 0; j < leavesRounds; j++) {
                terrainBrush.CalculateBounds(out Point3 min, out Point3 max);
                for (int k = min.X - 1; k <= max.X + 1; k++) {
                    for (int l = min.Z - 1; l <= max.Z + 1; l++) {
                        for (int m = 1; m <= max.Y + 1; m++) {
                            float num2 = leavesProbability(m, j);
                            if (random.Float(0f, 1f) < num2
                                && !terrainBrush.GetValue(k, m, l).HasValue
                                && (terrainBrush.CountNonDiagonalNeighbors(k, m, l, leavesIndex) != 0
                                    || terrainBrush.CountNonDiagonalNeighbors(
                                        k,
                                        m,
                                        l,
                                        (Func<int?, int>)(v => v.HasValue && Terrain.ExtractContents(v.Value) == woodIndex ? 1 : 0)
                                    )
                                    != 0)) {
                                terrainBrush.AddCell(k, m, l, 0);
                            }
                        }
                    }
                }
                terrainBrush.Replace(0, leavesIndex);
            }
            terrainBrush.AddCell(0, height, 0, leavesIndex);
            terrainBrush.Compile();
            return terrainBrush;
        }

        public static TerrainBrush CreateMimosaBrush(Random random, float size) {
            TerrainBrush terrainBrush = new();
            int value = m_treeTrunksByType[4];
            int value2 = m_treeLeavesByType[4];
            terrainBrush.AddRay(
                0,
                -1,
                0,
                0,
                0,
                0,
                1,
                1,
                1,
                value
            );
            List<Point3> list = new();
            float num = random.Float(0f, (float)Math.PI * 2f);
            for (int i = 0; i < 3; i++) {
                float radians = num + i * MathUtils.DegToRad(120f);
                Vector3 v = Vector3.Transform(Vector3.Normalize(new Vector3(1f, random.Float(1f, 1.5f), 0f)), Matrix.CreateRotationY(radians));
                int num2 = random.Int((int)(0.7f * size), (int)size);
                Point3 p = new(0, 0, 0);
                Point3 item = new(Vector3.Round(new Vector3(p) + v * num2));
                terrainBrush.AddRay(
                    p.X,
                    p.Y,
                    p.Z,
                    item.X,
                    item.Y,
                    item.Z,
                    1,
                    1,
                    1,
                    value
                );
                list.Add(item);
            }
            foreach (Point3 item2 in list) {
                float num3 = random.Float(0.3f * size, 0.45f * size);
                int num4 = (int)MathF.Ceiling(num3);
                for (int j = item2.X - num4; j <= item2.X + num4; j++) {
                    for (int k = item2.Y - num4; k <= item2.Y + num4; k++) {
                        for (int l = item2.Z - num4; l <= item2.Z + num4; l++) {
                            int num5 = Math.Abs(j - item2.X) + Math.Abs(k - item2.Y) + Math.Abs(l - item2.Z);
                            float num6 = ((new Vector3(j, k, l) - new Vector3(item2)) * new Vector3(1f, 1.7f, 1f)).Length();
                            if (num6 <= num3
                                && (num3 - num6 > 1f || num5 <= 2 || random.Bool(0.7f))
                                && !terrainBrush.GetValue(j, k, l).HasValue) {
                                terrainBrush.AddCell(j, k, l, value2);
                            }
                        }
                    }
                }
            }
            terrainBrush.Compile();
            return terrainBrush;
        }
    }
}