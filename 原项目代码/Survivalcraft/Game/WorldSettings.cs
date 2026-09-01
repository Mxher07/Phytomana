using Engine;
using TemplatesDatabase;

namespace Game {
    public class WorldSettings {
        public string Name = string.Empty;

        public string OriginalSerializationVersion = string.Empty;

        public string Seed = string.Empty;

        public GameMode GameMode = GameMode.Survival;

        public EnvironmentBehaviorMode EnvironmentBehaviorMode;

        public TimeOfDayMode TimeOfDayMode;

        public bool AreSeasonsChanging = true;

        public float YearDays = 24f;

        public float TimeOfYear = SubsystemSeasons.MidSummer;

        public StartingPositionMode StartingPositionMode;

        public bool AreWeatherEffectsEnabled = true;

        public bool IsAdventureRespawnAllowed = true;

        public bool AreAdventureSurvivalMechanicsEnabled = true;

        public bool AreSupernaturalCreaturesEnabled = true;

        public bool IsFriendlyFireEnabled = true;

        public TerrainGenerationMode TerrainGenerationMode;

        public Vector2 IslandSize = new(400f, 400f);

        public float BiomeSize = 1f;

        public int TerrainLevel = 64;

        public float ShoreRoughness = 0.5f;

        public int TerrainBlockIndex = 8;

        public int TerrainOceanBlockIndex = 18;

        public float TemperatureOffset;

        public float HumidityOffset;

        public int SeaLevelOffset;

        public string BlocksTextureName = string.Empty;

        public WorldPalette Palette = new();

        public bool CustomWorldSeed;

        public int WorldSeed;

        public void ResetOptionsForNonCreativeMode(WorldSettings originalWorldSettings) {
            if (TerrainGenerationMode == TerrainGenerationMode.FlatContinent) {
                TerrainGenerationMode = TerrainGenerationMode.Continent;
            }
            if (TerrainGenerationMode == TerrainGenerationMode.FlatIsland) {
                TerrainGenerationMode = TerrainGenerationMode.Island;
            }
            EnvironmentBehaviorMode environmentBehaviorModeBefore = EnvironmentBehaviorMode;
            EnvironmentBehaviorMode = EnvironmentBehaviorMode.Living;
            TimeOfDayMode timeOfDayModeBefore = TimeOfDayMode;
            TimeOfDayMode = TimeOfDayMode.Changing;
            bool areWeatherEffectsEnabledBefore = AreWeatherEffectsEnabled;
            AreWeatherEffectsEnabled = true;
            bool areSurvivalMechanicsEnabledBefore = AreAdventureSurvivalMechanicsEnabled;
            AreAdventureSurvivalMechanicsEnabled = true;
            IsAdventureRespawnAllowed = true;
            TerrainLevel = 64;
            ShoreRoughness = 0.5f;
            TerrainBlockIndex = 8;
            if (originalWorldSettings != null) {
                AreSeasonsChanging = originalWorldSettings.AreSeasonsChanging;
                YearDays = originalWorldSettings.YearDays;
                TimeOfYear = originalWorldSettings.TimeOfYear;
            }
            ModsManager.HookAction(
                "ResetOptionsForNonCreativeMode",
                loader => {
                    loader.ResetOptionsForNonCreativeMode(
                        this,
                        environmentBehaviorModeBefore,
                        timeOfDayModeBefore,
                        areWeatherEffectsEnabledBefore,
                        areSurvivalMechanicsEnabledBefore
                    );
                    return false;
                }
            );
        }

