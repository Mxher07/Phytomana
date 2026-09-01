using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemGameInfo : Subsystem, IUpdateable {
        public double? m_lastTotalElapsedGameTime;

        public SubsystemTime m_subsystemTime;

        public SubsystemTimeOfDay m_subsystemTimeOfDay;
        public WorldSettings WorldSettings { get; set; }

        public string DirectoryName { get; set; }

        public double TotalElapsedGameTime { get; set; }

        public float TotalElapsedGameTimeDelta { get; set; }

        public int WorldSeed { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public IEnumerable<ActiveExternalContentInfo> GetActiveExternalContent() {
            string downloadedContentAddress = CommunityContentManager.GetDownloadedContentAddress(ExternalContentType.World, DirectoryName);
            if (!string.IsNullOrEmpty(downloadedContentAddress)) {
                yield return new ActiveExternalContentInfo {
                    Address = downloadedContentAddress, DisplayName = WorldSettings.Name, Type = ExternalContentType.World
                };
            }
            if (!BlocksTexturesManager.IsBuiltIn(WorldSettings.BlocksTextureName)) {
                downloadedContentAddress = CommunityContentManager.GetDownloadedContentAddress(
                    ExternalContentType.BlocksTexture,
                    WorldSettings.BlocksTextureName
                );
                if (!string.IsNullOrEmpty(downloadedContentAddress)) {
                    yield return new ActiveExternalContentInfo {
                        Address = downloadedContentAddress,
                        DisplayName = BlocksTexturesManager.GetDisplayName(WorldSettings.BlocksTextureName),
                        Type = ExternalContentType.BlocksTexture
                    };
                }
            }
            SubsystemPlayers subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
            foreach (PlayerData playersDatum in subsystemPlayers.PlayersData) {
                if (!CharacterSkinsManager.IsBuiltIn(playersDatum.CharacterSkinName)) {
                    downloadedContentAddress = CommunityContentManager.GetDownloadedContentAddress(
                        ExternalContentType.CharacterSkin,
                        playersDatum.CharacterSkinName
                    );
                    if (!string.IsNullOrEmpty(downloadedContentAddress)) {
                        yield return new ActiveExternalContentInfo {
                            Address = downloadedContentAddress,
                            DisplayName = CharacterSkinsManager.GetDisplayName(playersDatum.CharacterSkinName),
                            Type = ExternalContentType.CharacterSkin
                        };
                    }
                }
            }
            SubsystemFurnitureBlockBehavior subsystemFurnitureBlockBehavior = Project.FindSubsystem<SubsystemFurnitureBlockBehavior>(true);
            foreach (FurnitureSet furnitureSet in subsystemFurnitureBlockBehavior.FurnitureSets) {
                if (furnitureSet.ImportedFrom != null) {
                    downloadedContentAddress = CommunityContentManager.GetDownloadedContentAddress(
                        ExternalContentType.FurniturePack,
                        furnitureSet.ImportedFrom
                    );
                    if (!string.IsNullOrEmpty(downloadedContentAddress)) {
                        yield return new ActiveExternalContentInfo {
                            Address = downloadedContentAddress,
                            DisplayName = FurniturePacksManager.GetDisplayName(furnitureSet.ImportedFrom),
                            Type = ExternalContentType.FurniturePack
                        };
                    }
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(true);
            WorldSettings = new WorldSettings();
            WorldSettings.Load(valuesDictionary);
            DirectoryName = valuesDictionary.GetValue<string>("WorldDirectoryName");
            TotalElapsedGameTime = valuesDictionary.GetValue<double>("TotalElapsedGameTime");
            WorldSeed = valuesDictionary.GetValue<int>("WorldSeed");
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            WorldSettings.Save(valuesDictionary, false);
            valuesDictionary.SetValue("WorldSeed", WorldSeed);
            valuesDictionary.SetValue("TotalElapsedGameTime", TotalElapsedGameTime);
        }

        public virtual void Update(float dt) {
            TotalElapsedGameTime += dt;
            TotalElapsedGameTimeDelta = m_lastTotalElapsedGameTime.HasValue ? (float)(TotalElapsedGameTime - m_lastTotalElapsedGameTime.Value) : 0f;
            m_lastTotalElapsedGameTime = TotalElapsedGameTime;
            if (WorldSettings.AreSeasonsChanging
                && m_subsystemTime.PeriodicGameTimeEvent(10.0, 5.0)) {
                float num = WorldSettings.YearDays * m_subsystemTimeOfDay.DayDuration;
                WorldSettings.TimeOfYear = IntervalUtils.Normalize(WorldSettings.TimeOfYear + 10f / num);
            }
            if (m_subsystemTime.GameTime >= 600.0
                && m_subsystemTime.GameTime - m_subsystemTime.GameTimeDelta < 600.0
                && UserManager.ActiveUser != null) {
                foreach (ActiveExternalContentInfo item in GetActiveExternalContent()) {
                    CommunityContentManager.SendPlayTime(
                        item.Address,
                        UserManager.ActiveUser.UniqueId,
                        m_subsystemTime.GameTime,
                        null,
                        delegate { },
                        delegate { }
                    );
                }
            }
        }
    }
}