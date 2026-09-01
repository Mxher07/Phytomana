using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemFireBlockBehavior : SubsystemBlockBehavior, IUpdateable {
        public class FireData {
            public Point3 Point;

            public float Time0;

            public float Time1;

            public float Time2;

            public float Time3;

            public float Time5;

            public float FireExpandability;
        }

        public SubsystemTime m_subsystemTime;

        public SubsystemGameWidgets m_subsystemViews;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemAmbientSounds m_subsystemAmbientSounds;

        public Dictionary<Point3, float> m_expansionProbabilities = [];

        public Dictionary<Point3, FireData> m_fireData = [];

        public DynamicArray<Point3> m_firePointsCopy = [];

        public Dictionary<Point3, float> m_toBurnAway = [];

        public Dictionary<Point3, float> m_toExpand = [];

        public int m_copyIndex;

        public float m_remainderToScan;

        public double m_lastScanTime;

        public float m_lastScanDuration;

        public Random m_random = new();

        public float m_fireSoundVolume;

        public float m_fireSoundIntensity;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override int[] HandledBlocks => [104];

        public bool IsCellOnFire(int x, int y, int z) {
            for (int i = 0; i < 4; i++) {
                Point3 point = CellFace.FaceToPoint3(i);
                int cellValue = SubsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z);
                if (Terrain.ExtractContents(cellValue) == 104) {
                    int num = Terrain.ExtractData(cellValue);
                    int num2 = CellFace.OppositeFace(i);
                    if ((num & (1 << num2)) != 0) {
                        return true;
                    }
                }
            }
            int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
            if (Terrain.ExtractContents(cellValue2) == 104
                && Terrain.ExtractData(cellValue2) == 0) {
                return true;
            }
            return false;
        }

        public bool SetCellOnFire(int x, int y, int z, float fireExpandability) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
            int num = Terrain.ExtractContents(cellValue);
            if (BlocksManager.Blocks[num].GetFireDuration(cellValue) == 0f) {
                return false;
            }
            bool result = false;
            for (int i = 0; i < 5; i++) {
                Point3 point = CellFace.FaceToPoint3(i);
                int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z);
                int num2 = Terrain.ExtractContents(cellValue2);
                if (num2 == 0
                    || num2 == 104
                    || num2 == 61) {
                    int num3 = num2 == 104 ? Terrain.ExtractData(cellValue2) : 0;
                    int num4 = CellFace.OppositeFace(i);
                    num3 |= (1 << num4) & 0xF;
                    cellValue = Terrain.ReplaceData(Terrain.ReplaceContents(104), num3);
                    AddFire(x + point.X, y + point.Y, z + point.Z, fireExpandability);
                    SubsystemTerrain.ChangeCell(x + point.X, y + point.Y, z + point.Z, cellValue);
                    result = true;
                }
            }
            return result;
        }

        public virtual void Update(float dt) {
            if (m_firePointsCopy.Count == 0) {
                m_firePointsCopy.Count += m_fireData.Count;
                m_fireData.Keys.CopyTo(m_firePointsCopy.Array, 0);
                m_copyIndex = 0;
                m_lastScanDuration = (float)(m_subsystemTime.GameTime - m_lastScanTime);
                m_lastScanTime = m_subsystemTime.GameTime;
                if (m_firePointsCopy.Count == 0) {
                    m_fireSoundVolume = 0f;
                }
            }
            if (m_firePointsCopy.Count > 0) {
                float num = MathUtils.Min(1f * dt * m_firePointsCopy.Count + m_remainderToScan, 50f);
                int num2 = (int)num;
                m_remainderToScan = num - num2;
                int num3 = MathUtils.Min(m_copyIndex + num2, m_firePointsCopy.Count);
                while (m_copyIndex < num3) {
                    if (m_fireData.TryGetValue(m_firePointsCopy.Array[m_copyIndex], out FireData value)) {
                        int x = value.Point.X;
                        int y = value.Point.Y;
                        int z = value.Point.Z;
                        int num4 = Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(x, y, z));
                        m_fireSoundIntensity += 1f / (m_subsystemAudio.CalculateListenerDistanceSquared(new Vector3(x, y, z)) + 0.01f);
                        if ((num4 & 1) != 0) {
                            value.Time0 -= m_lastScanDuration;
                            if (value.Time0 <= 0f) {
                                QueueBurnAway(x, y, z + 1, value.FireExpandability * 0.85f);
                            }
                            foreach (KeyValuePair<Point3, float> expansionProbability in m_expansionProbabilities) {
                                if (m_random.Float(0f, 1f) < expansionProbability.Value * m_lastScanDuration * value.FireExpandability) {
                                    m_toExpand[new Point3(
                                        x + expansionProbability.Key.X,
                                        y + expansionProbability.Key.Y,
                                        z + 1 + expansionProbability.Key.Z
                                    )] = value.FireExpandability * 0.85f;
                                }
                            }
                        }
                        if ((num4 & 2) != 0) {
                            value.Time1 -= m_lastScanDuration;
                            if (value.Time1 <= 0f) {
                                QueueBurnAway(x + 1, y, z, value.FireExpandability * 0.85f);
                            }
                            foreach (KeyValuePair<Point3, float> expansionProbability2 in m_expansionProbabilities) {
                                if (m_random.Float(0f, 1f) < expansionProbability2.Value * m_lastScanDuration * value.FireExpandability) {
                                    m_toExpand[new Point3(
                                        x + 1 + expansionProbability2.Key.X,
                                        y + expansionProbability2.Key.Y,
                                        z + expansionProbability2.Key.Z
                                    )] = value.FireExpandability * 0.85f;
                                }
                            }
                        }
                        if ((num4 & 4) != 0) {
                            value.Time2 -= m_lastScanDuration;
                            if (value.Time2 <= 0f) {
                                QueueBurnAway(x, y, z - 1, value.FireExpandability * 0.85f);
                            }
                            foreach (KeyValuePair<Point3, float> expansionProbability3 in m_expansionProbabilities) {
                                if (m_random.Float(0f, 1f) < expansionProbability3.Value * m_lastScanDuration * value.FireExpandability) {
                                    m_toExpand[new Point3(
                                        x + expansionProbability3.Key.X,
                                        y + expansionProbability3.Key.Y,
                                        z - 1 + expansionProbability3.Key.Z
                                    )] = value.FireExpandability * 0.85f;
                                }
                            }
                        }
                        if ((num4 & 8) != 0) {
                            value.Time3 -= m_lastScanDuration;
                            if (value.Time3 <= 0f) {
                                QueueBurnAway(x - 1, y, z, value.FireExpandability * 0.85f);
                            }
                            foreach (KeyValuePair<Point3, float> expansionProbability4 in m_expansionProbabilities) {
                                if (m_random.Float(0f, 1f) < expansionProbability4.Value * m_lastScanDuration * value.FireExpandability) {
                                    m_toExpand[new Point3(
                                        x - 1 + expansionProbability4.Key.X,
                                        y + expansionProbability4.Key.Y,
                                        z + expansionProbability4.Key.Z
                                    )] = value.FireExpandability * 0.85f;
                                }
                            }
                        }
                        if (num4 == 0) {
                            value.Time5 -= m_lastScanDuration;
                            if (value.Time5 <= 0f) {
                                QueueBurnAway(x, y - 1, z, value.FireExpandability * 0.85f);
                            }
                        }
                    }
                    m_copyIndex++;
                }
                if (m_copyIndex >= m_firePointsCopy.Count) {
                    m_fireSoundVolume = 0.75f * m_fireSoundIntensity;
                    m_firePointsCopy.Clear();
                    m_fireSoundIntensity = 0f;
                }
            }
            if (m_subsystemTime.PeriodicGameTimeEvent(5.0, 0.0)) {
                int num5 = 0;
                int num6 = 0;
                foreach (KeyValuePair<Point3, float> item in m_toBurnAway) {
                    Point3 key = item.Key;
                    float value2 = item.Value;
                    SubsystemTerrain.ChangeCell(key.X, key.Y, key.Z, Terrain.ReplaceContents(0));
                    if (value2 > 0.25f) {
                        for (int i = 0; i < 5; i++) {
                            Point3 point = CellFace.FaceToPoint3(i);
                            SetCellOnFire(key.X + point.X, key.Y + point.Y, key.Z + point.Z, value2);
                        }
                    }
                    float num7 = m_subsystemViews.CalculateDistanceFromNearestView(new Vector3(key));
                    if (num5 < 15
                        && num7 < 24f) {
                        m_subsystemParticles.AddParticleSystem(new BurntDebrisParticleSystem(SubsystemTerrain, key.X, key.Y, key.Z));
                        num5++;
                    }
                    if (num6 < 4
                        && num7 < 16f) {
                        m_subsystemAudio.PlayRandomSound(
                            "Audio/Sizzles",
                            1f,
                            m_random.Float(-0.25f, 0.25f),
                            new Vector3(key.X, key.Y, key.Z),
                            3f,
                            true
                        );
                        num6++;
                    }
                }
                foreach (KeyValuePair<Point3, float> item2 in m_toExpand) {
                    SetCellOnFire(item2.Key.X, item2.Key.Y, item2.Key.Z, item2.Value);
                }
                m_toBurnAway.Clear();
                m_toExpand.Clear();
            }
            m_subsystemAmbientSounds.FireSoundVolume = MathUtils.Max(m_subsystemAmbientSounds.FireSoundVolume, m_fireSoundVolume);
        }

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int num = Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(x, y, z));
            int cellValue_px = SubsystemTerrain.Terrain.GetCellValue(x + 1, y, z);
            int cellValue_nx = SubsystemTerrain.Terrain.GetCellValue(x - 1, y, z);
            int cellValue_py = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
            int cellValue_ny = SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z);
            int cellValue_pz = SubsystemTerrain.Terrain.GetCellValue(x, y, z + 1);
            int cellValue_nz = SubsystemTerrain.Terrain.GetCellValue(x, y, z - 1);
            if ((num & 1) != 0
                && BlocksManager.Blocks[Terrain.ExtractContents(cellValue_pz)].GetFireDuration(cellValue_pz) == 0f) {
                num &= -2;
            }
            if ((num & 2) != 0
                && BlocksManager.Blocks[Terrain.ExtractContents(cellValue_px)].GetFireDuration(cellValue_px) == 0f) {
                num &= -3;
            }
            if ((num & 4) != 0
                && BlocksManager.Blocks[Terrain.ExtractContents(cellValue_nz)].GetFireDuration(cellValue_nz) == 0f) {
                num &= -5;
            }
            if ((num & 8) != 0
                && BlocksManager.Blocks[Terrain.ExtractContents(cellValue_nx)].GetFireDuration(cellValue_nx) == 0f) {
                num &= -9;
            }
            if (m_fireData.TryGetValue(new Point3(x, y, z), out FireData value)) {
                if ((num & 1) != 0
                    && neighborX == x
                    && neighborY == y
                    && neighborZ == z + 1) {
                    InitializeFireDataTime(value, 0);
                }
                if ((num & 2) != 0
                    && neighborX == x + 1
                    && neighborY == y
                    && neighborZ == z) {
                    InitializeFireDataTime(value, 1);
                }
                if ((num & 4) != 0
                    && neighborX == x
                    && neighborY == y
                    && neighborZ == z - 1) {
                    InitializeFireDataTime(value, 2);
                }
                if ((num & 8) != 0
                    && neighborX == x - 1
                    && neighborY == y
                    && neighborZ == z) {
                    InitializeFireDataTime(value, 3);
                }
                if (num == 0
                    && neighborX == x
                    && neighborY == y - 1
                    && neighborZ == z) {
                    InitializeFireDataTime(value, 5);
                }
            }
            int contents = 104;
            if (num == 0
                && BlocksManager.Blocks[Terrain.ExtractContents(cellValue_ny)].GetFireDuration(cellValue_ny) == 0f) {
                contents = 0;
            }
            int value2 = Terrain.ReplaceData(Terrain.ReplaceContents(contents), num);
            SubsystemTerrain.ChangeCell(x, y, z, value2);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            AddFire(x, y, z, 1f);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            RemoveFire(x, y, z);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            AddFire(x, y, z, 1f);
        }

        public override void OnChunkDiscarding(TerrainChunk chunk) {
            List<Point3> list = new();
            foreach (Point3 key in m_fireData.Keys) {
                if (key.X >= chunk.Origin.X
                    && key.X < chunk.Origin.X + 16
                    && key.Z >= chunk.Origin.Y
                    && key.Z < chunk.Origin.Y + 16) {
                    list.Add(key);
                }
            }
            foreach (Point3 item in list) {
                RemoveFire(item.X, item.Y, item.Z);
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemViews = Project.FindSubsystem<SubsystemGameWidgets>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemAmbientSounds = Project.FindSubsystem<SubsystemAmbientSounds>(true);
            for (int i = -2; i <= 2; i++) {
                for (int j = -1; j <= 2; j++) {
                    for (int k = -2; k <= 2; k++) {
                        if (i != 0
                            || j != 0
                            || k != 0) {
                            float num = j < 0 ? 1.5f : 2.5f;
                            if (MathF.Sqrt(i * i + j * j + k * k) <= num) {
                                float num2 = MathF.Sqrt(i * i + k * k);
                                float num3 = j > 0 ? 0.5f * j : -j;
                                m_expansionProbabilities[new Point3(i, j, k)] = 0.02f / (num2 + num3);
                            }
                        }
                    }
                }
            }
        }

        public void AddFire(int x, int y, int z, float expandability) {
            Point3 point = new(x, y, z);
            if (!m_fireData.ContainsKey(point)) {
                FireData fireData = new() { Point = point, FireExpandability = expandability };
                InitializeFireDataTimes(fireData);
                m_fireData[point] = fireData;
            }
        }

        public void RemoveFire(int x, int y, int z) {
            Point3 key = new(x, y, z);
            m_fireData.Remove(key);
        }

        public void InitializeFireDataTimes(FireData fireData) {
            InitializeFireDataTime(fireData, 0);
            InitializeFireDataTime(fireData, 1);
            InitializeFireDataTime(fireData, 2);
            InitializeFireDataTime(fireData, 3);
            InitializeFireDataTime(fireData, 5);
        }

        public void InitializeFireDataTime(FireData fireData, int face) {
            Point3 point = CellFace.FaceToPoint3(face);
            int x = fireData.Point.X + point.X;
            int y = fireData.Point.Y + point.Y;
            int z = fireData.Point.Z + point.Z;
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
            switch (face) {
                case 4: break;
                case 0: fireData.Time0 = block.GetFireDuration(cellValue) * m_random.Float(0.75f, 1.25f); break;
                case 1: fireData.Time1 = block.GetFireDuration(cellValue) * m_random.Float(0.75f, 1.25f); break;
                case 2: fireData.Time2 = block.GetFireDuration(cellValue) * m_random.Float(0.75f, 1.25f); break;
                case 3: fireData.Time3 = block.GetFireDuration(cellValue) * m_random.Float(0.75f, 1.25f); break;
                case 5: fireData.Time5 = block.GetFireDuration(cellValue) * m_random.Float(0.75f, 1.25f); break;
            }
        }

        public void QueueBurnAway(int x, int y, int z, float expandability) {
            Point3 key = new(x, y, z);
            m_toBurnAway.TryAdd(key, expandability);
        }
    }
}