using Engine;

namespace Game {
    public class TerrainContentsGeneratorFlat : ITerrainContentsGenerator {
        public SubsystemTerrain m_subsystemTerrain;

        public WorldSettings m_worldSettings;

        public Vector2 m_oceanCorner;

        public Vector2? m_islandSize;

        public Vector2 m_shoreRoughnessFrequency;

        public Vector2 m_shoreRoughnessAmplitude;

        public Vector2 m_shoreRoughnessOctaves;

        public float[] m_shoreRoughnessOffset = new float[4];

        public int OceanLevel => m_worldSettings.TerrainLevel + m_worldSettings.SeaLevelOffset;

        public TerrainContentsGeneratorFlat(SubsystemTerrain subsystemTerrain) {
            m_subsystemTerrain = subsystemTerrain;
            SubsystemGameInfo subsystemGameInfo = subsystemTerrain.Project.FindSubsystem<SubsystemGameInfo>(true);
            m_worldSettings = subsystemGameInfo.WorldSettings;
            m_oceanCorner = string.CompareOrdinal(subsystemGameInfo.WorldSettings.OriginalSerializationVersion, "2.1") < 0
                ? m_oceanCorner = new Vector2(2001f, 2001f)
                : m_oceanCorner = new Vector2(-199f, -199f);
            m_islandSize = m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.FlatIsland
                ? new Vector2?(m_worldSettings.IslandSize)
                : null;
            m_shoreRoughnessAmplitude.X = MathF.Pow(m_worldSettings.ShoreRoughness, 2f)
                * (m_islandSize.HasValue ? Math.Min(4f * m_islandSize.Value.X, 400f) : 400f);
            m_shoreRoughnessAmplitude.Y = MathF.Pow(m_worldSettings.ShoreRoughness, 2f)
                * (m_islandSize.HasValue ? Math.Min(4f * m_islandSize.Value.Y, 400f) : 400f);
            m_shoreRoughnessFrequency = MathUtils.Lerp(0.5f, 1f, m_worldSettings.ShoreRoughness) * new Vector2(1f) / m_shoreRoughnessAmplitude;
            m_shoreRoughnessOctaves.X = (int)Math.Clamp(MathF.Log(1f / m_shoreRoughnessFrequency.X) / MathF.Log(2f) - 1f, 1f, 7f);
            m_shoreRoughnessOctaves.Y = (int)Math.Clamp(MathF.Log(1f / m_shoreRoughnessFrequency.Y) / MathF.Log(2f) - 1f, 1f, 7f);
            Random random = new(subsystemGameInfo.WorldSeed);
            m_shoreRoughnessOffset[0] = random.Float(-2000f, 2000f);
            m_shoreRoughnessOffset[1] = random.Float(-2000f, 2000f);
            m_shoreRoughnessOffset[2] = random.Float(-2000f, 2000f);
            m_shoreRoughnessOffset[3] = random.Float(-2000f, 2000f);
        }

        public Vector3 FindCoarseSpawnPosition() {
            for (int i = -400; i <= 400; i += 10) {
                for (int j = -400; j <= 400; j += 10) {
                    Vector2 vector = m_oceanCorner + new Vector2(i, j);
                    float num = CalculateOceanShoreDistance(vector.X, vector.Y);
                    if (num >= 1f
                        && num <= 20f) {
                        return new Vector3(vector.X, CalculateHeight(vector.X, vector.Y), vector.Y);
                    }
                }
            }
            return new Vector3(m_oceanCorner.X, CalculateHeight(m_oceanCorner.X, m_oceanCorner.Y), m_oceanCorner.Y);
        }

