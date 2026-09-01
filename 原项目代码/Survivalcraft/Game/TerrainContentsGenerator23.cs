using Engine;

namespace Game {
    public class TerrainContentsGenerator23 : ITerrainContentsGenerator {
        public class CavePoint {
            public Vector3 Position;

            public Vector3 Direction;

            public int BrushType;

            public int Length;

            public int StepsTaken;
        }

        public class Grid2d {
            public int m_sizeX;

            public int m_sizeY;

            public float[] m_data;

            public int SizeX => m_sizeX;

            public int SizeY => m_sizeY;

            public Grid2d(int sizeX, int sizeY) {
                m_sizeX = sizeX;
                m_sizeY = sizeY;
                m_data = new float[m_sizeX * m_sizeY];
            }

            public float Get(int x, int y) => m_data[x + y * m_sizeX];

            public void Set(int x, int y, float value) {
                m_data[x + y * m_sizeX] = value;
            }

            public float Sample(float x, float y) {
                int num = (int)MathF.Floor(x);
                int num2 = (int)MathF.Floor(y);
                int num3 = (int)MathF.Ceiling(x);
                int num4 = (int)MathF.Ceiling(y);
                float f = x - num;
                float f2 = y - num2;
                float x2 = m_data[num + num2 * m_sizeX];
                float x3 = m_data[num3 + num2 * m_sizeX];
                float x4 = m_data[num + num4 * m_sizeX];
                float x5 = m_data[num3 + num4 * m_sizeX];
                float x6 = MathUtils.Lerp(x2, x3, f);
                float x7 = MathUtils.Lerp(x4, x5, f);
                return MathUtils.Lerp(x6, x7, f2);
            }
        }

        public class Grid3d {
            public int m_sizeX;

            public int m_sizeY;

            public int m_sizeZ;

            public int m_sizeXY;

            public float[] m_data;

            public int SizeX => m_sizeX;

            public int SizeY => m_sizeY;

            public int SizeZ => m_sizeZ;

            public Grid3d(int sizeX, int sizeY, int sizeZ) {
                m_sizeX = sizeX;
                m_sizeY = sizeY;
                m_sizeZ = sizeZ;
                m_sizeXY = m_sizeX * m_sizeY;
                m_data = new float[m_sizeX * m_sizeY * m_sizeZ];
            }

            public void Get8(int x,
                int y,
                int z,
                out float v111,
                out float v211,
                out float v121,
                out float v221,
                out float v112,
                out float v212,
                out float v122,
                out float v222) {
                int num = x + y * m_sizeX + z * m_sizeXY;
                v111 = m_data[num];
                v211 = m_data[num + 1];
                v121 = m_data[num + m_sizeX];
                v221 = m_data[num + 1 + m_sizeX];
                v112 = m_data[num + m_sizeXY];
                v212 = m_data[num + 1 + m_sizeXY];
                v122 = m_data[num + m_sizeX + m_sizeXY];
                v222 = m_data[num + 1 + m_sizeX + m_sizeXY];
            }

            public float Get(int x, int y, int z) => m_data[x + y * m_sizeX + z * m_sizeXY];

            public void Set(int x, int y, int z, float value) {
                m_data[x + y * m_sizeX + z * m_sizeXY] = value;
            }

            public float Sample(float x, float y, float z) {
                int num = (int)MathF.Floor(x);
                int num2 = (int)MathF.Ceiling(x);
                int num3 = (int)MathF.Floor(y);
                int num4 = (int)MathF.Ceiling(y);
                int num5 = (int)MathF.Floor(z);
                int num6 = (int)MathF.Ceiling(z);
                float f = x - num;
                float f2 = y - num3;
                float f3 = z - num5;
                float x2 = m_data[num + num3 * m_sizeX + num5 * m_sizeX * m_sizeY];
                float x3 = m_data[num2 + num3 * m_sizeX + num5 * m_sizeX * m_sizeY];
                float x4 = m_data[num + num4 * m_sizeX + num5 * m_sizeX * m_sizeY];
                float x5 = m_data[num2 + num4 * m_sizeX + num5 * m_sizeX * m_sizeY];
                float x6 = m_data[num + num3 * m_sizeX + num6 * m_sizeX * m_sizeY];
                float x7 = m_data[num2 + num3 * m_sizeX + num6 * m_sizeX * m_sizeY];
                float x8 = m_data[num + num4 * m_sizeX + num6 * m_sizeX * m_sizeY];
                float x9 = m_data[num2 + num4 * m_sizeX + num6 * m_sizeX * m_sizeY];
                float x10 = MathUtils.Lerp(x2, x3, f);
                float x11 = MathUtils.Lerp(x4, x5, f);
                float x12 = MathUtils.Lerp(x6, x7, f);
                float x13 = MathUtils.Lerp(x8, x9, f);
                float x14 = MathUtils.Lerp(x10, x11, f2);
                float x15 = MathUtils.Lerp(x12, x13, f2);
                return MathUtils.Lerp(x14, x15, f3);
            }
        }

        public static List<TerrainBrush> m_coalBrushes;

        public static List<TerrainBrush> m_ironBrushes;

        public static List<TerrainBrush> m_copperBrushes;

        public static List<TerrainBrush> m_saltpeterBrushes;

        public static List<TerrainBrush> m_sulphurBrushes;

        public static List<TerrainBrush> m_diamondBrushes;

        public static List<TerrainBrush> m_germaniumBrushes;

        public static List<TerrainBrush> m_dirtPocketBrushes;

        public static List<TerrainBrush> m_gravelPocketBrushes;

        public static List<TerrainBrush> m_limestonePocketBrushes;

        public static List<TerrainBrush> m_sandPocketBrushes;

        public static List<TerrainBrush> m_basaltPocketBrushes;

        public static List<TerrainBrush> m_granitePocketBrushes;

        public static List<TerrainBrush> m_clayPocketBrushes;

        public static List<TerrainBrush> m_waterPocketBrushes;

        public static List<TerrainBrush> m_magmaPocketBrushes;

        public static List<List<TerrainBrush>> m_caveBrushesByType;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemBottomSuckerBlockBehavior m_subsystemBottomSuckerBlockBehavior;

        public WorldSettings m_worldSettings;

        public int m_seed;

        public Vector2? m_islandSize;

        public Vector2 m_oceanCorner;

        public Vector2 m_temperatureOffset;

        public Vector2 m_humidityOffset;

        public Vector2 m_mountainsOffset;

        public Vector2 m_riversOffset;

        public float TGBiomeScaling;

        public float TGShoreFluctuations;

        public float TGShoreFluctuationsScaling;

        public float TGOceanSlope;

        public float TGOceanSlopeVariation;

        public float TGIslandsFrequency;

        public float TGDensityBias;

        public float TGHeightBias;

        public float TGHillsPercentage;

        public float TGHillsStrength;

        public int TGHillsOctaves;

        public float TGHillsFrequency;

        public float TGHillsPersistence;

        public float TGMountainsStrength;

        public float TGMountainRangeFreq;

        public float TGMountainsPercentage;

        public static float TGMountainsDetailFreq;

        public static int TGMountainsDetailOctaves;

        public static float TGMountainsDetailPersistence;

        public float TGRiversStrength;

        public float TGTurbulenceStrength;

        public float TGTurbulenceFreq;

        public int TGTurbulenceOctaves;

        public float TGTurbulencePersistence;

        public float TGMinTurbulence;

        public float TGTurbulenceZero;

        public static float TGSurfaceMultiplier;

        public bool TGWater;

        public bool TGExtras;

        public bool TGCavesAndPockets;

        public int MaxBedrockHeight = 4;

        public int OceanLevel => 64 + m_worldSettings.SeaLevelOffset;
        public List<ChunkGenerationStep> ChunkGenerationStep1 = [];
        public List<ChunkGenerationStep> ChunkGenerationStep2 = [];
        public List<ChunkGenerationStep> ChunkGenerationStep3 = [];
        public List<ChunkGenerationStep> ChunkGenerationStep4 = [];

        static TerrainContentsGenerator23() {
            m_coalBrushes = [];
            m_ironBrushes = [];
            m_copperBrushes = [];
            m_saltpeterBrushes = [];
            m_sulphurBrushes = [];
            m_diamondBrushes = [];
            m_germaniumBrushes = [];
            m_dirtPocketBrushes = [];
            m_gravelPocketBrushes = [];
            m_limestonePocketBrushes = [];
            m_sandPocketBrushes = [];
            m_basaltPocketBrushes = [];
            m_granitePocketBrushes = [];
            m_clayPocketBrushes = [];
            m_waterPocketBrushes = [];
            m_magmaPocketBrushes = [];
            m_caveBrushesByType = [];
            CreateBrushes();
        }

