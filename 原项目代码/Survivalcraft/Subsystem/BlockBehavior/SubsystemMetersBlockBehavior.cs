using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemMetersBlockBehavior : SubsystemBlockBehavior, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemWeather m_subsystemWeather;

        public SubsystemSky m_subsystemSky;

        public Dictionary<Point3, int> m_thermometersByPoint = [];

        public DynamicArray<Point3> m_thermometersToSimulate = [];

        public int m_thermometersToSimulateIndex;

        public const int m_diameterBits = 6;

        public const int m_diameter = 64;

        public const int m_diameterMask = 63;

        public const int m_radius = 32;

        public DynamicArray<int> m_toVisit = [];

        public int[] m_visited = new int[8192];

        public override int[] HandledBlocks => [120, 121];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            Point3 point = CellFace.FaceToPoint3(Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(x, y, z)));
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x - point.X, y - point.Y, z - point.Z);
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
            AddRemoveMeter(value, x, y, z);
        }

        public override void OnBlockRemoved(int value, int oldValue, int x, int y, int z) {
            AddRemoveMeter(oldValue, x, y, z);
        }

        public override void OnBlockModified(int value, int oldValue, int x, int y, int z) {
            AddRemoveMeter(value, x, y, z);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            AddRemoveMeter(value, x, y, z);
        }

        public override void OnChunkDiscarding(TerrainChunk chunk) {
            List<Point3> list = new();
            foreach (Point3 key in m_thermometersByPoint.Keys) {
                if (key.X >= chunk.Origin.X
                    && key.X < chunk.Origin.X + 16
                    && key.Z >= chunk.Origin.Y
                    && key.Z < chunk.Origin.Y + 16) {
                    list.Add(key);
                }
            }
            foreach (Point3 item in list) {
                AddRemoveMeter(0, item.X, item.Y, item.Z);
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
        }

        public virtual void Update(float dt) {
            if (m_thermometersToSimulateIndex < m_thermometersToSimulate.Count) {
                double period = Math.Max(4 / m_thermometersToSimulate.Count, 0.5);
                if (m_subsystemTime.PeriodicGameTimeEvent(period, 0.0)) {
                    Point3 point = m_thermometersToSimulate.Array[m_thermometersToSimulateIndex];
                    SimulateThermometer(point.X, point.Y, point.Z, true);
                    m_thermometersToSimulateIndex++;
                }
            }
            else if (m_thermometersByPoint.Count > 0) {
                m_thermometersToSimulateIndex = 0;
                m_thermometersToSimulate.Clear();
                m_thermometersToSimulate.AddRange((IEnumerable<Point3>)m_thermometersByPoint.Keys);
            }
        }

        public int GetThermometerReading(int x, int y, int z) {
            m_thermometersByPoint.TryGetValue(new Point3(x, y, z), out int value);
            return value;
        }

        /// <summary>
        /// 计算温度
        /// </summary>
        /// <param name="x">世界坐标 X</param>
        /// <param name="y">世界坐标 Y</param>
        /// <param name="z">世界坐标 Z</param>
        /// <param name="meterTemperature">温度计（或者生物体）当前的温度（上一帧的温度）</param>
        /// <param name="meterInsulation">隔热系数。如果这个值很高，外界温度对目标温度的影响会变慢（例如穿了厚衣服）</param>
        /// <param name="targetTemperature">最终计算出的目标平衡温度（算上隔热后）</param>
        /// <param name="targetTemperatureFlux">温度变化率（温度改变的速度）</param>
        /// <param name="environmentTemperature">纯环境温度（不考虑自身隔热）</param>
        public void CalculateTemperature(int x,
            int y,
            int z,
            float meterTemperature,
            float meterInsulation,
            out float targetTemperature,
            out float targetTemperatureFlux,
            out float environmentTemperature) {
            m_toVisit.Count = 0;
            Array.Clear(m_visited);
            Terrain terrain = SubsystemTerrain.Terrain;
            float num = 0f;
            float num2 = 0f;
            float num3 = 0f;
            float num4 = 0f;
            float num5 = 0f;
            float num6 = 0f;
            m_toVisit.Add(133152);
            for (int i = 0; i < m_toVisit.Count; i++) {
                int num7 = m_toVisit.Array[i];
                if ((m_visited[num7 / 32] & (1 << num7)) != 0) {
                    continue;
                }
                m_visited[num7 / 32] |= 1 << num7;
                int num8 = (num7 & 0x3F) - 32;
                int num9 = ((num7 >> 6) & 0x3F) - 32;
                int num10 = ((num7 >> 12) & 0x3F) - 32;
                int num11 = num8 + x;
                int num12 = num9 + y;
                int num13 = num10 + z;
                TerrainChunk chunkAtCell = terrain.GetChunkAtCell(num11, num13);
                if (chunkAtCell == null
                    || num12 < 0
                    || num12 >= 256) {
                    continue;
                }
                int x2 = num11 & 0xF;
                int y2 = num12;
                int z2 = num13 & 0xF;
                int cellValueFast = chunkAtCell.GetCellValueFast(x2, y2, z2);
                int num14 = Terrain.ExtractContents(cellValueFast);
                Block block = BlocksManager.Blocks[num14];
                float heat = GetHeat(cellValueFast);
                if (heat > 0f) {
                    int num15 = Math.Abs(num8) + Math.Abs(num9) + Math.Abs(num10);
                    int num16 = num15 <= 0 ? 1 : 4 * num15 * num15 + 2;
                    float num17 = 1f / num16;
                    num5 += num17 * 36f * heat;
                    num6 += num17;
                }
                else if (block.IsHeatBlocker(cellValueFast)) {
                    int num18 = Math.Abs(num8) + Math.Abs(num9) + Math.Abs(num10);
                    int num19 = num18 <= 0 ? 1 : 4 * num18 * num18 + 2;
                    float num20 = 1f / num19;
                    float num21 = terrain.SeasonTemperature;
                    float num22 = SubsystemWeather.GetTemperatureAdjustmentAtHeight(y2);
                    float num23 = block is WaterBlock ? MathUtils.Max(chunkAtCell.GetTemperatureFast(x2, z2) + num21 - 7f, 0f) + num22 :
                        !(block is IceBlock) ? MathUtils.Max(chunkAtCell.GetTemperatureFast(x2, z2) + num21, 0f) + num22 :
                        MathUtils.Max(0f + num21 + num22, 0f);
                    num += num20 * num23;
                    num2 += num20;
                }
                else if (y >= chunkAtCell.GetTopHeightFast(x2, z2)) {
                    int num24 = Math.Abs(num8) + Math.Abs(num9) + Math.Abs(num10);
                    int num25 = num24 <= 0 ? 1 : 4 * num24 * num24 + 2;
                    float num26 = 1f / num25;
                    PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(x, z);
                    float num27 = terrain.SeasonTemperature;
                    float num28 = y >= precipitationShaftInfo.YLimit ? MathUtils.Lerp(0f, -2f, precipitationShaftInfo.Intensity) : 0f;
                    float num29 = MathUtils.Lerp(-6f, 0f, m_subsystemSky.SkyLightIntensity);
                    float num30 = SubsystemWeather.GetTemperatureAdjustmentAtHeight(y2);
                    num3 += num26 * (MathUtils.Max(chunkAtCell.GetTemperatureFast(x2, z2) + num27, 0f) + num28 + num29 + num30);
                    num4 += num26;
                }
                else if (m_toVisit.Count < 4090) {
                    if (num8 > -30) {
                        m_toVisit.Add(num7 - 1);
                    }
                    if (num8 < 30) {
                        m_toVisit.Add(num7 + 1);
                    }
                    if (num9 > -30) {
                        m_toVisit.Add(num7 - 64);
                    }
                    if (num9 < 30) {
                        m_toVisit.Add(num7 + 64);
                    }
                    if (num10 > -30) {
                        m_toVisit.Add(num7 - 4096);
                    }
                    if (num10 < 30) {
                        m_toVisit.Add(num7 + 4096);
                    }
                }
            }
            float num31 = 0f;
            for (int j = -7; j <= 7; j++) {
                for (int k = -7; k <= 7; k++) {
                    TerrainChunk chunkAtCell2 = SubsystemTerrain.Terrain.GetChunkAtCell(x + j, z + k);
                    if (chunkAtCell2 == null
                        || chunkAtCell2.State < TerrainChunkState.InvalidLight) {
                        continue;
                    }
                    for (int l = -7; l <= 7; l++) {
                        int num32 = j * j + l * l + k * k;
                        if (num32 > 49
                            || num32 <= 0) {
                            continue;
                        }
                        int x3 = (x + j) & 0xF;
                        int num33 = y + l;
                        int z3 = (z + k) & 0xF;
                        if (num33 >= 0
                            && num33 < 256) {
                            float heat2 = GetHeat(chunkAtCell2.GetCellValueFast(x3, num33, z3));
                            if (heat2 > 0f
                                && !SubsystemTerrain.Raycast(
                                        new Vector3(x, y, z) + new Vector3(0.5f, 0.75f, 0.5f),
                                        new Vector3(x + j, y + l, z + k) + new Vector3(0.5f, 0.75f, 0.5f),
                                        false,
                                        true,
                                        delegate(int raycastValue, float _) {
                                            Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(raycastValue)];
                                            return block2.IsCollidable_(raycastValue) && !block2.IsTransparent_(raycastValue);
                                        }
                                    )
                                    .HasValue) {
                                num31 += heat2 * 3f / (num32 + 2);
                            }
                        }
                    }
                }
            }
            float num34 = 0f;
            float num35 = 0f;
            if (num31 > 0f) {
                float num36 = 3f * num31;
                num34 += 35f * num36;
                num35 += num36;
            }
            if (num2 > 0f) {
                float num37 = 1f;
                num34 += num / num2 * num37;
                num35 += num37;
            }
            if (num4 > 0f) {
                float num38 = 4f * MathF.Pow(num4, 0.25f);
                num34 += num3 / num4 * num38;
                num35 += num38;
            }
            if (num6 > 0f) {
                float num39 = 1.5f * MathF.Pow(num6, 0.25f);
                num34 += num5 / num6 * num39;
                num35 += num39;
            }
            environmentTemperature = num35 > 0f ? num34 / num35 : meterTemperature;
            if (meterInsulation > 0f) {
                num34 += meterTemperature * meterInsulation;
                num35 += meterInsulation;
            }
            targetTemperature = num35 > 0f ? num34 / num35 : meterTemperature;
            targetTemperatureFlux = 0.01f + 0.004f * MathUtils.Max(num35 - meterInsulation, 0f);
        }

        public static float GetHeat(int value) {
            int num = Terrain.ExtractContents(value);
            return BlocksManager.Blocks[num].GetHeat(value);
        }

        public void SimulateThermometer(int x, int y, int z, bool invalidateTerrainOnChange) {
            Point3 key = new(x, y, z);
            if (!m_thermometersByPoint.TryGetValue(key, out int num)) {
                return;
            }
            CalculateTemperature(
                x,
                y,
                z,
                0f,
                0f,
                out float _,
                out float _,
                out float environmentTemperature
            );
            int num2 = (int)MathF.Round(environmentTemperature);
            if (num2 == num) {
                return;
            }
            m_thermometersByPoint[new Point3(x, y, z)] = num2;
            if (invalidateTerrainOnChange) {
                TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(x, z);
                if (chunkAtCell != null) {
                    SubsystemTerrain.TerrainUpdater.DowngradeChunkNeighborhoodState(chunkAtCell.Coords, 0, TerrainChunkState.InvalidVertices1, true);
                }
            }
        }

        public void AddRemoveMeter(int value, int x, int y, int z) {
            if (Terrain.ExtractContents(value) == 120) {
                m_thermometersByPoint[new Point3(x, y, z)] = 0;
                SimulateThermometer(x, y, z, false);
            }
            else {
                m_thermometersByPoint.Remove(new Point3(x, y, z));
            }
        }
    }
}