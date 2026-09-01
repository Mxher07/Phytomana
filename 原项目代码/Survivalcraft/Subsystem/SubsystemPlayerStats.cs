using System.Globalization;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemPlayerStats : Subsystem {
        public Dictionary<int, PlayerStats> m_playerStats = [];

        public PlayerStats GetPlayerStats(int playerIndex) {
            if (!m_playerStats.TryGetValue(playerIndex, out PlayerStats value)) {
                value = new PlayerStats();
                m_playerStats.Add(playerIndex, value);
            }
            return value;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue<ValuesDictionary>("Stats")) {
                PlayerStats playerStats = new();
                playerStats.Load((ValuesDictionary)item.Value);
                m_playerStats.Add(int.Parse(item.Key, CultureInfo.InvariantCulture), playerStats);
            }
            // 根据已启用模组中最高的GameplayImpactLevel设置所有玩家的该字段
            if (!SettingsManager.SafeMode) {
                GameplayImpactLevel highestImpactLevel = GameplayImpactLevel.Cosmetic;
                foreach (ModEntity modEntity in ModsManager.ModList) {
                    if (modEntity.modInfo != null
                        && modEntity.modInfo.GameplayImpactLevel > highestImpactLevel) {
                        highestImpactLevel = modEntity.modInfo.GameplayImpactLevel;
                    }
                }
                foreach (PlayerStats stats in m_playerStats.Values) {
                    if ((int)highestImpactLevel > stats.HighestGameplayImpactLevel) {
                        stats.HighestGameplayImpactLevel = (int)highestImpactLevel;
                    }
                }
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            ValuesDictionary valuesDictionary2 = new();
            valuesDictionary.SetValue("Stats", valuesDictionary2);
            foreach (KeyValuePair<int, PlayerStats> playerStat in m_playerStats) {
                ValuesDictionary valuesDictionary3 = new();
                valuesDictionary2.SetValue(playerStat.Key.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
                playerStat.Value.Save(valuesDictionary3);
            }
        }
    }
}