        public TerrainContentsGenerator23(SubsystemTerrain subsystemTerrain) {
            m_subsystemTerrain = subsystemTerrain;
            m_subsystemBottomSuckerBlockBehavior = subsystemTerrain.Project.FindSubsystem<SubsystemBottomSuckerBlockBehavior>(true);
            SubsystemGameInfo subsystemGameInfo = subsystemTerrain.Project.FindSubsystem<SubsystemGameInfo>(true);
            m_worldSettings = subsystemGameInfo.WorldSettings;
            m_seed = subsystemGameInfo.WorldSeed;
            m_islandSize = m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.Island ? new Vector2?(m_worldSettings.IslandSize) : null;
            Random random = new(m_seed);
            float num = m_islandSize.HasValue ? MathUtils.Min(m_islandSize.Value.X, m_islandSize.Value.Y) : float.MaxValue;
            m_oceanCorner = new Vector2(-200f, -200f);
            m_temperatureOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
            m_humidityOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
            m_mountainsOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
            m_riversOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
            TGBiomeScaling = (m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.Island ? 1f : 1.75f) * m_worldSettings.BiomeSize;
            TGShoreFluctuations = Math.Clamp(2f * num, 0f, 150f);
            TGShoreFluctuationsScaling = Math.Clamp(0.04f * num, 0.5f, 3f);
            TGOceanSlope = 0.006f;
            TGOceanSlopeVariation = 0.004f;
            TGIslandsFrequency = 0.01f;
            TGDensityBias = 55f;
            TGHeightBias = 1f;
            TGRiversStrength = 1f;
            TGMountainsStrength = 220f;
            TGMountainRangeFreq = 0.0006f;
            TGMountainsPercentage = 0.13f;
            TGMountainsDetailFreq = 0.003f;
            TGMountainsDetailOctaves = 3;
            TGMountainsDetailPersistence = 0.53f;
            TGHillsPercentage = 0.3f;
            TGHillsStrength = 32f;
            TGHillsOctaves = 1;
            TGHillsFrequency = 0.014f;
            TGHillsPersistence = 0.5f;
            TGTurbulenceStrength = 55f;
            TGTurbulenceFreq = 0.03f;
            TGTurbulenceOctaves = 1;
            TGTurbulencePersistence = 0.5f;
            TGMinTurbulence = 0.04f;
            TGTurbulenceZero = 0.84f;
            TGSurfaceMultiplier = 2f;
            TGWater = true;
            TGExtras = true;
            TGCavesAndPockets = true;
            //Step 1
            ChunkGenerationStep1.Add(new ChunkGenerationStep(1000, chunk => GenerateSurfaceParameters(chunk, 0, 0, 16, 8)));
            ChunkGenerationStep1.Add(new ChunkGenerationStep(2000, chunk => GenerateTerrain(chunk, 0, 0, 16, 8)));
            //Step 2
            ChunkGenerationStep2.Add(new ChunkGenerationStep(1000, chunk => GenerateSurfaceParameters(chunk, 0, 8, 16, 16)));
            ChunkGenerationStep2.Add(new ChunkGenerationStep(2000, chunk => GenerateTerrain(chunk, 0, 8, 16, 16)));
            //Step 3
            ChunkGenerationStep3.Add(new ChunkGenerationStep(100, GenerateCaves));
            ChunkGenerationStep3.Add(new ChunkGenerationStep(200, GeneratePockets));
            ChunkGenerationStep3.Add(new ChunkGenerationStep(300, GenerateMinerals));
            ChunkGenerationStep3.Add(new ChunkGenerationStep(400, GenerateSurface));
            ChunkGenerationStep3.Add(new ChunkGenerationStep(500, PropagateFluidsDownwards));
            //step 4
            ChunkGenerationStep4.Add(new ChunkGenerationStep(100, GenerateGrassAndPlants));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(200, GenerateLogs));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(300, GenerateTrees));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(400, GenerateCacti));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(500, GeneratePumpkins));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(600, GenerateKelp));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(700, GenerateSeagrass));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(800, GenerateBottomSuckers));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(900, GenerateTraps));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(1000, GenerateIvy));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(1100, GenerateGraves));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(1200, GenerateCairns));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(1300, GenerateSnowAndIce));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(1400, GenerateBedrockAndAir));
            ChunkGenerationStep4.Add(new ChunkGenerationStep(1500, UpdateFluidIsTop));
            ModsManager.HookAction(
                "TerrainContentsGenerator23Initialize",
                loader => {
#pragma warning disable CS0618
                    loader.TerrainContentsGenerator23Initialize(this, subsystemTerrain);
#pragma warning restore CS0618
                    return false;
                }
            );
            ChunkGenerationStep1.Sort((a, b) => a.GenerateOrder.CompareTo(b.GenerateOrder));
            ChunkGenerationStep2.Sort((a, b) => a.GenerateOrder.CompareTo(b.GenerateOrder));
            ChunkGenerationStep3.Sort((a, b) => a.GenerateOrder.CompareTo(b.GenerateOrder));
            ChunkGenerationStep4.Sort((a, b) => a.GenerateOrder.CompareTo(b.GenerateOrder));
        }

        public Vector3 FindCoarseSpawnPosition() {
            Vector2 vector = Vector2.Zero;
            float num = float.MinValue;
            for (int i = 0; i < 1000; i += 5) {
                for (int j = 0; j <= 12; j += 4) {
                    for (int k = 0; k < 2; k++) {
                        float num2;
                        float x;
                        if (k == 0) {
                            num2 = m_oceanCorner.Y + i;
                            x = CalculateOceanShoreX(num2) + j;
                        }
                        else {
                            x = m_oceanCorner.X + i;
                            num2 = CalculateOceanShoreZ(x) + j;
                        }
                        float num3 = ScoreSpawnPosition(Terrain.ToCell(x), Terrain.ToCell(num2));
                        if (num3 > num) {
                            vector = new Vector2(x, num2);
                            num = num3;
                        }
                    }
                }
            }
            Vector3 ans = new(vector.X, CalculateHeight(vector.X, vector.Y), vector.Y);
            Log.Information("Survivalcraft FindCoarseSpawnPosition");
            ModsManager.HookAction(
                "FindCoarseSpawnPosition",
                loader => {
                    loader.FindCoarseSpawnPosition(this, ref ans);
                    return false;
                }
            );
            return ans;
        }

        public void GenerateChunkContentsPass1(TerrainChunk chunk) {
            foreach (ChunkGenerationStep step in ChunkGenerationStep1) {
                if (step.ShouldGenerate) {
                    step.GenerateAction(chunk);
                }
            }
        }

        public void GenerateChunkContentsPass2(TerrainChunk chunk) {
            foreach (ChunkGenerationStep step in ChunkGenerationStep2) {
                if (step.ShouldGenerate) {
                    step.GenerateAction(chunk);
                }
            }
        }

        public void GenerateChunkContentsPass3(TerrainChunk chunk) {
            foreach (ChunkGenerationStep step in ChunkGenerationStep3) {
                if (step.ShouldGenerate) {
                    step.GenerateAction(chunk);
                }
            }
        }

        public void GenerateChunkContentsPass4(TerrainChunk chunk) {
            foreach (ChunkGenerationStep step in ChunkGenerationStep4) {
                if (step.ShouldGenerate) {
                    step.GenerateAction(chunk);
                }
            }
        }

        public float CalculateOceanShoreDistance(float x, float z) {
            if (m_islandSize.HasValue) {
                float num = CalculateOceanShoreX(z);
                float num2 = CalculateOceanShoreZ(x);
                float num3 = CalculateOceanShoreX(z + 1000f) + m_islandSize.Value.X;
                float num4 = CalculateOceanShoreZ(x + 1000f) + m_islandSize.Value.Y;
                return MathUtils.Min(x - num, z - num2, num3 - x, num4 - z);
            }
            float num5 = CalculateOceanShoreX(z);
            float num6 = CalculateOceanShoreZ(x);
            return MathUtils.Min(x - num5, z - num6);
        }

        public float CalculateMountainRangeFactor(float x, float z) => SimplexNoise.OctavedNoise(
            x + m_mountainsOffset.X,
            z + m_mountainsOffset.Y,
            TGMountainRangeFreq / TGBiomeScaling,
            3,
            1.91f,
            0.75f,
            true
        );

        public float CalculateHeight(float x, float z) {
            float num = TGOceanSlope
                + TGOceanSlopeVariation
                * MathUtils.PowSign(2f * SimplexNoise.OctavedNoise(x + m_mountainsOffset.X, z + m_mountainsOffset.Y, 0.01f, 1, 2f, 0.5f) - 1f, 0.5f);
            float num2 = CalculateOceanShoreDistance(x, z);
            float num3 = MathUtils.Saturate(2f - 0.05f * MathF.Abs(num2));
            float num4 = MathUtils.Saturate(MathF.Sin(TGIslandsFrequency * num2));
            float num5 = MathUtils.Saturate(MathUtils.Saturate((0f - num) * num2) - 0.85f * num4);
            float num6 = MathUtils.Saturate(MathUtils.Saturate(0.05f * (0f - num2 - 10f)) - num4);
            float v = CalculateMountainRangeFactor(x, z);
            float f = (1f - num3) * SimplexNoise.OctavedNoise(x, z, 0.001f / TGBiomeScaling, 2, 2f, 0.5f);
            float f2 = (1f - num3) * SimplexNoise.OctavedNoise(x, z, 0.0017f / TGBiomeScaling, 2, 4f, 0.7f);
            float num7 = (1f - num6) * (1f - num3) * Squish(v, 1f - TGHillsPercentage, 1f - TGMountainsPercentage);
            float num8 = (1f - num6) * Squish(v, 1f - TGMountainsPercentage, 1f);
            float num9 = 1f * SimplexNoise.OctavedNoise(x, z, TGHillsFrequency, TGHillsOctaves, 1.93f, TGHillsPersistence);
            float amplitudeStep = MathUtils.Lerp(0.75f * TGMountainsDetailPersistence, 1.33f * TGMountainsDetailPersistence, f);
            float num10 = 1.5f * SimplexNoise.OctavedNoise(x, z, TGMountainsDetailFreq, TGMountainsDetailOctaves, 1.98f, amplitudeStep) - 0.5f;
            float num11 = MathUtils.Lerp(60f, 30f, MathUtils.Saturate(1f * num8 + 0.5f * num7 + MathUtils.Saturate(1f - num2 / 30f)));
            float x2 = MathUtils.Lerp(-2f, -4f, MathUtils.Saturate(num8 + 0.5f * num7));
            float num12 = MathUtils.Saturate(
                1.5f - num11 * MathF.Abs(2f * SimplexNoise.OctavedNoise(x + m_riversOffset.X, z + m_riversOffset.Y, 0.001f, 4, 2f, 0.5f) - 1f)
            );
            float num13 = -50f * num5 + TGHeightBias;
            float num14 = MathUtils.Lerp(0f, 8f, f);
            float num15 = MathUtils.Lerp(0f, -6f, f2);
            float num16 = TGHillsStrength * num7 * num9;
            float num17 = TGMountainsStrength * num8 * num10;
            float f3 = TGRiversStrength * num12;
            float num18 = num13 + num14 + num15 + num17 + num16;
            float num19 = MathUtils.Min(MathUtils.Lerp(num18, x2, f3), num18);
            return Math.Clamp(64f + num19, 10f, 251f);
        }

        public int CalculateTemperature(float x, float z) => Math.Clamp(
            (int)(MathUtils.Saturate(
                    3f * SimplexNoise.OctavedNoise(x + m_temperatureOffset.X, z + m_temperatureOffset.Y, 0.0015f / TGBiomeScaling, 5, 2f, 0.6f)
                    - 1.1f
                    + m_worldSettings.TemperatureOffset / 16f
                )
                * 16f),
            0,
            15
        );

        public int CalculateHumidity(float x, float z) => Math.Clamp(
            (int)(MathUtils.Saturate(
                    3f * SimplexNoise.OctavedNoise(x + m_humidityOffset.X, z + m_humidityOffset.Y, 0.0012f / TGBiomeScaling, 5, 2f, 0.6f)
                    - 0.9f
                    + m_worldSettings.HumidityOffset / 16f
                )
                * 16f),
            0,
            15
        );

        public static float Squish(float v, float zero, float one) => MathUtils.Saturate((v - zero) / (one - zero));

        public float CalculateOceanShoreX(float z) => m_oceanCorner.X
            + TGShoreFluctuations * SimplexNoise.OctavedNoise(z, 0f, 0.005f / TGShoreFluctuationsScaling, 4, 1.95f, 1f);

        public float CalculateOceanShoreZ(float x) => m_oceanCorner.Y
            + TGShoreFluctuations * SimplexNoise.OctavedNoise(0f, x, 0.005f / TGShoreFluctuationsScaling, 4, 1.95f, 1f);

        public float ScoreSpawnPosition(int x, int z) {
            int num = CalculateTemperature(x, z);
            int num2 = CalculateHumidity(x, z);
            float num3 = CalculateMountainRangeFactor(x, z);
            float num4 = CalculateHeight(x, z);
            float x2 = CalculateHeight(x - 5, z - 5);
            float x3 = CalculateHeight(x - 5, z + 5);
            float x4 = CalculateHeight(x + 5, z - 5);
            float x5 = CalculateHeight(x + 5, z + 5);
            float num5 = MathUtils.Min(num4, MathUtils.Max(x2, x3, x4, x5));
            float num6 = MathUtils.Max(num4, MathUtils.Max(x2, x3, x4, x5));
            float num7 = num6 - num5;
            float num8 = 0f;
            if (MathUtils.Max(MathF.Abs(x), MathF.Abs(z)) > 400) {
                num8 -= 0.25f;
            }
            if (num4 < 65f) {
                num8 -= 1f;
            }
            if (num4 < 66f) {
                num8 -= 0.5f;
            }
            if (num3 > 0.9f) {
                num8 -= 1f;
            }
            if (num3 > 0.85f) {
                num8 -= 0.5f;
            }
            if (num3 > 0.8f) {
                num8 -= 0.25f;
            }
            switch (m_subsystemTerrain.SubsystemGameInfo.WorldSettings.StartingPositionMode) {
                case StartingPositionMode.Easy:
                    if (num < 9) {
                        num8 -= 0.5f;
                    }
                    if (num < 7) {
                        num8 -= 1f;
                    }
                    if (num2 > 2
                        && num2 < 10) {
                        num8 -= 0.5f;
                    }
                    if (num6 > 75f) {
                        num8 -= 1f;
                    }
                    if (num7 > 5f) {
                        num8 -= 1f;
                    }
                    break;
                case StartingPositionMode.Medium:
                    if (num < 3) {
                        num8 -= 0.5f;
                    }
                    if (num > 6) {
                        num8 -= 1f;
                    }
                    if (num2 > 3
                        && num2 < 8) {
                        num8 -= 0.5f;
                    }
                    if (num2 > 10) {
                        num8 -= 0.5f;
                    }
                    if (num6 > 80f) {
                        num8 -= 1f;
                    }
                    if (num7 > 10f) {
                        num8 -= 1f;
                    }
                    break;
                default:
                    if (num > 0) {
                        num8 -= 0.5f;
                    }
                    if (num > 2) {
                        num8 -= 1f;
                    }
                    if (num2 > 6) {
                        num8 -= 0.5f;
                    }
                    if (num2 > 8) {
                        num8 -= 1f;
                    }
                    if (num6 > 85f) {
                        num8 -= 1f;
                    }
                    if (num7 > 15f) {
                        num8 -= 1f;
                    }
                    if (num7 < 5f) {
                        num8 -= 0.5f;
                    }
                    break;
            }
            return num8;
        }

        public void GenerateSurfaceParameters(TerrainChunk chunk, int x1, int z1, int x2, int z2) {
            for (int i = x1; i < x2; i++) {
                for (int j = z1; j < z2; j++) {
                    int num = i + chunk.Origin.X;
                    int num2 = j + chunk.Origin.Y;
                    int temperature = CalculateTemperature(num, num2);
                    int humidity = CalculateHumidity(num, num2);
                    chunk.SetTemperatureFast(i, j, temperature);
                    chunk.SetHumidityFast(i, j, humidity);
                }
            }
        }

        public void GenerateTerrain(TerrainChunk chunk, int x1, int z1, int x2, int z2) {
            int num = x2 - x1;
            int num2 = z2 - z1;
            _ = m_subsystemTerrain.Terrain;
            int num3 = chunk.Origin.X + x1;
            int num4 = chunk.Origin.Y + z1;
            Grid2d grid2d = new(num, num2);
            Grid2d grid2d2 = new(num, num2);
            for (int i = 0; i < num2; i++) {
                for (int j = 0; j < num; j++) {
                    grid2d.Set(j, i, CalculateOceanShoreDistance(j + num3, i + num4));
                    grid2d2.Set(j, i, CalculateMountainRangeFactor(j + num3, i + num4));
                }
            }
            Grid3d grid3d = new(num / 4 + 1, 33, num2 / 4 + 1);
            for (int k = 0; k < grid3d.SizeX; k++) {
                for (int l = 0; l < grid3d.SizeZ; l++) {
                    int num5 = k * 4 + num3;
                    int num6 = l * 4 + num4;
                    float num7 = CalculateHeight(num5, num6);
                    float v = CalculateMountainRangeFactor(num5, num6);
                    float num8 = MathUtils.Lerp(TGMinTurbulence, 1f, Squish(v, TGTurbulenceZero, 1f));
                    for (int m = 0; m < grid3d.SizeY; m++) {
                        int num9 = m * 8;
                        float num10 = TGTurbulenceStrength
                            * num8
                            * MathUtils.Saturate(num7 - num9)
                            * (2f
                                * SimplexNoise.OctavedNoise(
                                    num5,
                                    num9,
                                    num6,
                                    TGTurbulenceFreq,
                                    TGTurbulenceOctaves,
                                    4f,
                                    TGTurbulencePersistence
                                )
                                - 1f);
                        float num11 = num9 + num10;
                        float num12 = num7 - num11;
                        num12 += MathUtils.Max(4f * (TGDensityBias - num9), 0f);
                        grid3d.Set(k, m, l, num12);
                    }
                }
            }
            int oceanLevel = OceanLevel;
            for (int n = 0; n < grid3d.SizeX - 1; n++) {
                for (int num13 = 0; num13 < grid3d.SizeZ - 1; num13++) {
                    for (int num14 = 0; num14 < grid3d.SizeY - 1; num14++) {
                        grid3d.Get8(
                            n,
                            num14,
                            num13,
                            out float v2,
                            out float v3,
                            out float v4,
                            out float v5,
                            out float v6,
                            out float v7,
                            out float v8,
                            out float v9
                        );
                        float num15 = (v3 - v2) / 4f;
                        float num16 = (v5 - v4) / 4f;
                        float num17 = (v7 - v6) / 4f;
                        float num18 = (v9 - v8) / 4f;
                        float num19 = v2;
                        float num20 = v4;
                        float num21 = v6;
                        float num22 = v8;
                        for (int num23 = 0; num23 < 4; num23++) {
                            float num24 = (num21 - num19) / 4f;
                            float num25 = (num22 - num20) / 4f;
                            float num26 = num19;
                            float num27 = num20;
                            for (int num28 = 0; num28 < 4; num28++) {
                                float num29 = (num27 - num26) / 8f;
                                float num30 = num26;
                                int num31 = num23 + n * 4;
                                int num32 = num28 + num13 * 4;
                                int x3 = x1 + num31;
                                int z3 = z1 + num32;
                                float x4 = grid2d.Get(num31, num32);
                                float num33 = grid2d2.Get(num31, num32);
                                int temperatureFast = chunk.GetTemperatureFast(x3, z3);
                                int humidityFast = chunk.GetHumidityFast(x3, z3);
                                float f = num33 - 0.01f * humidityFast;
                                float num34 = MathUtils.Lerp(100f, 0f, f);
                                float num35 = MathUtils.Lerp(300f, 30f, f);
                                bool flag = (temperatureFast > 8 && humidityFast < 8 && num33 < 0.97f) || (MathF.Abs(x4) < 16f && num33 < 0.97f);
                                int num36 = TerrainChunk.CalculateCellIndex(x3, 0, z3);
                                for (int num37 = 0; num37 < 8; num37++) {
                                    int num38 = num37 + num14 * 8;
                                    int value = 0;
                                    if (num30 < 0f) {
                                        if (num38 <= oceanLevel) {
                                            value = 18;
                                        }
                                    }
                                    else {
                                        value = !flag ? !(num30 < num35) ? 67 : 3 :
                                            !(num30 < num34) ? !(num30 < num35) ? 67 : 3 : 4;
                                    }
                                    chunk.SetCellValueFast(num36 + num38, value);
                                    num30 += num29;
                                }
                                num26 += num24;
                                num27 += num25;
                            }
                            num19 += num15;
                            num20 += num16;
                            num21 += num17;
                            num22 += num18;
                        }
                    }
                }
            }
        }

        public void GenerateSurface(TerrainChunk chunk) {
            Terrain terrain = m_subsystemTerrain.Terrain;
            Random random = new(m_seed + chunk.Coords.X + 101 * chunk.Coords.Y);
            for (int i = 0; i < 16; i++) {
                for (int j = 0; j < 16; j++) {
                    int num = i + chunk.Origin.X;
                    int num2 = j + chunk.Origin.Y;
                    int num3 = TerrainChunk.CalculateCellIndex(i, 254, j);
                    int num4 = 254;
                    while (num4 >= 0) {
                        int cellValue = chunk.GetCellValueFast(num3);
                        int num5 = Terrain.ExtractContents(cellValue);
                        if (!BlocksManager.Blocks[num5].IsTransparent_(cellValue)) {
                            float num6 = CalculateMountainRangeFactor(num, num2);
                            int temperature = terrain.GetTemperature(num, num2);
                            int humidity = terrain.GetHumidity(num, num2);
                            int num7;
                            if (num5 == 4) {
                                num7 = temperature > 4 && temperature < 7 ? 6 : 7;
                            }
                            else {
                                int num8 = temperature / 4;
                                int num9 = num4 + 1 < 255 ? chunk.GetCellContentsFast(i, num4 + 1, j) : 0;
                                num7 = num4 > 120 && SubsystemWeather.IsPlaceFrozen(temperature, num4) ? 62 :
                                    (num4 < 66 || num4 == 84 + num8 || num4 == 103 + num8) && humidity == 9 && temperature % 6 == 1 ? 66 :
                                    num9 != 18 || humidity <= 8 || humidity % 2 != 0 || temperature % 3 != 0 ? 2 : 72;
                            }
                            int num10;
                            if (num7 == 62) {
                                num10 = (int)Math.Clamp(1f * -temperature, 1f, 7f);
                            }
                            else {
                                float num11 = MathUtils.Saturate((num4 - 100f) * 0.05f);
                                float f = MathUtils.Saturate(
                                    MathUtils.Saturate((num6 - 0.9f) / 0.1f) - MathUtils.Saturate((humidity - 3f) / 12f) + TGSurfaceMultiplier * num11
                                );
                                int min = (int)MathUtils.Lerp(4f, 0f, f);
                                int max = (int)MathUtils.Lerp(7f, 0f, f);
                                num10 = MathUtils.Min(random.Int(min, max), num4);
                            }
                            int num12 = TerrainChunk.CalculateCellIndex(i, num4 + 1, j);
                            for (int k = num12 - num10; k < num12; k++) {
                                if (Terrain.ExtractContents(chunk.GetCellValueFast(k)) != 0) {
                                    int value = Terrain.ReplaceContents(num7);
                                    chunk.SetCellValueFast(k, value);
                                }
                            }
                            break;
                        }
                        num4--;
                        num3--;
                    }
                }
            }
        }

        public void GenerateMinerals(TerrainChunk chunk) {
            if (!TGCavesAndPockets) {
                return;
            }
            int x = chunk.Coords.X;
            int y = chunk.Coords.Y;
            for (int i = x - 1; i <= x + 1; i++) {
                for (int j = y - 1; j <= y + 1; j++) {
                    Random random = new(m_seed + i + 119 * j);
                    int num = random.Int(0, 10);
                    for (int k = 0; k < num; k++) {
                        random.Int(0, 1);
                    }
                    float num2 = CalculateMountainRangeFactor(i * 16, j * 16);
                    int num3 = (int)(5f + 3f * num2 * SimplexNoise.OctavedNoise(i, j, 0.33f, 1, 1f, 1f));
                    for (int l = 0; l < num3; l++) {
                        int x2 = i * 16 + random.Int(0, 15);
                        int y2 = random.Int(5, 200);
                        int z = j * 16 + random.Int(0, 15);
                        m_coalBrushes[random.Int(0, m_coalBrushes.Count - 1)].PaintFastSelective(chunk, x2, y2, z, 3);
                    }
                    int num4 = (int)(6f + 2f * num2 * SimplexNoise.OctavedNoise(i + 1211, j + 396, 0.33f, 1, 1f, 1f));
                    for (int m = 0; m < num4; m++) {
                        int x3 = i * 16 + random.Int(0, 15);
                        int y3 = random.Int(20, 65);
                        int z2 = j * 16 + random.Int(0, 15);
                        m_copperBrushes[random.Int(0, m_copperBrushes.Count - 1)].PaintFastSelective(chunk, x3, y3, z2, 3);
                    }
                    int num5 = (int)(5f + 2f * num2 * SimplexNoise.OctavedNoise(i + 713, j + 211, 0.33f, 1, 1f, 1f));
                    for (int n = 0; n < num5; n++) {
                        int x4 = i * 16 + random.Int(0, 15);
                        int y4 = random.Int(2, 40);
                        int z3 = j * 16 + random.Int(0, 15);
                        m_ironBrushes[random.Int(0, m_ironBrushes.Count - 1)].PaintFastSelective(chunk, x4, y4, z3, 67);
                    }
                    int num6 = (int)(3f + 3f * num2 * SimplexNoise.OctavedNoise(i + 915, j + 272, 0.33f, 1, 1f, 1f));
                    for (int num7 = 0; num7 < num6; num7++) {
                        int x5 = i * 16 + random.Int(0, 15);
                        int y5 = random.Int(50, 90);
                        int z4 = j * 16 + random.Int(0, 15);
                        m_saltpeterBrushes[random.Int(0, m_saltpeterBrushes.Count - 1)].PaintFastSelective(chunk, x5, y5, z4, 4);
                    }
                    int num8 = (int)(3f + 2f * num2 * SimplexNoise.OctavedNoise(i + 711, j + 1194, 0.33f, 1, 1f, 1f));
                    for (int num9 = 0; num9 < num8; num9++) {
                        int x6 = i * 16 + random.Int(0, 15);
                        int y6 = random.Int(2, 40);
                        int z5 = j * 16 + random.Int(0, 15);
                        m_sulphurBrushes[random.Int(0, m_sulphurBrushes.Count - 1)].PaintFastSelective(chunk, x6, y6, z5, 67);
                    }
                    int num10 = (int)(0.5f + 2f * num2 * SimplexNoise.OctavedNoise(i + 432, j + 907, 0.33f, 1, 1f, 1f));
                    for (int num11 = 0; num11 < num10; num11++) {
                        int x7 = i * 16 + random.Int(0, 15);
                        int y7 = random.Int(2, 15);
                        int z6 = j * 16 + random.Int(0, 15);
                        m_diamondBrushes[random.Int(0, m_diamondBrushes.Count - 1)].PaintFastSelective(chunk, x7, y7, z6, 67);
                    }
                    int num12 = (int)(3f + 2f * num2 * SimplexNoise.OctavedNoise(i + 799, j + 131, 0.33f, 1, 1f, 1f));
                    for (int num13 = 0; num13 < num12; num13++) {
                        int x8 = i * 16 + random.Int(0, 15);
                        int y8 = random.Int(2, 50);
                        int z7 = j * 16 + random.Int(0, 15);
                        m_germaniumBrushes[random.Int(0, m_germaniumBrushes.Count - 1)].PaintFastSelective(chunk, x8, y8, z7, 67);
                    }
                }
            }
        }

        public void GeneratePockets(TerrainChunk chunk) {
            if (!TGCavesAndPockets) {
                return;
            }
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    int num = i + chunk.Coords.X;
                    int num2 = j + chunk.Coords.Y;
                    Random random = new(m_seed + num + 71 * num2);
                    int num3 = random.Int(0, 10);
                    for (int k = 0; k < num3; k++) {
                        random.Int(0, 1);
                    }
                    float num4 = CalculateMountainRangeFactor(num * 16, num2 * 16);
                    for (int l = 0; l < 3; l++) {
                        int x = num * 16 + random.Int(0, 15);
                        int y = random.Int(50, 100);
                        int z = num2 * 16 + random.Int(0, 15);
                        m_dirtPocketBrushes[random.Int(0, m_dirtPocketBrushes.Count - 1)].PaintFastSelective(chunk, x, y, z, 3);
                    }
                    for (int m = 0; m < 10; m++) {
                        int x2 = num * 16 + random.Int(0, 15);
                        int y2 = random.Int(20, 80);
                        int z2 = num2 * 16 + random.Int(0, 15);
                        m_gravelPocketBrushes[random.Int(0, m_gravelPocketBrushes.Count - 1)].PaintFastSelective(chunk, x2, y2, z2, 3);
                    }
                    for (int n = 0; n < 2; n++) {
                        int x3 = num * 16 + random.Int(0, 15);
                        int y3 = random.Int(20, 120);
                        int z3 = num2 * 16 + random.Int(0, 15);
                        m_limestonePocketBrushes[random.Int(0, m_limestonePocketBrushes.Count - 1)].PaintFastSelective(chunk, x3, y3, z3, 3);
                    }
                    for (int num5 = 0; num5 < 1; num5++) {
                        int x4 = num * 16 + random.Int(0, 15);
                        int y4 = random.Int(50, 70);
                        int z4 = num2 * 16 + random.Int(0, 15);
                        m_clayPocketBrushes[random.Int(0, m_clayPocketBrushes.Count - 1)].PaintFastSelective(chunk, x4, y4, z4, 3);
                    }
                    for (int num6 = 0; num6 < 6; num6++) {
                        int x5 = num * 16 + random.Int(0, 15);
                        int y5 = random.Int(40, 80);
                        int z5 = num2 * 16 + random.Int(0, 15);
                        m_sandPocketBrushes[random.Int(0, m_sandPocketBrushes.Count - 1)].PaintFastSelective(chunk, x5, y5, z5, 4);
                    }
                    for (int num7 = 0; num7 < 4; num7++) {
                        int x6 = num * 16 + random.Int(0, 15);
                        int y6 = random.Int(40, 60);
                        int z6 = num2 * 16 + random.Int(0, 15);
                        m_basaltPocketBrushes[random.Int(0, m_basaltPocketBrushes.Count - 1)].PaintFastSelective(chunk, x6, y6, z6, 4);
                    }
                    for (int num8 = 0; num8 < 3; num8++) {
                        int x7 = num * 16 + random.Int(0, 15);
                        int y7 = random.Int(20, 40);
                        int z7 = num2 * 16 + random.Int(0, 15);
                        m_basaltPocketBrushes[random.Int(0, m_basaltPocketBrushes.Count - 1)].PaintFastSelective(chunk, x7, y7, z7, 3);
                    }
                    for (int num9 = 0; num9 < 6; num9++) {
                        int x8 = num * 16 + random.Int(0, 15);
                        int y8 = random.Int(4, 50);
                        int z8 = num2 * 16 + random.Int(0, 15);
                        m_granitePocketBrushes[random.Int(0, m_granitePocketBrushes.Count - 1)].PaintFastSelective(chunk, x8, y8, z8, 67);
                    }
                    if (random.Bool(0.02f + 0.01f * num4)) {
                        int num10 = num * 16;
                        int num11 = random.Int(40, 60);
                        int num12 = num2 * 16;
                        int num13 = random.Int(1, 3);
                        for (int num14 = 0; num14 < num13; num14++) {
                            Vector2 vector = random.Vector2(7f);
                            int num15 = 8 + (int)MathF.Round(vector.X);
                            int num16 = 0;
                            int num17 = 8 + (int)MathF.Round(vector.Y);
                            m_waterPocketBrushes[random.Int(0, m_waterPocketBrushes.Count - 1)]
                                .PaintFast(chunk, num10 + num15, num11 + num16, num12 + num17);
                        }
                    }
                    if (random.Bool(0.06f + 0.05f * num4)) {
                        int num18 = num * 16;
                        int num19 = random.Int(15, 42);
                        int num20 = num2 * 16;
                        int num21 = random.Int(1, 2);
                        for (int num22 = 0; num22 < num21; num22++) {
                            Vector2 vector2 = random.Vector2(7f);
                            int num23 = 8 + (int)MathF.Round(vector2.X);
                            int num24 = random.Int(0, 1);
                            int num25 = 8 + (int)MathF.Round(vector2.Y);
                            m_magmaPocketBrushes[random.Int(0, m_magmaPocketBrushes.Count - 1)]
                                .PaintFast(chunk, num18 + num23, num19 + num24, num20 + num25);
                        }
                    }
                }
            }
        }

        public void GenerateCaves(TerrainChunk chunk) {
            if (!TGCavesAndPockets) {
                return;
            }
            List<CavePoint> list = [];
            int x = chunk.Coords.X;
            int y = chunk.Coords.Y;
            for (int i = x - 2; i <= x + 2; i++) {
                for (int j = y - 2; j <= y + 2; j++) {
                    list.Clear();
                    Random random = new(m_seed + i + 9973 * j);
                    int num = i * 16 + random.Int(0, 15);
                    int num2 = j * 16 + random.Int(0, 15);
                    float probability = 0.5f;
                    if (!random.Bool(probability)) {
                        continue;
                    }
                    int num3 = (int)CalculateHeight(num, num2);
                    int num4 = (int)CalculateHeight(num + 3, num2);
                    int num5 = (int)CalculateHeight(num, num2 + 3);
                    Vector3 position = new(num, num3 - 1, num2);
                    Vector3 v = new(3f, num4 - num3, 0f);
                    Vector3 v2 = new(0f, num5 - num3, 3f);
                    Vector3 direction = Vector3.Normalize(Vector3.Cross(v, v2));
                    if (direction.Y > -0.6f) {
                        list.Add(new CavePoint { Position = position, Direction = direction, BrushType = 0, Length = random.Int(80, 240) });
                    }
                    int num6 = i * 16 + 8;
                    int num7 = j * 16 + 8;
                    int num8 = 0;
                    while (num8 < list.Count) {
                        CavePoint cavePoint = list[num8];
                        List<TerrainBrush> list2 = m_caveBrushesByType[cavePoint.BrushType];
                        list2[random.Int(0, list2.Count - 1)]
                            .PaintFastAvoidWater(
                                chunk,
                                Terrain.ToCell(cavePoint.Position.X),
                                Terrain.ToCell(cavePoint.Position.Y),
                                Terrain.ToCell(cavePoint.Position.Z)
                            );
                        cavePoint.Position += 2f * cavePoint.Direction;
                        cavePoint.StepsTaken += 2;
                        float num9 = cavePoint.Position.X - num6;
                        float num10 = cavePoint.Position.Z - num7;
                        if (random.Bool(0.5f)) {
                            Vector3 vector = Vector3.Normalize(random.Vector3(1f));
                            if ((num9 < -25.5f && vector.X < 0f)
                                || (num9 > 25.5f && vector.X > 0f)) {
                                vector.X = 0f - vector.X;
                            }
                            if ((num10 < -25.5f && vector.Z < 0f)
                                || (num10 > 25.5f && vector.Z > 0f)) {
                                vector.Z = 0f - vector.Z;
                            }
                            if ((cavePoint.Direction.Y < -0.5f && vector.Y < -10f)
                                || (cavePoint.Direction.Y > 0.1f && vector.Y > 0f)) {
                                vector.Y = 0f - vector.Y;
                            }
                            cavePoint.Direction = Vector3.Normalize(cavePoint.Direction + 0.5f * vector);
                        }
                        if (cavePoint.StepsTaken > 20
                            && random.Bool(0.06f)) {
                            cavePoint.Direction = Vector3.Normalize(random.Vector3(1f) * new Vector3(1f, 0.33f, 1f));
                        }
                        if (cavePoint.StepsTaken > 20
                            && random.Bool(0.05f)) {
                            cavePoint.Direction.Y = 0f;
                            cavePoint.BrushType = MathUtils.Min(cavePoint.BrushType + 2, m_caveBrushesByType.Count - 1);
                        }
                        if (cavePoint.StepsTaken > 30
                            && random.Bool(0.03f)) {
                            cavePoint.Direction.X = 0f;
                            cavePoint.Direction.Y = -1f;
                            cavePoint.Direction.Z = 0f;
                        }
                        if (cavePoint.StepsTaken > 30
                            && cavePoint.Position.Y < 30f
                            && random.Bool(0.02f)) {
                            cavePoint.Direction.X = 0f;
                            cavePoint.Direction.Y = 1f;
                            cavePoint.Direction.Z = 0f;
                        }
                        if (random.Bool(0.33f)) {
                            cavePoint.BrushType = (int)(MathF.Pow(random.Float(0f, 0.999f), 7f) * m_caveBrushesByType.Count);
                        }
                        if (random.Bool(0.06f)
                            && list.Count < 12
                            && cavePoint.StepsTaken > 20
                            && cavePoint.Position.Y < 58f) {
                            list.Add(
                                new CavePoint {
                                    Position = cavePoint.Position,
                                    Direction = Vector3.Normalize(random.Vector3(1f, 1f) * new Vector3(1f, 0.33f, 1f)),
                                    BrushType = (int)(MathF.Pow(random.Float(0f, 0.999f), 7f) * m_caveBrushesByType.Count),
                                    Length = random.Int(40, 180)
                                }
                            );
                        }
                        if (cavePoint.StepsTaken >= cavePoint.Length
                            || MathF.Abs(num9) > 34f
                            || MathF.Abs(num10) > 34f
                            || cavePoint.Position.Y < 5f
                            || cavePoint.Position.Y > 246f) {
                            num8++;
                        }
                        else if (cavePoint.StepsTaken % 20 == 0) {
                            float num11 = CalculateHeight(cavePoint.Position.X, cavePoint.Position.Z);
                            if (cavePoint.Position.Y > num11 + 1f) {
                                num8++;
                            }
                        }
                    }
                }
            }
        }

        public void GenerateLogs(TerrainChunk chunk) {
            if (!TGExtras) {
                return;
            }
            Terrain terrain = m_subsystemTerrain.Terrain;
            int x = chunk.Origin.X;
            int num = x + 16;
            int y = chunk.Origin.Y;
            int num2 = y + 16;
            int x2 = chunk.Coords.X;
            int y2 = chunk.Coords.Y;
            for (int i = x2; i <= x2; i++) {
                for (int j = y2; j <= y2; j++) {
                    Random random = new(m_seed + i + 3943 * j);
                    int humidity = CalculateHumidity(i * 16, j * 16);
                    int num3 = CalculateTemperature(i * 16, j * 16);
                    float num4 = MathUtils.Saturate(
                        (SimplexNoise.OctavedNoise(i, j, 0.1f, 2, 2f, 0.5f) - 0.25f) / 0.2f + (random.Bool(0.25f) ? 0.5f : 0f)
                    );
                    int num5 = 0;
                    if (num4 > 0.9f) {
                        num5 = random.Int(1, 2);
                    }
                    else if (num4 > 0.6f) {
                        num5 = random.Int(0, 1);
                    }
                    int num6 = 0;
                    for (int k = 0; k < 12; k++) {
                        if (num6 >= num5) {
                            break;
                        }
                        int num7 = i * 16 + random.Int(0, 15);
                        int num8 = j * 16 + random.Int(0, 15);
                        int num9 = terrain.CalculateTopmostCellHeight(num7, num8);
                        if (num9 < 66) {
                            continue;
                        }
                        int cellContentsFast = terrain.GetCellContentsFast(num7, num9, num8);
                        if (cellContentsFast != 2
                            && cellContentsFast != 8) {
                            continue;
                        }
                        num9++;
                        int num10 = random.Int(3, 7);
                        Point3 point = CellFace.FaceToPoint3(random.Int(0, 3));
                        if (point.X < 0
                            && num7 - num10 + 1 < 0) {
                            point.X *= -1;
                        }
                        if (point.X > 0
                            && num7 + num10 - 1 > 15) {
                            point.X *= -1;
                        }
                        if (point.Z < 0
                            && num8 - num10 + 1 < 0) {
                            point.Z *= -1;
                        }
                        if (point.Z > 0
                            && num8 + num10 - 1 > 15) {
                            point.Z *= -1;
                        }
                        bool flag = true;
                        bool flag2 = false;
                        bool flag3 = false;
                        for (int l = 0; l < num10; l++) {
                            int num11 = num7 + point.X * l;
                            int num12 = num8 + point.Z * l;
                            if (num11 < x + 1
                                || num11 >= num - 1
                                || num12 < y + 1
                                || num12 >= num2 - 1) {
                                flag = false;
                                break;
                            }
                            if (BlocksManager.Blocks[terrain.GetCellContentsFast(num11, num9, num12)].IsCollidable) {
                                flag = false;
                                break;
                            }
                            if (BlocksManager.Blocks[terrain.GetCellContentsFast(num11, num9 - 1, num12)].IsCollidable) {
                                if (l <= MathUtils.Max(num10 / 2, 0)) {
                                    flag2 = true;
                                }
                                if (l >= MathUtils.Min(num10 / 2 + 1, num10 - 1)) {
                                    flag3 = true;
                                }
                            }
                        }
                        if (!(flag && flag2 && flag3)) {
                            continue;
                        }
                        Point3 point2 = point.X != 0 ? new Point3(0, 0, 1) : new Point3(1, 0, 0);
                        TreeType? treeType = PlantsManager.GenerateRandomTreeType(
                            random,
                            num3 + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num9),
                            humidity,
                            num9,
                            2f
                        );
                        if (treeType.HasValue) {
                            int treeTrunkValue = PlantsManager.GetTreeTrunkValue(treeType.Value);
                            treeTrunkValue = Terrain.ReplaceData(
                                treeTrunkValue,
                                WoodBlock.SetCutFace(Terrain.ExtractData(treeTrunkValue), point.X != 0 ? 1 : 0)
                            );
                            int treeLeavesValue = PlantsManager.GetTreeLeavesValue(treeType.Value);
                            for (int m = 0; m < num10; m++) {
                                int num13 = num7 + point.X * m;
                                int num14 = num8 + point.Z * m;
                                terrain.SetCellValueFast(num13, num9, num14, treeTrunkValue);
                                if (m > num10 / 2) {
                                    if (random.Bool(0.5f)
                                        && !BlocksManager.Blocks[terrain.GetCellContentsFast(num13 + point2.X, num9, num14 + point2.Z)]
                                            .IsCollidable) {
                                        terrain.SetCellValueFast(num13 + point2.X, num9, num14 + point2.Z, treeLeavesValue);
                                    }
                                    if (random.Bool(0.05f)
                                        && !BlocksManager.Blocks[terrain.GetCellContentsFast(num13 + point2.X, num9, num14 + point2.Z)]
                                            .IsCollidable) {
                                        terrain.SetCellValueFast(num13 + point2.X, num9, num14 + point2.Z, treeTrunkValue);
                                    }
                                    if (random.Bool(0.5f)
                                        && !BlocksManager.Blocks[terrain.GetCellContentsFast(num13 - point2.X, num9, num14 - point2.Z)]
                                            .IsCollidable) {
                                        terrain.SetCellValueFast(num13 - point2.X, num9, num14 - point2.Z, treeLeavesValue);
                                    }
                                    if (random.Bool(0.05f)
                                        && !BlocksManager.Blocks[terrain.GetCellContentsFast(num13 - point2.X, num9, num14 - point2.Z)]
                                            .IsCollidable) {
                                        terrain.SetCellValueFast(num13 - point2.X, num9, num14 - point2.Z, treeTrunkValue);
                                    }
                                    if (random.Bool(0.5f)
                                        && !BlocksManager.Blocks[terrain.GetCellContentsFast(num13, num9 + 1, num14)].IsCollidable) {
                                        terrain.SetCellValueFast(num13, num9 + 1, num14, treeLeavesValue);
                                    }
                                }
                            }
                        }
                        num6++;
                    }
                }
            }
        }

        public void GenerateTrees(TerrainChunk chunk) {
            if (!TGExtras) {
                return;
            }
            Terrain terrain = m_subsystemTerrain.Terrain;
            _ = chunk.Origin.X;
            _ = chunk.Origin.Y;
            int x = chunk.Coords.X;
            int y = chunk.Coords.Y;
            for (int i = x; i <= x; i++) {
                for (int j = y; j <= y; j++) {
                    Random random = new(m_seed + i + 3943 * j);
                    int humidity = CalculateHumidity(i * 16, j * 16);
                    int num = CalculateTemperature(i * 16, j * 16);
                    float num2 = MathUtils.Saturate(
                        (SimplexNoise.OctavedNoise(i, j, 0.1f, 2, 2f, 0.5f) - 0.25f) / 0.2f + (random.Bool(0.25f) ? 0.5f : 0f)
                    );
                    int num3 = (int)(5f * num2);
                    int num4 = 0;
                    for (int k = 0; k < 32; k++) {
                        if (num4 >= num3) {
                            break;
                        }
                        int num5 = i * 16 + random.Int(2, 13);
                        int num6 = j * 16 + random.Int(2, 13);
                        int num7 = terrain.CalculateTopmostCellHeight(num5, num6);
                        if (num7 < 66) {
                            continue;
                        }
                        int cellContentsFast = terrain.GetCellContentsFast(num5, num7, num6);
                        if (cellContentsFast != 2
                            && cellContentsFast != 8) {
                            continue;
                        }
                        num7++;
                        if (!BlocksManager.Blocks[terrain.GetCellContentsFast(num5 + 1, num7, num6)].IsCollidable
                            && !BlocksManager.Blocks[terrain.GetCellContentsFast(num5 - 1, num7, num6)].IsCollidable
                            && !BlocksManager.Blocks[terrain.GetCellContentsFast(num5, num7, num6 + 1)].IsCollidable
                            && !BlocksManager.Blocks[terrain.GetCellContentsFast(num5, num7, num6 - 1)].IsCollidable) {
                            TreeType? treeType = PlantsManager.GenerateRandomTreeType(
                                random,
                                num + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num7),
                                humidity,
                                num7
                            );
                            if (treeType.HasValue) {
                                ReadOnlyList<TerrainBrush> treeBrushes = PlantsManager.GetTreeBrushes(treeType.Value);
                                TerrainBrush terrainBrush = treeBrushes[random.Int(treeBrushes.Count)];
                                terrainBrush.PaintFast(chunk, num5, num7, num6);
                                chunk.AddBrushPaint(num5, num7, num6, terrainBrush);
                            }
                            num4++;
                        }
                    }
                }
            }
        }

        public void GenerateBedrockAndAir(TerrainChunk chunk) {
            int value = Terrain.MakeBlockValue(1);
            for (int i = 0; i < 16; i++) {
                for (int j = 0; j < 16; j++) {
                    int num = i + chunk.Origin.X;
                    int num2 = j + chunk.Origin.Y;
                    float num3 = 2 + (int)(MaxBedrockHeight * SimplexNoise.OctavedNoise(num, num2, 0.1f, 1, 1f, 1f));
                    for (int k = 0; k < num3; k++) {
                        chunk.SetCellValueFast(i, k, j, value);
                    }
                    chunk.SetCellValueFast(i, 255, j, 0);
                }
            }
        }

        public void GenerateGrassAndPlants(TerrainChunk chunk) {
            if (!TGExtras) {
                return;
            }
            Random random = new(m_seed + chunk.Coords.X + 3943 * chunk.Coords.Y);
            for (int i = 0; i < 16; i++) {
                for (int j = 0; j < 16; j++) {
                    for (int num = 254; num >= 0; num--) {
                        int cellValueFast = chunk.GetCellValueFast(i, num, j);
                        int num2 = Terrain.ExtractContents(cellValueFast);
                        if (num2 != 0) {
                            if (!(BlocksManager.Blocks[num2] is FluidBlock)) {
                                int temperatureFast = chunk.GetTemperatureFast(i, j);
                                int humidityFast = chunk.GetHumidityFast(i, j);
                                int num3 = PlantsManager.GenerateRandomPlantValue(random, cellValueFast, temperatureFast, humidityFast, num + 1);
                                if (num3 != 0) {
                                    chunk.SetCellValueFast(i, num + 1, j, num3);
                                }
                                if (num2 == 2) {
                                    chunk.SetCellValueFast(i, num, j, Terrain.MakeBlockValue(8, 0, 0));
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }

        public void GenerateBottomSuckers(TerrainChunk chunk) {
            if (!TGExtras) {
                return;
            }
            Random random = new(m_seed + chunk.Coords.X + 2210 * chunk.Coords.Y);
            for (int i = 0; i < 16; i++) {
                for (int j = 0; j < 16; j++) {
                    if (!random.Bool(0.2f)) {
                        continue;
                    }
                    int num = chunk.Origin.X + i;
                    int num2 = chunk.Origin.Y + j;
                    int temperatureFast = chunk.GetTemperatureFast(i, j);
                    if (CalculateOceanShoreDistance(num, num2) > 10f) {
                        continue;
                    }
                    int num3 = 0;
                    for (int num4 = 254; num4 >= 0; num4--) {
                        if (Terrain.ExtractContents(chunk.GetCellValueFast(i, num4, j)) == 18) {
                            num3++;
                            int face = random.Int(0, 5);
                            Point3 point = CellFace.FaceToPoint3(face);
                            if (i + point.X >= 0
                                && i + point.X < 16
                                && num4 + point.Y >= 0
                                && num4 + point.Y < 254
                                && j + point.Z >= 0
                                && j + point.Z < 16) {
                                int cellValueFast = chunk.GetCellValueFast(i + point.X, num4 + point.Y, j + point.Z);
                                if (m_subsystemBottomSuckerBlockBehavior.IsSupport(cellValueFast, CellFace.OppositeFace(face))) {
                                    int num5 = 0;
                                    float num6 = 0.6f;
                                    float num7 = 0.4f;
                                    if (temperatureFast < 8) {
                                        num6 = 0.9f;
                                        num7 = 0.1f;
                                    }
                                    if (num3 > 6) {
                                        num6 *= 0.25f;
                                    }
                                    if (num3 > 12) {
                                        num7 *= 0.5f;
                                    }
                                    if (num3 < 4) {
                                        num7 *= 0.5f;
                                    }
                                    if (num4 < 45) {
                                        num6 *= 0.1f;
                                        num7 *= 0.1f;
                                    }
                                    float num8 = random.Float(0f, 1f);
                                    num8 -= num6;
                                    if ( /*num5 == 0 && */num8 < 0f) {
                                        num5 = 226;
                                    }
                                    num8 -= num7;
                                    if (num5 == 0
                                        && num8 < 0f) {
                                        num5 = 229;
                                    }
                                    if (num5 != 0) {
                                        int face2 = random.Int(0, 3);
                                        int data = BottomSuckerBlock.SetFace(BottomSuckerBlock.SetSubvariant(0, face2), CellFace.OppositeFace(face));
                                        int value = Terrain.MakeBlockValue(num5, 0, data);
                                        chunk.SetCellValueFast(i, num4, j, value);
                                    }
                                }
                            }
                        }
                        else {
                            num3 = 0;
                        }
                    }
                }
            }
        }

        public void GenerateCacti(TerrainChunk chunk) {
            if (!TGExtras) {
                return;
            }
            int x = chunk.Coords.X;
            int y = chunk.Coords.Y;
            Random random = new(m_seed + x + 1991 * y);
            if (!random.Bool(0.5f)) {
                return;
            }
            int num = random.Int(0, MathUtils.Max(1, 1));
            for (int i = 0; i < num; i++) {
                int num2 = random.Int(3, 12);
                int num3 = random.Int(3, 12);
                int humidityFast = chunk.GetHumidityFast(num2, num3);
                int temperatureFast = chunk.GetTemperatureFast(num2, num3);
                if (humidityFast >= 6
                    || temperatureFast <= 8) {
                    continue;
                }
                for (int j = 0; j < 8; j++) {
                    int num4 = num2 + random.Int(-2, 2);
                    int num5 = num3 + random.Int(-2, 2);
                    for (int num6 = 251; num6 >= 0; num6--) {
                        switch (Terrain.ExtractContents(chunk.GetCellValueFast(num4, num6, num5))) {
                            case 7: {
                                for (int k = num6 + 1;
                                    k <= num6 + 3
                                    && chunk.GetCellContentsFast(num4 + 1, k, num5) == 0
                                    && chunk.GetCellContentsFast(num4 - 1, k, num5) == 0
                                    && chunk.GetCellContentsFast(num4, k, num5 + 1) == 0
                                    && chunk.GetCellContentsFast(num4, k, num5 - 1) == 0;
                                    k++) {
                                    chunk.SetCellValueFast(num4, k, num5, Terrain.MakeBlockValue(127));
                                }
                                break;
                            }
                            case 0: continue;
                        }
                        break;
                    }
                }
            }
        }

        public void GeneratePumpkins(TerrainChunk chunk) {
            if (!TGExtras) {
                return;
            }
            int x = chunk.Coords.X;
            int y = chunk.Coords.Y;
            Random random = new(m_seed + x + 1495 * y);
            if (!random.Bool(0.2f)) {
                return;
            }
            int num = random.Int(0, MathUtils.Max(1, 1));
            for (int i = 0; i < num; i++) {
                int num2 = random.Int(1, 14);
                int num3 = random.Int(1, 14);
                int humidityFast = chunk.GetHumidityFast(num2, num3);
                int temperatureFast = chunk.GetTemperatureFast(num2, num3);
                if (humidityFast < 10
                    || temperatureFast <= 6) {
                    continue;
                }
                for (int j = 0; j < 5; j++) {
                    int x2 = num2 + random.Int(-1, 1);
                    int z = num3 + random.Int(-1, 1);
                    for (int num4 = 254; num4 >= 0; num4--) {
                        switch (Terrain.ExtractContents(chunk.GetCellValueFast(x2, num4, z))) {
                            case 8:
                                chunk.SetCellValueFast(
                                    x2,
                                    num4 + 1,
                                    z,
                                    random.Bool(0.25f) ? Terrain.MakeBlockValue(244) : Terrain.MakeBlockValue(131)
                                ); break;
                            case 0: continue;
                        }
                        break;
                    }
                }
            }
        }

        public void GenerateKelp(TerrainChunk chunk) {
            if (!TGExtras) {
                return;
            }
            int x = chunk.Coords.X;
            int y = chunk.Coords.Y;
            Random random = new(0);
            float num = 0f;
            for (int i = 0; i < 9; i++) {
                int num2 = i % 3 - 1;
                int num3 = i / 3 - 1;
                random.Seed(m_seed + x + num2 + 850 * (y + num3));
                if (random.Bool(0.2f)) {
                    num = MathUtils.Max(num, 0.025f);
                    if (i == 4) {
                        num = MathUtils.Max(num, 0.1f);
                    }
                }
            }
            if (num == 0f) {
                return;
            }
            random.Seed(m_seed + x + 850 * y);
            int num4 = random.Int(0, MathUtils.Max((int)(256f * num), 1));
            for (int j = 0; j < num4; j++) {
                int num5 = random.Int(2, 13);
                int num6 = random.Int(2, 13);
                int num7 = num5 + chunk.Origin.X;
                int num8 = num6 + chunk.Origin.Y;
                int num9 = random.Int(10, 26);
                int num10 = 6;
                bool flag = true;
                if (CalculateOceanShoreDistance(num7, num8) > 5f) {
                    num10 = 4;
                    flag = false;
                }
                if (num9 <= 0) {
                    continue;
                }
                for (int k = 0; k < num9; k++) {
                    int x2 = num5 + random.Int(-2, 2);
                    int z = num6 + random.Int(-2, 2);
                    int num11 = 0;
                    for (int num12 = 254; num12 >= 0; num12--) {
                        int num13 = Terrain.ExtractContents(chunk.GetCellValueFast(x2, num12, z));
                        Block block = BlocksManager.Blocks[num13];
                        if (num13 != 0) {
                            if (!(block is WaterBlock)) {
                                if ((num13 == 2 || num13 == 7 || num13 == 72)
                                    && num11 >= 2) {
                                    int num14 = flag ? random.Int(num11 - 2, num11 - 1) : random.Int(num11 - 1, num11);
                                    for (int l = 0; l < num14; l++) {
                                        chunk.SetCellValueFast(x2, num12 + 1 + l, z, Terrain.MakeBlockValue(232));
                                    }
                                }
                                break;
                            }
                            num11++;
                            if (num11 > num10) {
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void GenerateSeagrass(TerrainChunk chunk) {
            if (!TGExtras) {
                return;
            }
            int x = chunk.Coords.X;
            int y = chunk.Coords.Y;
            Random random = new(m_seed + x + 378 * y);
            for (int i = 0; i < 6; i++) {
                int num = random.Int(1, 14);
                int num2 = random.Int(1, 14);
                int num3 = chunk.Origin.X + num;
                int num4 = chunk.Origin.Y + num2;
                bool flag = CalculateOceanShoreDistance(num3, num4) < 10f;
                int num5 = random.Int(1, 3);
                for (int j = 0; j < num5; j++) {
                    int x2 = num + random.Int(-1, 1);
                    int z = num2 + random.Int(-1, 1);
                    int num6 = 0;
                    for (int num7 = 254; num7 >= 0; num7--) {
                        int num8 = Terrain.ExtractContents(chunk.GetCellValueFast(x2, num7, z));
                        switch (num8) {
                            case 18:
                                num6++;
                                if (num6 <= 16) {
                                    continue;
                                }
                                break;
                            default:
                                if (num6 > 1
                                    && (num8 == 2 || num8 == 7 || num8 == 72 || num8 == 3)) {
                                    int x3 = !random.Bool(0.1f) ? 1 : 2;
                                    x3 = flag ? MathUtils.Min(x3, num6 - 1) : MathUtils.Min(x3, num6);
                                    for (int k = 0; k < x3; k++) {
                                        chunk.SetCellValueFast(x2, num7 + 1 + k, z, Terrain.MakeBlockValue(233));
                                    }
                                }
                                break;
                            case 0: continue;
                        }
                        break;
                    }
                }
            }
        }

        public void GenerateIvy(TerrainChunk chunk) {
            if (!TGExtras) {
                return;
            }
            Random random = new(m_seed + chunk.Coords.X + 2191 * chunk.Coords.Y);
            int num = random.Int(0, MathUtils.Max(12, 1));
            for (int i = 0; i < num; i++) {
                int num2 = random.Int(4, 11);
                int num3 = random.Int(4, 11);
                int humidityFast = chunk.GetHumidityFast(num2, num3);
                int temperatureFast = chunk.GetTemperatureFast(num2, num3);
                if (humidityFast < 10
                    || temperatureFast < 10) {
                    continue;
                }
                int num4 = chunk.CalculateTopmostCellHeight(num2, num3);
                for (int j = 0; j < 100; j++) {
                    int num5 = num2 + random.Int(-3, 3);
                    int num6 = Math.Clamp(num4 + random.Int(-12, 1), 1, 255);
                    int num7 = num3 + random.Int(-3, 3);
                    switch (Terrain.ExtractContents(chunk.GetCellValueFast(num5, num6, num7))) {
                        case 2:
                        case 3:
                        case 8:
                        case 9:
                        case 12:
                        case 66:
                        case 67: {
                            int num8 = random.Int(0, 3);
                            for (int k = 0; k < 4; k++) {
                                int face = (k + num8) % 4;
                                Point3 point = CellFace.FaceToPoint3(face);
                                if (chunk.GetCellContentsFast(num5 + point.X, num6, num7 + point.Z) != 0) {
                                    continue;
                                }
                                int num9 = num6 - 1;
                                while (num9 >= 1
                                    && chunk.GetCellContentsFast(num5 + point.X, num9, num7 + point.Z) == 0
                                    && chunk.GetCellContentsFast(num5, num9, num7) != 0) {
                                    num9--;
                                }
                                if (chunk.GetCellContentsFast(num5 + point.X, num9, num7 + point.Z) != 0) {
                                    break;
                                }
                                num9++;
                                int value = Terrain.MakeBlockValue(197, 0, IvyBlock.SetFace(0, CellFace.OppositeFace(face)));
                                while (num9 >= 1
                                    && chunk.GetCellContentsFast(num5 + point.X, num9, num7 + point.Z) == 0) {
                                    chunk.SetCellValueFast(num5 + point.X, num9, num7 + point.Z, value);
                                    if (IvyBlock.IsGrowthStopCell(num5 + point.X, num9, num7 + point.Z)) {
                                        break;
                                    }
                                    num9--;
                                }
                                break;
                            }
                            break;
                        }
                    }
                }
            }
        }

        public void GenerateTraps(TerrainChunk chunk) {
            if (!TGExtras) {
                return;
            }
            int x = chunk.Coords.X;
            int y = chunk.Coords.Y;
            _ = m_subsystemTerrain.Terrain;
            Random random = new(m_seed + x + 2113 * y);
            if (!random.Bool(0.15f)
                || !(CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) > 50f)) {
                return;
            }
            int num = random.Int(0, MathUtils.Max(2, 1));
            for (int i = 0; i < num; i++) {
                int num2 = random.Int(2, 5);
                int num3 = random.Int(2, 5);
                int num4 = random.Int(1, 16 - num2 - 2);
                int num5 = random.Int(1, 16 - num3 - 2);
                bool flag = random.Float(0f, 1f) < 0.5f;
                int num6 = random.Int(3, 5);
                int? num7 = null;
                int num8 = num4 - 1;
                while (true) {
                    if (num8 < num4 + num2 + 1) {
                        for (int j = num5 - 1; j < num5 + num3 + 1; j++) {
                            int num9 = chunk.CalculateTopmostCellHeight(num8, j);
                            int num10 = MathUtils.Max(num9 - 20, 5);
                            while (num9 >= num10
                                && chunk.GetCellContentsFast(num8, num9, j) != 8) {
                                num9--;
                            }
                            if (num7.HasValue
                                && num7 != num9) {
                                goto end_IL_01a0;
                            }
                            num7 = num9;
                            if (chunk.GetCellContentsFast(num8, num9, j) != 8) {
                                goto end_IL_01a0;
                            }
                        }
                        num8++;
                        continue;
                    }
                    if (!num7.HasValue
                        || num7 - num6 < 5) {
                        break;
                    }
                    for (int k = num4; k < num4 + num2; k++) {
                        for (int l = num5; l < num5 + num3; l++) {
                            for (int num11 = num7.Value - 1; num11 >= num7 - num6 + 1; num11--) {
                                chunk.SetCellValueFast(k, num11, l, Terrain.MakeBlockValue(0));
                            }
                            chunk.SetCellValueFast(k, num7.Value, l, Terrain.MakeBlockValue(87));
                            if (flag) {
                                int data = SpikedPlankBlock.SetSpikesState(0, random.Float(0f, 1f) < 0.33f);
                                chunk.SetCellValueFast(k, num7.Value - num6 + 1, l, Terrain.MakeBlockValue(86, 0, data));
                            }
                        }
                    }
                    break;
                    end_IL_01a0:
                    break;
                }
            }
        }

        public void GenerateGraves(TerrainChunk chunk) {
            if (!TGExtras) {
                return;
            }
            int x = chunk.Coords.X;
            int y = chunk.Coords.Y;
            Random random = new((int)MathUtils.Hash((uint)(m_seed + x + 10323 * y)));
            if (!(random.Float(0f, 1f) < 0.033f)
                || !(CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) > 10f)) {
                return;
            }
            int num = random.Int(0, MathUtils.Max(1, 1));
            for (int i = 0; i < num; i++) {
                int num2 = random.Int(6, 9);
                int num3 = random.Int(6, 9);
                int num4 = random.Bool(0.2f) ? random.Int(6, 20) : random.Int(1, 5);
                bool flag = random.Bool(0.5f);
                for (int j = 0; j < num4; j++) {
                    int num5 = num2 + random.Int(-4, 4);
                    int num6 = num3 + random.Int(-4, 4);
                    int num7 = chunk.CalculateTopmostCellHeight(num5, num6);
                    if (num7 < 10
                        || num7 > 246) {
                        continue;
                    }
                    int num8 = random.Int(0, 3);
                    for (int k = 0; k < 4; k++) {
                        int num9 = (k + num8) % 4;
                        Point3 p = CellFace.FaceToPoint3(num9);
                        Point3 p2 = new(-p.Z, p.Y, p.X);
                        int num10 = p.X < 0 ? num5 - 2 : num5 - 1;
                        int num11 = p.X > 0 ? num5 + 2 : num5 + 1;
                        int num12 = p.Z < 0 ? num6 - 2 : num6 - 1;
                        int num13 = p.Z > 0 ? num6 + 2 : num6 + 1;
                        for (int l = num10; l <= num11; l++) {
                            for (int m = num7 - 2; m <= num7 + 2; m++) {
                                for (int n = num12; n <= num13; n++) {
                                    int num14 = Terrain.ExtractContents(chunk.GetCellValueFast(l, m, n));
                                    Block block = BlocksManager.Blocks[num14];
                                    if (m > num7) {
                                        if (!block.IsCollidable) {
                                            continue;
                                        }
                                    }
                                    else if (num14 == 8
                                        || num14 == 2
                                        || num14 == 7
                                        || num14 == 3
                                        || num14 == 4) {
                                        continue;
                                    }
                                    goto IL_06ac;
                                }
                            }
                        }
                        int num15 = random.Int(0, 7);
                        int data = GravestoneBlock.SetVariant(GravestoneBlock.SetRotation(0, num9 % 2), num15);
                        int? num16 = null;
                        int contents = 217;
                        int contents2 = 136;
                        if (num15 >= 4
                            && !flag) {
                            int cellContentsFast = chunk.GetCellContentsFast(num5, num7, num6);
                            if (cellContentsFast == 7
                                || cellContentsFast == 4) {
                                num16 = Terrain.MakeBlockValue(4);
                                contents = 51;
                                contents2 = 52;
                            }
                            else if (random.Float(0f, 1f) < 0.5f) {
                                num16 = Terrain.MakeBlockValue(3);
                                contents = 217;
                                contents2 = 136;
                            }
                            else {
                                num16 = Terrain.MakeBlockValue(67);
                                contents = 96;
                                contents2 = 95;
                            }
                        }
                        bool flag2 = num16.HasValue && random.Bool(0.33f);
                        float num17 = random.Float(0f, 1f);
                        float num18 = random.Float(0f, 1f);
                        int num19 = random.Int(-1, 0);
                        int num20 = random.Int(1, 2);
                        int num21 = flag2 ? num7 + 2 : num7 + 1;
                        chunk.SetCellValueFast(num5, num21, num6, Terrain.MakeBlockValue(189, 0, data));
                        for (int num22 = num19; num22 <= num20; num22++) {
                            int num23 = num5 + p.X * num22;
                            int num24 = num6 + p.Z * num22;
                            if (num22 == 0
                                || num22 == 1) {
                                chunk.SetCellValueFast(num23, num21 - 2, num24, Terrain.MakeBlockValue(190));
                                if (num16.HasValue) {
                                    chunk.SetCellValueFast(num23, num21 - 1, num24, num16.Value);
                                    if (num22 == 1) {
                                        int num25 = 0;
                                        if (num18 < 0.2f) {
                                            num25 = Terrain.MakeBlockValue(20);
                                        }
                                        else if (num18 < 0.3f) {
                                            num25 = Terrain.MakeBlockValue(24);
                                        }
                                        else if (num18 < 0.4f) {
                                            num25 = Terrain.MakeBlockValue(25);
                                        }
                                        else if (num18 < 0.5f) {
                                            num25 = Terrain.MakeBlockValue(31, 0, 4);
                                        }
                                        else if (num18 < 0.6f) {
                                            num25 = Terrain.MakeBlockValue(132, 0, CellFace.OppositeFace(num9));
                                        }
                                        if (num25 != 0) {
                                            chunk.SetCellValueFast(num23, num21, num24, num25);
                                        }
                                    }
                                }
                            }
                            if (!flag2) {
                                continue;
                            }
                            if (num17 < 0.3f) {
                                int value = Terrain.MakeBlockValue(contents, 0, StairsBlock.SetRotation(0, CellFace.Point3ToFace(p2)));
                                int value2 = Terrain.MakeBlockValue(
                                    contents,
                                    0,
                                    StairsBlock.SetRotation(0, CellFace.OppositeFace(CellFace.Point3ToFace(p2)))
                                );
                                chunk.SetCellValueFast(num23 + p2.X, num21 - 1, num24 + p2.Z, value);
                                chunk.SetCellValueFast(num23 - p2.X, num21 - 1, num24 - p2.Z, value2);
                                if (num22 == -1) {
                                    int value3 = Terrain.MakeBlockValue(
                                        contents,
                                        0,
                                        StairsBlock.SetRotation(0, CellFace.OppositeFace(CellFace.Point3ToFace(p)))
                                    );
                                    chunk.SetCellValueFast(num23, num21 - 1, num24, value3);
                                }
                                if (num22 == 2) {
                                    int value4 = Terrain.MakeBlockValue(contents, 0, StairsBlock.SetRotation(0, CellFace.Point3ToFace(p)));
                                    chunk.SetCellValueFast(num23, num21 - 1, num24, value4);
                                }
                            }
                            else if (num17 < 0.4f) {
                                chunk.SetCellValueFast(num23 + p2.X, num21 - 1, num24 + p2.Z, Terrain.MakeBlockValue(contents2));
                                chunk.SetCellValueFast(num23 - p2.X, num21 - 1, num24 - p2.Z, Terrain.MakeBlockValue(contents2));
                                if (num22 == -1) {
                                    chunk.SetCellValueFast(num23, num21 - 1, num24, Terrain.MakeBlockValue(contents2));
                                }
                                if (num22 == 2) {
                                    chunk.SetCellValueFast(num23, num21 - 1, num24, Terrain.MakeBlockValue(contents2));
                                }
                            }
                            else if (num17 < 0.6f) {
                                if (num22 == 0
                                    || num22 == 1) {
                                    chunk.SetCellValueFast(
                                        num23 + p2.X,
                                        num21 - 1,
                                        num24 + p2.Z,
                                        Terrain.MakeBlockValue(31, 0, CellFace.Point3ToFace(p2))
                                    );
                                    chunk.SetCellValueFast(
                                        num23 - p2.X,
                                        num21 - 1,
                                        num24 - p2.Z,
                                        Terrain.MakeBlockValue(31, 0, CellFace.OppositeFace(CellFace.Point3ToFace(p2)))
                                    );
                                }
                                if (num22 == -1) {
                                    chunk.SetCellValueFast(num23, num21 - 1, num24, Terrain.MakeBlockValue(31, 0, CellFace.OppositeFace(num9)));
                                }
                                if (num22 == 2) {
                                    chunk.SetCellValueFast(num23, num21 - 1, num24, Terrain.MakeBlockValue(31, 0, num9));
                                }
                            }
                        }
                        break;
                        IL_06ac: ;
                    }
                }
            }
        }

        public void GenerateCairns(TerrainChunk chunk) {
            int num = 190;
            Point2 point = default;
            for (int i = 0; i < 16; i++) {
                for (int j = 0; j < 16; j++) {
                    if (Terrain.ExtractContents(chunk.GetCellValueFast(i, num, j)) == 0) {
                        continue;
                    }
                    for (int k = num + 1; k < 256; k++) {
                        if (Terrain.ExtractContents(chunk.GetCellValueFast(i, k, j)) == 0) {
                            num = k;
                            point = new Point2(i, j);
                            break;
                        }
                    }
                }
            }
            if (num >= 190 /*&& num <= 255 */
                && point.X >= 1
                && point.X < 15
                && point.Y >= 1
                && point.Y < 15) {
                int data = Math.Clamp((int)(4f * MathUtils.LinearStep(190f, 256f, num)), 0, 3);
                chunk.SetCellValueFast(point.X, num, point.Y, Terrain.MakeBlockValue(258, 0, data));
            }
        }

        public void GenerateSnowAndIce(TerrainChunk chunk) {
            for (int i = 0; i < 16; i++) {
                for (int j = 0; j < 16; j++) {
                    int num = i + chunk.Origin.X;
                    int num2 = j + chunk.Origin.Y;
                    for (int num3 = 254; num3 >= 0; num3--) {
                        int cellValueFast = chunk.GetCellValueFast(i, num3, j);
                        int num4 = Terrain.ExtractContents(cellValueFast);
                        if (num4 != 0) {
                            if (!SubsystemWeather.IsPlaceFrozen(chunk.GetTemperatureFast(i, j), num3)) {
                                break;
                            }
                            if (BlocksManager.Blocks[num4] is WaterBlock) {
                                if (CalculateOceanShoreDistance(num, num2) > -20f) {
                                    float num5 = 1 + (int)(2f * MathUtils.Sqr(SimplexNoise.OctavedNoise(num, num2, 0.2f, 1, 2f, 1f)));
                                    for (int k = 0; k < num5; k++) {
                                        if (num3 - k > 0) {
                                            if (!(BlocksManager.Blocks[chunk.GetCellContentsFast(i, num3 - k, j)] is WaterBlock)) {
                                                break;
                                            }
                                            chunk.SetCellValueFast(i, num3 - k, j, 62);
                                        }
                                    }
                                    if (SubsystemWeather.ShaftHasSnowOnIce(num, num2)) {
                                        chunk.SetCellValueFast(i, num3 + 1, j, 61);
                                    }
                                }
                            }
                            else if (SubsystemSnowBlockBehavior.CanSupportSnow(cellValueFast)) {
                                chunk.SetCellValueFast(i, num3 + 1, j, 61);
                            }
                            if (num4 == 8) {
                                chunk.SetCellValueFast(i, num3, j, Terrain.MakeBlockValue(8, 0, 1));
                            }
                            break;
                        }
                    }
                }
            }
        }

        public void PropagateFluidsDownwards(TerrainChunk chunk) {
            for (int i = 0; i < 16; i++) {
                for (int j = 0; j < 16; j++) {
                    int num = TerrainChunk.CalculateCellIndex(i, 255, j);
                    int num2 = 0;
                    int num3 = 255;
                    while (num3 >= 0) {
                        int num4 = Terrain.ExtractContents(chunk.GetCellValueFast(num));
                        if (num4 == 0
                            && num2 != 0
                            && BlocksManager.FluidBlocks[num2] != null) {
                            chunk.SetCellValueFast(num, num2);
                            num4 = num2;
                        }
                        num2 = num4;
                        num3--;
                        num--;
                    }
                }
            }
        }

        public virtual void UpdateFluidIsTop(TerrainChunk chunk) {
            for (int i = 0; i < 16; i++) {
                for (int j = 0; j < 16; j++) {
                    int num = TerrainChunk.CalculateCellIndex(i, 255, j);
                    int num2 = 0;
                    int num3 = 255;
                    while (num3 >= 0) {
                        int cellValueFast = chunk.GetCellValueFast(num);
                        int num4 = Terrain.ExtractContents(cellValueFast);
                        if (num4 != num2
                            && BlocksManager.FluidBlocks[num4] != null
                            && BlocksManager.FluidBlocks[num2] == null) {
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

        public static void CreateBrushes() {
            Random random = new(17);
            for (int i = 0; i < 16; i++) {
                TerrainBrush terrainBrush = new();
                int num = random.Int(4, 12);
                for (int j = 0; j < num; j++) {
                    Vector3 vector = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f)));
                    int num2 = random.Int(3, 8);
                    Vector3 zero = Vector3.Zero;
                    for (int k = 0; k < num2; k++) {
                        terrainBrush.AddBox(
                            (int)MathF.Floor(zero.X),
                            (int)MathF.Floor(zero.Y),
                            (int)MathF.Floor(zero.Z),
                            1,
                            1,
                            1,
                            16
                        );
                        zero += vector;
                    }
                }
                if (i == 0) {
                    terrainBrush.AddCell(0, 0, 0, 150);
                }
                terrainBrush.Compile();
                m_coalBrushes.Add(terrainBrush);
            }
            for (int l = 0; l < 16; l++) {
                TerrainBrush terrainBrush2 = new();
                int num3 = random.Int(3, 7);
                for (int m = 0; m < num3; m++) {
                    Vector3 vector2 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f)));
                    int num4 = random.Int(3, 6);
                    Vector3 zero2 = Vector3.Zero;
                    for (int n = 0; n < num4; n++) {
                        terrainBrush2.AddBox(
                            (int)MathF.Floor(zero2.X),
                            (int)MathF.Floor(zero2.Y),
                            (int)MathF.Floor(zero2.Z),
                            1,
                            1,
                            1,
                            39
                        );
                        zero2 += vector2;
                    }
                }
                terrainBrush2.Compile();
                m_ironBrushes.Add(terrainBrush2);
            }
            for (int num5 = 0; num5 < 16; num5++) {
                TerrainBrush terrainBrush3 = new();
                int num6 = random.Int(4, 10);
                for (int num7 = 0; num7 < num6; num7++) {
                    Vector3 vector3 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-2f, 2f), random.Float(-1f, 1f)));
                    int num8 = random.Int(3, 6);
                    Vector3 zero3 = Vector3.Zero;
                    for (int num9 = 0; num9 < num8; num9++) {
                        terrainBrush3.AddBox(
                            (int)MathF.Floor(zero3.X),
                            (int)MathF.Floor(zero3.Y),
                            (int)MathF.Floor(zero3.Z),
                            1,
                            1,
                            1,
                            41
                        );
                        zero3 += vector3;
                    }
                }
                terrainBrush3.Compile();
                m_copperBrushes.Add(terrainBrush3);
            }
            for (int num10 = 0; num10 < 16; num10++) {
                TerrainBrush terrainBrush4 = new();
                int num11 = random.Int(8, 16);
                for (int num12 = 0; num12 < num11; num12++) {
                    Vector3 vector4 = 0.5f
                        * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.25f, 0.25f), random.Float(-1f, 1f)));
                    int num13 = random.Int(4, 8);
                    Vector3 zero4 = Vector3.Zero;
                    for (int num14 = 0; num14 < num13; num14++) {
                        terrainBrush4.AddBox(
                            (int)MathF.Floor(zero4.X),
                            (int)MathF.Floor(zero4.Y),
                            (int)MathF.Floor(zero4.Z),
                            1,
                            1,
                            1,
                            100
                        );
                        zero4 += vector4;
                    }
                }
                terrainBrush4.Compile();
                m_saltpeterBrushes.Add(terrainBrush4);
            }
            for (int num15 = 0; num15 < 16; num15++) {
                TerrainBrush terrainBrush5 = new();
                int num16 = random.Int(4, 10);
                for (int num17 = 0; num17 < num16; num17++) {
                    Vector3 vector5 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f)));
                    int num18 = random.Int(3, 6);
                    Vector3 zero5 = Vector3.Zero;
                    for (int num19 = 0; num19 < num18; num19++) {
                        terrainBrush5.AddBox(
                            (int)MathF.Floor(zero5.X),
                            (int)MathF.Floor(zero5.Y),
                            (int)MathF.Floor(zero5.Z),
                            1,
                            1,
                            1,
                            101
                        );
                        zero5 += vector5;
                    }
                }
                terrainBrush5.Compile();
                m_sulphurBrushes.Add(terrainBrush5);
            }
            for (int num20 = 0; num20 < 16; num20++) {
                TerrainBrush terrainBrush6 = new();
                int num21 = random.Int(2, 6);
                for (int num22 = 0; num22 < num21; num22++) {
                    Vector3 vector6 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f)));
                    int num23 = random.Int(3, 6);
                    Vector3 zero6 = Vector3.Zero;
                    for (int num24 = 0; num24 < num23; num24++) {
                        terrainBrush6.AddBox(
                            (int)MathF.Floor(zero6.X),
                            (int)MathF.Floor(zero6.Y),
                            (int)MathF.Floor(zero6.Z),
                            1,
                            1,
                            1,
                            112
                        );
                        zero6 += vector6;
                    }
                }
                terrainBrush6.Compile();
                m_diamondBrushes.Add(terrainBrush6);
            }
            for (int num25 = 0; num25 < 16; num25++) {
                TerrainBrush terrainBrush7 = new();
                int num26 = random.Int(4, 10);
                for (int num27 = 0; num27 < num26; num27++) {
                    Vector3 vector7 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f)));
                    int num28 = random.Int(3, 6);
                    Vector3 zero7 = Vector3.Zero;
                    for (int num29 = 0; num29 < num28; num29++) {
                        terrainBrush7.AddBox(
                            (int)MathF.Floor(zero7.X),
                            (int)MathF.Floor(zero7.Y),
                            (int)MathF.Floor(zero7.Z),
                            1,
                            1,
                            1,
                            148
                        );
                        zero7 += vector7;
                    }
                }
                terrainBrush7.Compile();
                m_germaniumBrushes.Add(terrainBrush7);
            }
            for (int num30 = 0; num30 < 16; num30++) {
                TerrainBrush terrainBrush8 = new();
                int num31 = random.Int(16, 32);
                for (int num32 = 0; num32 < num31; num32++) {
                    Vector3 vector8 = 0.5f
                        * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.75f, 0.75f), random.Float(-1f, 1f)));
                    int num33 = random.Int(6, 12);
                    Vector3 zero8 = Vector3.Zero;
                    for (int num34 = 0; num34 < num33; num34++) {
                        terrainBrush8.AddBox(
                            (int)MathF.Floor(zero8.X),
                            (int)MathF.Floor(zero8.Y),
                            (int)MathF.Floor(zero8.Z),
                            1,
                            1,
                            1,
                            2
                        );
                        zero8 += vector8;
                    }
                }
                terrainBrush8.Compile();
                m_dirtPocketBrushes.Add(terrainBrush8);
            }
            for (int num35 = 0; num35 < 16; num35++) {
                TerrainBrush terrainBrush9 = new();
                int num36 = random.Int(16, 32);
                for (int num37 = 0; num37 < num36; num37++) {
                    Vector3 vector9 = 0.5f
                        * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.75f, 0.75f), random.Float(-1f, 1f)));
                    int num38 = random.Int(6, 12);
                    Vector3 zero9 = Vector3.Zero;
                    for (int num39 = 0; num39 < num38; num39++) {
                        terrainBrush9.AddBox(
                            (int)MathF.Floor(zero9.X),
                            (int)MathF.Floor(zero9.Y),
                            (int)MathF.Floor(zero9.Z),
                            1,
                            1,
                            1,
                            6
                        );
                        zero9 += vector9;
                    }
                }
                terrainBrush9.Compile();
                m_gravelPocketBrushes.Add(terrainBrush9);
            }
            for (int num40 = 0; num40 < 16; num40++) {
                TerrainBrush terrainBrush10 = new();
                int num41 = random.Int(16, 32);
                for (int num42 = 0; num42 < num41; num42++) {
                    Vector3 vector10 = 0.5f
                        * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.75f, 0.75f), random.Float(-1f, 1f)));
                    int num43 = random.Int(6, 12);
                    Vector3 zero10 = Vector3.Zero;
                    for (int num44 = 0; num44 < num43; num44++) {
                        terrainBrush10.AddBox(
                            (int)MathF.Floor(zero10.X),
                            (int)MathF.Floor(zero10.Y),
                            (int)MathF.Floor(zero10.Z),
                            1,
                            1,
                            1,
                            66
                        );
                        zero10 += vector10;
                    }
                }
                terrainBrush10.Compile();
                m_limestonePocketBrushes.Add(terrainBrush10);
            }
            for (int num45 = 0; num45 < 16; num45++) {
                TerrainBrush terrainBrush11 = new();
                int num46 = random.Int(16, 32);
                for (int num47 = 0; num47 < num46; num47++) {
                    Vector3 vector11 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.1f, 0.1f), random.Float(-1f, 1f)));
                    int num48 = random.Int(6, 12);
                    Vector3 zero11 = Vector3.Zero;
                    for (int num49 = 0; num49 < num48; num49++) {
                        terrainBrush11.AddBox(
                            (int)MathF.Floor(zero11.X),
                            (int)MathF.Floor(zero11.Y),
                            (int)MathF.Floor(zero11.Z),
                            1,
                            1,
                            1,
                            72
                        );
                        zero11 += vector11;
                    }
                }
                terrainBrush11.Compile();
                m_clayPocketBrushes.Add(terrainBrush11);
            }
            for (int num50 = 0; num50 < 16; num50++) {
                TerrainBrush terrainBrush12 = new();
                int num51 = random.Int(16, 32);
                for (int num52 = 0; num52 < num51; num52++) {
                    Vector3 vector12 = 0.5f
                        * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.75f, 0.75f), random.Float(-1f, 1f)));
                    int num53 = random.Int(6, 12);
                    Vector3 zero12 = Vector3.Zero;
                    for (int num54 = 0; num54 < num53; num54++) {
                        terrainBrush12.AddBox(
                            (int)MathF.Floor(zero12.X),
                            (int)MathF.Floor(zero12.Y),
                            (int)MathF.Floor(zero12.Z),
                            1,
                            1,
                            1,
                            7
                        );
                        zero12 += vector12;
                    }
                }
                terrainBrush12.Compile();
                m_sandPocketBrushes.Add(terrainBrush12);
            }
            for (int num55 = 0; num55 < 16; num55++) {
                TerrainBrush terrainBrush13 = new();
                int num56 = random.Int(16, 32);
                for (int num57 = 0; num57 < num56; num57++) {
                    Vector3 vector13 = 0.5f
                        * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.75f, 0.75f), random.Float(-1f, 1f)));
                    int num58 = random.Int(6, 12);
                    Vector3 zero13 = Vector3.Zero;
                    for (int num59 = 0; num59 < num58; num59++) {
                        terrainBrush13.AddBox(
                            (int)MathF.Floor(zero13.X),
                            (int)MathF.Floor(zero13.Y),
                            (int)MathF.Floor(zero13.Z),
                            1,
                            1,
                            1,
                            67
                        );
                        zero13 += vector13;
                    }
                }
                terrainBrush13.Compile();
                m_basaltPocketBrushes.Add(terrainBrush13);
            }
            for (int num60 = 0; num60 < 16; num60++) {
                TerrainBrush terrainBrush14 = new();
                int num61 = random.Int(16, 32);
                for (int num62 = 0; num62 < num61; num62++) {
                    Vector3 vector14 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f)));
                    int num63 = random.Int(5, 10);
                    Vector3 zero14 = Vector3.Zero;
                    for (int num64 = 0; num64 < num63; num64++) {
                        terrainBrush14.AddBox(
                            (int)MathF.Floor(zero14.X),
                            (int)MathF.Floor(zero14.Y),
                            (int)MathF.Floor(zero14.Z),
                            1,
                            1,
                            1,
                            3
                        );
                        zero14 += vector14;
                    }
                }
                terrainBrush14.Compile();
                m_granitePocketBrushes.Add(terrainBrush14);
            }
            int[] array = [4, 6, 8];
            for (int num65 = 0; num65 < 4 * array.Length; num65++) {
                TerrainBrush terrainBrush15 = new();
                int num66 = array[num65 / 4];
                int num67 = num65 % 2 + 1;
                float num68 = num65 % 4 == 2 ? 0.5f : 1f;
                int num69 = num65 % 4 == 1 ? num66 * num66 : 2 * num66 * num66;
                for (int num70 = 0; num70 < num69; num70++) {
                    Vector2 vector15 = random.Vector2(0f, num66);
                    float num71 = vector15.Length();
                    int num72 = random.Int(3, 4);
                    int sizeY = 1 + (int)MathUtils.Lerp(MathUtils.Max(num66 / 3, 2.5f) * num68, 0f, num71 / num66) + random.Int(0, 1);
                    terrainBrush15.AddBox(
                        (int)MathF.Floor(vector15.X),
                        0,
                        (int)MathF.Floor(vector15.Y),
                        num72,
                        sizeY,
                        num72,
                        0
                    );
                    terrainBrush15.AddBox(
                        (int)MathF.Floor(vector15.X),
                        -num67,
                        (int)MathF.Floor(vector15.Y),
                        num72,
                        num67,
                        num72,
                        18
                    );
                }
                terrainBrush15.Compile();
                m_waterPocketBrushes.Add(terrainBrush15);
            }
            int[] array2 = [8, 12, 14, 16];
            for (int num73 = 0; num73 < 4 * array2.Length; num73++) {
                TerrainBrush terrainBrush16 = new();
                int num74 = array2[num73 / 4];
                int num75 = num74 + 2;
                float num76 = num73 % 4 == 2 ? 0.5f : 1f;
                int num77 = num73 % 4 == 1 ? num74 * num74 : 2 * num74 * num74;
                for (int num78 = 0; num78 < num77; num78++) {
                    Vector2 vector16 = random.Vector2(0f, num74);
                    float num79 = vector16.Length();
                    int num80 = random.Int(3, 4);
                    int sizeY2 = 1 + (int)MathUtils.Lerp(MathUtils.Max(num74 / 3, 2.5f) * num76, 0f, num79 / num74) + random.Int(0, 1);
                    int num81 = 1 + (int)MathUtils.Lerp(num75, 0f, num79 / num74) + random.Int(0, 1);
                    terrainBrush16.AddBox(
                        (int)MathF.Floor(vector16.X),
                        0,
                        (int)MathF.Floor(vector16.Y),
                        num80,
                        sizeY2,
                        num80,
                        0
                    );
                    terrainBrush16.AddBox(
                        (int)MathF.Floor(vector16.X),
                        -num81,
                        (int)MathF.Floor(vector16.Y),
                        num80,
                        num81,
                        num80,
                        92
                    );
                }
                terrainBrush16.Compile();
                m_magmaPocketBrushes.Add(terrainBrush16);
            }
            for (int num82 = 0; num82 < 7; num82++) {
                m_caveBrushesByType.Add([]);
                for (int num83 = 0; num83 < 3; num83++) {
                    TerrainBrush terrainBrush17 = new();
                    int num84 = 6 + 4 * num82;
                    int max = 3 + num82 / 3;
                    int max2 = 9 + num82;
                    for (int num85 = 0; num85 < num84; num85++) {
                        int num86 = random.Int(2, max);
                        int num87 = random.Int(8, max2) - 2 * num86;
                        Vector3 vector17 = 0.5f * new Vector3(random.Float(-1f, 1f), random.Float(0f, 1f), random.Float(-1f, 1f));
                        Vector3 zero15 = Vector3.Zero;
                        for (int num88 = 0; num88 < num87; num88++) {
                            terrainBrush17.AddBox(
                                (int)MathF.Floor(zero15.X) - num86 / 2,
                                (int)MathF.Floor(zero15.Y) - num86 / 2,
                                (int)MathF.Floor(zero15.Z) - num86 / 2,
                                num86,
                                num86,
                                num86,
                                0
                            );
                            zero15 += vector17;
                        }
                    }
                    terrainBrush17.Compile();
                    m_caveBrushesByType[num82].Add(terrainBrush17);
                }
            }
            ModsManager.HookAction(
                "OnTerrainBrushesCreated",
                loader => {
                    loader.OnTerrainBrushesCreated();
                    return false;
                }
            );
        }
    }
}