using Engine;
using Engine.Audio;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemWeather : Subsystem, IDrawable, IUpdateable {
        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemBlocksScanner m_subsystemBlocksScanner;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemSeasons m_subsystemSeasons;

        public Random m_random = new();

        public Dictionary<GameWidget, Dictionary<Point2, PrecipitationShaftParticleSystem>> m_activeShafts = [];

        public List<PrecipitationShaftParticleSystem> m_toRemove = [];

        public Dictionary<GameWidget, Vector2?> m_lastShaftsUpdatePositions = [];

        public float m_targetRainSoundVolume;

        public double m_precipitationStartTime;

        public double m_precipitationEndTime;

        public float m_precipitationRampTime;

        public float m_lightningIntensity;

        public double m_fogStartTime;

        public double m_fogEndTime;

        public float m_fogRampTime;

        public const int m_rainSoundRadius = 7;

        public float m_rainVolumeFactor;

        public Sound m_rainSound;

        public int[] m_shuffledOrder;

        public static int[] m_drawOrders = [50];

        public SubsystemTerrain SubsystemTerrain { get; set; }

        public SubsystemSky SubsystemSky { get; set; }

        public SubsystemTime SubsystemTime { get; set; }

        public virtual RainSplashParticleSystem RainSplashParticleSystem { get; set; }

        public virtual SnowSplashParticleSystem SnowSplashParticleSystem { get; set; }

        public virtual Color RainColor { get; set; }

        public virtual Color SnowColor { get; set; }

        public virtual float PrecipitationIntensity { get; set; }

        public virtual int FogSeed { get; set; }

        public virtual float FogProgress { get; set; }

        public virtual float FogIntensity { get; set; }

        public virtual bool IsPrecipitationStarted {
            get {
                if (m_subsystemGameInfo.TotalElapsedGameTime >= m_precipitationStartTime) {
                    return m_subsystemGameInfo.TotalElapsedGameTime < m_precipitationEndTime - m_precipitationRampTime;
                }
                return false;
            }
        }

        public virtual bool IsFogStarted {
            get {
                if (m_subsystemGameInfo.TotalElapsedGameTime >= m_fogStartTime) {
                    return m_subsystemGameInfo.TotalElapsedGameTime < m_fogEndTime - m_fogRampTime;
                }
                return false;
            }
        }

        public int[] DrawOrders => m_drawOrders;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public static Func<int, int> GetTemperatureAdjustmentAtHeight =
            y => (int)MathF.Round(y > 64 ? -0.0008f * MathUtils.Sqr(y - 64) : 0.1f * (64 - y));

        public static Func<int, int, bool> IsPlaceFrozen = (temperature, y) => temperature + GetTemperatureAdjustmentAtHeight(y) <= 0;
        public static Func<int, int, bool> ShaftHasSnowOnIce = (x, z) => MathUtils.Hash((uint)((x & 0xFFFF) | (z << 16))) > 429496729;

        public virtual PrecipitationShaftInfo GetPrecipitationShaftInfo(int x, int z) {
            int shaftValue = SubsystemTerrain.Terrain.GetShaftValue(x, z);
            int seasonalTemperature = SubsystemTerrain.Terrain.GetSeasonalTemperature(shaftValue);
            int num = Terrain.ExtractTopHeight(shaftValue);
            PrecipitationShaftInfo result;
            if (IsPlaceFrozen(seasonalTemperature, num)) {
                result = default;
                result.Intensity = PrecipitationIntensity;
                result.Type = PrecipitationType.Snow;
                result.YLimit = num + 1;
                return result;
            }
            int seasonalHumidity = SubsystemTerrain.Terrain.GetSeasonalHumidity(shaftValue);
            if (seasonalTemperature <= 8
                || seasonalHumidity >= 8) {
                result = default;
                result.Intensity = PrecipitationIntensity;
                result.Type = PrecipitationType.Rain;
                result.YLimit = num + 1;
                return result;
            }
            result = default;
            result.Intensity = 0f;
            result.Type = PrecipitationType.Rain;
            result.YLimit = num + 1;
            return result;
        }

        public virtual void ManualLightingStrike(Vector3 position, Vector3 direction) {
            int num = Terrain.ToCell(position.X + direction.X * 32f);
            int num2 = Terrain.ToCell(position.Z + direction.Z * 32f);
            Vector3? vector = null;
            for (int i = 0; i < 300; i++) {
                int num3 = m_random.Int(-8, 8);
                int num4 = m_random.Int(-8, 8);
                int num5 = num + num3;
                int num6 = num2 + num4;
                int num7 = SubsystemTerrain.Terrain.CalculateTopmostCellHeight(num5, num6);
                if (!vector.HasValue
                    || num7 > vector.Value.Y) {
                    vector = new Vector3(num5, num7, num6);
                }
            }
            if (vector.HasValue) {
                SubsystemSky.MakeLightningStrike(vector.Value, true);
            }
        }

        public virtual void ManualPrecipitationStart() {
            m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime;
            m_precipitationEndTime = double.PositiveInfinity;
            m_precipitationRampTime = 1f;
        }

        public virtual void ManualPrecipitationEnd() {
            m_precipitationRampTime = 1f;
            m_precipitationEndTime = m_subsystemGameInfo.TotalElapsedGameTime + m_precipitationRampTime;
        }

        public virtual void ManualFogStart() {
            m_fogStartTime = m_subsystemGameInfo.TotalElapsedGameTime;
            m_fogEndTime = double.PositiveInfinity;
            m_fogRampTime = 3f;
        }

        public virtual void ManualFogEnd() {
            m_fogRampTime = 3f;
            m_fogEndTime = m_subsystemGameInfo.TotalElapsedGameTime + m_fogRampTime;
        }

        public virtual void Draw(Camera camera, int drawOrder) {
            int num = SettingsManager.VisibilityRange > 128 ? 9 :
                SettingsManager.VisibilityRange <= 64 ? 7 : 8;
            int num2 = num * num;
            Dictionary<Point2, PrecipitationShaftParticleSystem> activeShafts = GetActiveShafts(camera.GameWidget);
            byte b = (byte)(255f * MathUtils.Lerp(0.15f, 1f, SubsystemSky.SkyLightIntensity));
            byte b2 = (byte)(255f * MathUtils.Lerp(0.15f, 1f, SubsystemSky.SkyLightIntensity));
            Color rainColor = new(b, b, b);
            Color snowColor = new(b2, b2, b2);
            ModsManager.HookAction(
                "SetRainAndSnowColor",
                modloader => {
                    modloader.SetRainAndSnowColor(ref rainColor, ref snowColor);
                    return false;
                }
            );
            RainColor = rainColor;
            SnowColor = snowColor;
            Vector2 vector = new(camera.ViewPosition.X, camera.ViewPosition.Z);
            Point2 point = Terrain.ToCell(vector);
            m_lastShaftsUpdatePositions.TryGetValue(camera.GameWidget, out Vector2? value);
            if (value.HasValue
                && !(Vector2.DistanceSquared(value.Value, vector) > 1f)) {
                return;
            }
            m_lastShaftsUpdatePositions[camera.GameWidget] = vector;
            m_toRemove.Clear();
            foreach (PrecipitationShaftParticleSystem value2 in activeShafts.Values) {
                if (MathUtils.Sqr(value2.Point.X + 0.5f - vector.X) + MathUtils.Sqr(value2.Point.Y + 0.5f - vector.Y) > num2 + 1f) {
                    m_toRemove.Add(value2);
                }
            }
            foreach (PrecipitationShaftParticleSystem item in m_toRemove) {
                if (m_subsystemParticles.ContainsParticleSystem(item)) {
                    m_subsystemParticles.RemoveParticleSystem(item);
                }
                activeShafts.Remove(item.Point);
            }
            for (int i = point.X - num; i <= point.X + num; i++) {
                for (int j = point.Y - num; j <= point.Y + num; j++) {
                    if (MathUtils.Sqr(i + 0.5f - vector.X) + MathUtils.Sqr(j + 0.5f - vector.Y) <= num2) {
                        Point2 point2 = new(i, j);
                        if (!activeShafts.ContainsKey(point2)) {
                            PrecipitationShaftParticleSystem precipitationShaftParticleSystem = new(camera.GameWidget, this, m_random, point2);
                            m_subsystemParticles.AddParticleSystem(precipitationShaftParticleSystem);
                            activeShafts.Add(point2, precipitationShaftParticleSystem);
                        }
                    }
                }
            }
        }

        public virtual void Update(float dt) {
            UpdatePrecipitation(dt);
            UpdateLightning(dt);
            UpdateFog(dt);
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            SubsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemBlocksScanner = Project.FindSubsystem<SubsystemBlocksScanner>(true);
            SubsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            SubsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemSeasons = Project.FindSubsystem<SubsystemSeasons>(true);
            m_precipitationStartTime = valuesDictionary.GetValue<double>("WeatherStartTime");
            m_precipitationEndTime = valuesDictionary.GetValue<double>("WeatherEndTime");
            m_precipitationRampTime = valuesDictionary.GetValue("WeatherRampTime", 25f);
            PrecipitationIntensity = valuesDictionary.GetValue("PrecipitationIntensity", 0f);
            m_lightningIntensity = valuesDictionary.GetValue<float>("LightningIntensity");
            m_fogStartTime = valuesDictionary.GetValue<double>("FogStartTime");
            m_fogEndTime = valuesDictionary.GetValue<double>("FogEndTime");
            m_fogRampTime = valuesDictionary.GetValue("FogRampTime", 25f);
            FogIntensity = valuesDictionary.GetValue("FogIntensity", 0f);
            FogProgress = valuesDictionary.GetValue("FogProgress", 0f);
            m_rainSound = m_subsystemAudio.CreateSound("Audio/Rain");
            m_rainSound.IsLooped = true;
            m_rainSound.Volume = 0f;
            RainSplashParticleSystem = new RainSplashParticleSystem();
            m_subsystemParticles.AddParticleSystem(RainSplashParticleSystem);
            SnowSplashParticleSystem = new SnowSplashParticleSystem();
            m_subsystemParticles.AddParticleSystem(SnowSplashParticleSystem);
            m_rainVolumeFactor = 0f;
            for (int i = -7; i <= 7; i++) {
                for (int j = -7; j <= 7; j++) {
                    float distance = MathF.Sqrt(i * i + j * j);
                    m_rainVolumeFactor += m_subsystemAudio.CalculateVolume(distance, 1f);
                }
            }
            m_subsystemBlocksScanner.ScanningChunkCompleted += delegate(TerrainChunk chunk) {
                if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living) {
                    FreezeThawAndDepositSnow(chunk, 0.66f, 0.66f, false);
                }
            };
            SubsystemTerrain.TerrainUpdater.ChunkInitialized += delegate(TerrainChunk chunk) {
                if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living) {
                    FreezeThawAndDepositSnow(chunk, 1f, 1f, m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled);
                    FreezeThawAndDepositSnow(chunk, 0.66f, 0.66f, m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled);
                }
            };
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            valuesDictionary.SetValue("WeatherStartTime", m_precipitationStartTime);
            valuesDictionary.SetValue("WeatherEndTime", m_precipitationEndTime);
            valuesDictionary.SetValue("WeatherRampTime", m_precipitationRampTime);
            valuesDictionary.SetValue("WeatherIntensity", PrecipitationIntensity);
            valuesDictionary.SetValue("LightningIntensity", m_lightningIntensity);
            valuesDictionary.SetValue("FogStartTime", m_fogStartTime);
            valuesDictionary.SetValue("FogEndTime", m_fogEndTime);
            valuesDictionary.SetValue("FogRampTime", m_fogRampTime);
            valuesDictionary.SetValue("FogIntensity", FogIntensity);
            valuesDictionary.SetValue("FogProgress", FogProgress);
        }

        public virtual void UpdatePrecipitation(float dt) {
            if (m_subsystemGameInfo.TotalElapsedGameTime > m_precipitationEndTime) {
                float num = 0f;
                float num2 = 0f;
                float probability = 0f;
                switch (m_subsystemSeasons.Season) {
                    case Season.Summer:
                        num = 1f;
                        num2 = 1f;
                        probability = 0.5f;
                        break;
                    case Season.Autumn:
                        num = 1.5f;
                        num2 = 1f;
                        probability = 0.5f;
                        break;
                    case Season.Winter:
                        num = 1f;
                        num2 = 0f;
                        probability = 0f;
                        break;
                    case Season.Spring:
                        num = 1.5f;
                        num2 = 2f;
                        probability = 1f;
                        break;
                }
                if (m_precipitationEndTime == 0.0) {
                    if (m_subsystemGameInfo.WorldSettings.StartingPositionMode == StartingPositionMode.Hard) {
                        m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime + 60f * m_random.Float(0f, 2f);
                        m_lightningIntensity = m_random.Float(0.66f, 1f) * num2;
                    }
                    else {
                        m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime + 60f * m_random.Float(3f, 6f);
                        m_lightningIntensity = m_random.Float(0.33f, 0.66f) * num2;
                    }
                }
                else {
                    m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime + 60f * m_random.Float(5f, 45f) / num;
                    m_lightningIntensity = m_random.Bool(probability) ? MathUtils.Saturate(m_random.Float(0.33f, 1f) * num2) : 0f;
                }
                m_precipitationEndTime = m_precipitationStartTime + 60f * m_random.Float(3f, 6f);
                m_precipitationRampTime = m_random.Float(10f, 30f);
            }
            if (m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled) {
                if (IsPrecipitationStarted) {
                    PrecipitationIntensity = MathUtils.Saturate(PrecipitationIntensity + dt / m_precipitationRampTime);
                }
                else {
                    PrecipitationIntensity = MathUtils.Saturate(PrecipitationIntensity - dt / m_precipitationRampTime);
                }
            }
            else {
                PrecipitationIntensity = 0f;
            }
            if (Time.PeriodicEvent(0.33000001311302185, 0.0)) {
                float num3 = 0f;
                if (PrecipitationIntensity > 0f) {
                    float num4 = 0f;
                    Vector3 vector = default;
                    foreach (Vector3 listenerPosition in m_subsystemAudio.ListenerPositions) {
                        int num5 = Terrain.ToCell(listenerPosition.X) - 7;
                        int num6 = Terrain.ToCell(listenerPosition.Z) - 7;
                        int num7 = Terrain.ToCell(listenerPosition.X) + 7;
                        int num8 = Terrain.ToCell(listenerPosition.Z) + 7;
                        for (int i = num5; i <= num7; i++) {
                            for (int j = num6; j <= num8; j++) {
                                PrecipitationShaftInfo precipitationShaftInfo = GetPrecipitationShaftInfo(i, j);
                                if (precipitationShaftInfo.Type == PrecipitationType.Rain
                                    && precipitationShaftInfo.Intensity > 0f) {
                                    vector.X = i + 0.5f;
                                    vector.Y = MathUtils.Max(precipitationShaftInfo.YLimit, listenerPosition.Y);
                                    vector.Z = j + 0.5f;
                                    float num9 = vector.X - listenerPosition.X;
                                    float num10 = 8f * (vector.Y - listenerPosition.Y);
                                    float num11 = vector.Z - listenerPosition.Z;
                                    float distance = MathF.Sqrt(num9 * num9 + num10 * num10 + num11 * num11);
                                    num4 += m_subsystemAudio.CalculateVolume(distance, 1f) * precipitationShaftInfo.Intensity;
                                }
                            }
                        }
                    }
                    num3 = MathUtils.Max(num3, num4);
                }
                m_targetRainSoundVolume = MathUtils.Saturate(2f * num3 / m_rainVolumeFactor);
            }
            m_rainSound.Volume = MathUtils.Saturate(
                MathUtils.Lerp(m_rainSound.Volume, SettingsManager.SoundsVolume * m_targetRainSoundVolume, 5f * dt)
            );
            if (m_rainSound.Volume > AudioManager.MinAudibleVolume) {
                m_rainSound.Play();
            }
            else {
                m_rainSound.Pause();
            }
        }

        public virtual void UpdateLightning(float dt) {
            if (PrecipitationIntensity != 1f
                || !SubsystemTime.PeriodicGameTimeEvent(1.0, 0.0)) {
                return;
            }
            TerrainChunk[] allocatedChunks = SubsystemTerrain.Terrain.AllocatedChunks;
            for (int i = 0; i < allocatedChunks.Length; i++) {
                TerrainChunk terrainChunk = allocatedChunks[m_random.Int(0, allocatedChunks.Length - 1)];
                if (terrainChunk.State < TerrainChunkState.InvalidVertices1
                    || !m_random.Bool(m_lightningIntensity * 0.0002f)) {
                    continue;
                }
                int num = terrainChunk.Origin.X + m_random.Int(0, 15);
                int num2 = terrainChunk.Origin.Y + m_random.Int(0, 15);
                Vector3? vector = null;
                for (int j = num - 8; j < num + 8; j++) {
                    for (int k = num2 - 8; k < num2 + 8; k++) {
                        int topHeight = SubsystemTerrain.Terrain.GetTopHeight(j, k);
                        if (!vector.HasValue
                            || topHeight > vector.Value.Y) {
                            vector = new Vector3(j, topHeight, k);
                        }
                    }
                }
                if (vector.HasValue) {
                    SubsystemSky.MakeLightningStrike(vector.Value, false);
                    break;
                }
            }
        }

        public virtual void UpdateFog(float dt) {
            if (m_subsystemGameInfo.TotalElapsedGameTime > m_fogEndTime) {
                float num = m_subsystemSeasons.Season == Season.Autumn || m_subsystemSeasons.Season == Season.Winter ? 1.75f : 1f;
                if (m_fogEndTime == 0.0
                    && m_subsystemGameInfo.WorldSettings.StartingPositionMode == StartingPositionMode.Hard) {
                    m_fogStartTime = m_subsystemGameInfo.TotalElapsedGameTime + 60f * m_random.Float(1f, 10f) / num;
                }
                else {
                    m_fogStartTime = m_subsystemGameInfo.TotalElapsedGameTime + 60f * m_random.Float(10f, 40f) / num;
                }
                m_fogEndTime = m_fogStartTime + 60f * m_random.Float(4f, 7f) * num;
                m_fogRampTime = m_random.Float(20f, 40f);
                FogProgress = 0f;
            }
            FogSeed = MathUtils.Hash((int)MathUtils.Remainder(m_fogStartTime, 1000000.0));
            if (m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled) {
                if (IsFogStarted) {
                    FogIntensity = MathUtils.Saturate(FogIntensity + dt / m_fogRampTime);
                    FogProgress += dt / (float)(m_fogEndTime - m_fogStartTime);
                }
                else {
                    FogIntensity = MathUtils.Saturate(FogIntensity - dt / m_fogRampTime);
                }
            }
            else {
                FogIntensity = 0f;
            }
        }

        public virtual Dictionary<Point2, PrecipitationShaftParticleSystem> GetActiveShafts(GameWidget gameWidget) {
            if (!m_activeShafts.TryGetValue(gameWidget, out Dictionary<Point2, PrecipitationShaftParticleSystem> value)) {
                value = [];
                m_activeShafts.Add(gameWidget, value);
            }
            return value;
        }

        public virtual void FreezeThawAndDepositSnow(TerrainChunk chunk, float freezeProbability, float thawProbability, bool forceDepositSnow) {
            if (m_shuffledOrder == null) {
                m_shuffledOrder = Enumerable.Range(0, 256).ToArray();
            }
            m_shuffledOrder.RandomShuffle(i => m_random.Int(i));
            Terrain terrain = SubsystemTerrain.Terrain;
            for (int j = 0; j < m_shuffledOrder.Length; j++) {
                int num = m_shuffledOrder[j] & 0xF;
                int num2 = m_shuffledOrder[j] >> 4;
                int num3 = chunk.GetTopHeightFast(num, num2);
                int cellValueFast = chunk.GetCellValueFast(num, num3, num2);
                int num4 = Terrain.ExtractContents(cellValueFast);
                int num5 = chunk.Origin.X + num;
                int num6 = num3;
                int num7 = chunk.Origin.Y + num2;
                PrecipitationShaftInfo precipitationShaftInfo = GetPrecipitationShaftInfo(num5, num7);
                if (precipitationShaftInfo.Type == PrecipitationType.Snow) {
                    if (!m_random.Bool(freezeProbability)) {
                        continue;
                    }
                    if (num4 == 18
                        && SubsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(num5, num7) > -35f) {
                        int cellContents = terrain.GetCellContents(num5 + 1, num6, num7);
                        int cellContents2 = terrain.GetCellContents(num5 - 1, num6, num7);
                        int cellContents3 = terrain.GetCellContents(num5, num6, num7 - 1);
                        int cellContents4 = terrain.GetCellContents(num5, num6, num7 + 1);
                        bool num8 = BlocksManager.FluidBlocks[cellContents] == null && cellContents != 0;
                        bool flag = BlocksManager.FluidBlocks[cellContents2] == null && cellContents2 != 0;
                        bool flag2 = BlocksManager.FluidBlocks[cellContents3] == null && cellContents3 != 0;
                        bool flag3 = BlocksManager.FluidBlocks[cellContents4] == null && cellContents4 != 0;
                        if (num8
                            || flag
                            || flag2
                            || flag3) {
                            SubsystemTerrain.ChangeCell(num5, num6, num7, Terrain.MakeBlockValue(62));
                        }
                    }
                    else {
                        if ((!forceDepositSnow && !(precipitationShaftInfo.Intensity > 0.5f))
                            || num6 + 1 >= 255) {
                            continue;
                        }
                        if (SubsystemSnowBlockBehavior.CanSupportSnow(cellValueFast)) {
                            if (num4 != 62
                                || ShaftHasSnowOnIce(num5, num7)) {
                                SubsystemTerrain.ChangeCell(num5, num6 + 1, num7, Terrain.MakeBlockValue(61));
                            }
                        }
                        else if (SubsystemSnowBlockBehavior.CanBeReplacedBySnow(cellValueFast)) {
                            SubsystemTerrain.ChangeCell(num5, num6, num7, Terrain.MakeBlockValue(61));
                        }
                    }
                }
                else {
                    if (!m_random.Bool(thawProbability)) {
                        continue;
                    }
                    for (;
                        num6 > 0;
                        num3--, num6--, cellValueFast = chunk.GetCellValueFast(num, num3, num2), num4 = Terrain.ExtractContents(cellValueFast)) {
                        switch (num4) {
                            case 61:
                                SubsystemTerrain.DestroyCell(
                                    0,
                                    num5,
                                    num6,
                                    num7,
                                    0,
                                    true,
                                    true
                                );
                                continue;
                            case 62:
                                SubsystemTerrain.DestroyCell(
                                    0,
                                    num5,
                                    num6,
                                    num7,
                                    0,
                                    false,
                                    true
                                );
                                continue;
                        }
                        break;
                    }
                }
            }
        }
    }
}