        public void Load(ValuesDictionary valuesDictionary) {
            Name = valuesDictionary.GetValue<string>("WorldName");
            OriginalSerializationVersion = valuesDictionary.GetValue("OriginalSerializationVersion", string.Empty);
            Seed = valuesDictionary.GetValue("WorldSeedString", string.Empty);
            GameMode = valuesDictionary.GetValue("GameMode", GameMode.Challenging);
            EnvironmentBehaviorMode = valuesDictionary.GetValue("EnvironmentBehaviorMode", EnvironmentBehaviorMode.Living);
            TimeOfDayMode = valuesDictionary.GetValue("TimeOfDayMode", TimeOfDayMode.Changing);
            AreSeasonsChanging = valuesDictionary.GetValue("AreSeasonsChanging", true);
            YearDays = valuesDictionary.GetValue("YearDays", 24f);
            TimeOfYear = valuesDictionary.GetValue("TimeOfYear", SubsystemSeasons.MidSummer);
            StartingPositionMode = valuesDictionary.GetValue("StartingPositionMode", StartingPositionMode.Easy);
            AreWeatherEffectsEnabled = valuesDictionary.GetValue("AreWeatherEffectsEnabled", true);
            IsAdventureRespawnAllowed = valuesDictionary.GetValue("IsAdventureRespawnAllowed", true);
            AreAdventureSurvivalMechanicsEnabled = valuesDictionary.GetValue("AreAdventureSurvivalMechanicsEnabled", true);
            AreSupernaturalCreaturesEnabled = valuesDictionary.GetValue("AreSupernaturalCreaturesEnabled", true);
            IsFriendlyFireEnabled = valuesDictionary.GetValue("IsFriendlyFireEnabled", true);
            TerrainGenerationMode = valuesDictionary.GetValue("TerrainGenerationMode", TerrainGenerationMode.Continent);
            IslandSize = valuesDictionary.GetValue("IslandSize", new Vector2(200f, 200f));
            TerrainLevel = valuesDictionary.GetValue("TerrainLevel", 64);
            ShoreRoughness = valuesDictionary.GetValue("ShoreRoughness", 0f);
            TerrainBlockIndex = valuesDictionary.GetValue("TerrainBlockIndex", 8);
            TerrainOceanBlockIndex = valuesDictionary.GetValue("TerrainOceanBlockIndex", 18);
            TemperatureOffset = valuesDictionary.GetValue("TemperatureOffset", 0f);
            HumidityOffset = valuesDictionary.GetValue("HumidityOffset", 0f);
            SeaLevelOffset = valuesDictionary.GetValue("SeaLevelOffset", 0);
            BiomeSize = valuesDictionary.GetValue("BiomeSize", 1f);
            BlocksTextureName = valuesDictionary.GetValue("BlockTextureName", string.Empty);
            Palette = new WorldPalette(valuesDictionary.GetValue("Palette", new ValuesDictionary()));
            WorldSeed = valuesDictionary.GetValue("WorldSeed", 0);
        }

        public void Save(ValuesDictionary valuesDictionary, bool liveModifiableParametersOnly) {
            valuesDictionary.SetValue("WorldName", Name);
            valuesDictionary.SetValue("OriginalSerializationVersion", OriginalSerializationVersion);
            valuesDictionary.SetValue("GameMode", GameMode);
            valuesDictionary.SetValue("EnvironmentBehaviorMode", EnvironmentBehaviorMode);
            valuesDictionary.SetValue("TimeOfDayMode", TimeOfDayMode);
            valuesDictionary.SetValue("AreSeasonsChanging", AreSeasonsChanging);
            valuesDictionary.SetValue("YearDays", YearDays);
            valuesDictionary.SetValue("TimeOfYear", TimeOfYear);
            valuesDictionary.SetValue("AreWeatherEffectsEnabled", AreWeatherEffectsEnabled);
            valuesDictionary.SetValue("IsAdventureRespawnAllowed", IsAdventureRespawnAllowed);
            valuesDictionary.SetValue("AreAdventureSurvivalMechanicsEnabled", AreAdventureSurvivalMechanicsEnabled);
            valuesDictionary.SetValue("AreSupernaturalCreaturesEnabled", AreSupernaturalCreaturesEnabled);
            valuesDictionary.SetValue("IsFriendlyFireEnabled", IsFriendlyFireEnabled);
            if (!liveModifiableParametersOnly) {
                valuesDictionary.SetValue("WorldSeedString", Seed);
                valuesDictionary.SetValue("TerrainGenerationMode", TerrainGenerationMode);
                valuesDictionary.SetValue("IslandSize", IslandSize);
                valuesDictionary.SetValue("TerrainLevel", TerrainLevel);
                valuesDictionary.SetValue("ShoreRoughness", ShoreRoughness);
                valuesDictionary.SetValue("TerrainBlockIndex", TerrainBlockIndex);
                valuesDictionary.SetValue("TerrainOceanBlockIndex", TerrainOceanBlockIndex);
                valuesDictionary.SetValue("TemperatureOffset", TemperatureOffset);
                valuesDictionary.SetValue("HumidityOffset", HumidityOffset);
                valuesDictionary.SetValue("SeaLevelOffset", SeaLevelOffset);
                valuesDictionary.SetValue("BiomeSize", BiomeSize);
                valuesDictionary.SetValue("StartingPositionMode", StartingPositionMode);
            }
            valuesDictionary.SetValue("BlockTextureName", BlocksTextureName);
            valuesDictionary.SetValue("Palette", Palette.Save());
        }
    }
}