        public void GenerateChunkContentsPass1(TerrainChunk chunk) {
            for (int i = 0; i < TerrainChunk.Size; i++) {
                for (int j = 0; j < TerrainChunk.Size; j++) {
                    int num = i + chunk.Origin.X;
                    int num2 = j + chunk.Origin.Y;
                    chunk.SetTemperatureFast(i, j, CalculateTemperature(num, num2));
                    chunk.SetHumidityFast(i, j, CalculateHumidity(num, num2));
                    bool flag = CalculateOceanShoreDistance(num, num2) >= 0f;
                    int num3 = TerrainChunk.CalculateCellIndex(i, 0, j);
                    for (int k = 0; k < TerrainChunk.Height; k++) {
                        int value = Terrain.MakeBlockValue(0);
                        if (flag) {
                            if (k < 2
                                && m_worldSettings.TerrainLevel > 0) {
                                value = Terrain.MakeBlockValue(1);
                            }
                            else if (k < m_worldSettings.TerrainLevel) {
                                value = Terrain.MakeBlockValue(m_worldSettings.TerrainBlockIndex == 8 ? 2 : m_worldSettings.TerrainBlockIndex);
                            }
                            else if (k == m_worldSettings.TerrainLevel) {
                                value = Terrain.MakeBlockValue(m_worldSettings.TerrainBlockIndex);
                            }
                            else if (k <= OceanLevel) {
                                value = Terrain.MakeBlockValue(m_worldSettings.TerrainOceanBlockIndex);
                            }
                        }
                        else if (k < 2
                            && m_worldSettings.TerrainLevel > 0) {
                            value = Terrain.MakeBlockValue(1);
                        }
                        else if (k <= OceanLevel) {
                            value = Terrain.MakeBlockValue(m_worldSettings.TerrainOceanBlockIndex);
                        }
                        chunk.SetCellValueFast(num3 + k, value);
                    }
                }
            }
        }

        public void GenerateChunkContentsPass2(TerrainChunk chunk) {
            UpdateFluidIsTop(chunk);
        }

        public void GenerateChunkContentsPass3(TerrainChunk chunk) { }

        public void GenerateChunkContentsPass4(TerrainChunk chunk) { }

        public float CalculateOceanShoreDistance(float x, float z) {
            float x2 = 0f;
            float x3 = 0f;
            float y = 0f;
            float y2 = 0f;
            if (m_shoreRoughnessAmplitude.X > 0f) {
                x2 = m_shoreRoughnessAmplitude.X
                    * SimplexNoise.OctavedNoise(z + m_shoreRoughnessOffset[0], m_shoreRoughnessFrequency.X, (int)m_shoreRoughnessOctaves.X, 2f, 0.6f);
                x3 = m_shoreRoughnessAmplitude.X
                    * SimplexNoise.OctavedNoise(z + m_shoreRoughnessOffset[1], m_shoreRoughnessFrequency.X, (int)m_shoreRoughnessOctaves.X, 2f, 0.6f);
            }
            if (m_shoreRoughnessAmplitude.Y > 0f) {
                y = m_shoreRoughnessAmplitude.Y
                    * SimplexNoise.OctavedNoise(x + m_shoreRoughnessOffset[2], m_shoreRoughnessFrequency.Y, (int)m_shoreRoughnessOctaves.Y, 2f, 0.6f);
                y2 = m_shoreRoughnessAmplitude.Y
                    * SimplexNoise.OctavedNoise(x + m_shoreRoughnessOffset[3], m_shoreRoughnessFrequency.Y, (int)m_shoreRoughnessOctaves.Y, 2f, 0.6f);
            }
            Vector2 vector = m_oceanCorner + new Vector2(x2, y);
            Vector2 vector2 = m_oceanCorner + (m_islandSize ?? new Vector2(3.40282347E+38f)) + new Vector2(x3, y2);
            return MathUtils.Min(x - vector.X, vector2.X - x, z - vector.Y, vector2.Y - z);
        }

        public float CalculateHeight(float x, float z) => m_worldSettings.TerrainLevel;

        public int CalculateTemperature(float x, float z) => Math.Clamp(12 + (int)m_worldSettings.TemperatureOffset, 0, 15);

        public int CalculateHumidity(float x, float z) => Math.Clamp(12 + (int)m_worldSettings.HumidityOffset, 0, 15);

        public float CalculateMountainRangeFactor(float x, float z) => 0f;

        public virtual void UpdateFluidIsTop(TerrainChunk chunk) {
            _ = m_subsystemTerrain.Terrain;
            for (int i = 0; i < TerrainChunk.Size; i++) {
                for (int j = 0; j < TerrainChunk.Size; j++) {
                    int num = TerrainChunk.CalculateCellIndex(i, TerrainChunk.HeightMinusOne, j);
                    int num2 = 0;
                    int num3 = TerrainChunk.HeightMinusOne;
                    while (num3 >= 0) {
                        int cellValueFast = chunk.GetCellValueFast(num);
                        int num4 = Terrain.ExtractContents(cellValueFast);
                        if (num4 != 0
                            && num4 != num2
                            && BlocksManager.Blocks[num4] is FluidBlock) {
                            int data = Terrain.ExtractData(cellValueFast);
                            chunk.SetCellValueFast(num, Terrain.MakeBlockValue(num4, 0, FluidBlock.SetIsTop(data, true)));
                        }
                        num2 = num4;
                        num3--;
                        num--;
                    }
                }
            }
        }
    }
}