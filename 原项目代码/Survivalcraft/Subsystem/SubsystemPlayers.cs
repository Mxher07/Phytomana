using System.Globalization;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemPlayers : Subsystem, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public List<PlayerData> m_playersData = [];

        public List<ComponentPlayer> m_componentPlayers = [];

        public int m_nextPlayerIndex;

        public static int MaxPlayers = 4;

        public bool PlayerStartedPlaying = false;

        public ReadOnlyList<PlayerData> PlayersData => new(m_playersData);

        public ReadOnlyList<ComponentPlayer> ComponentPlayers => new(m_componentPlayers);

        public Vector3 GlobalSpawnPosition { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.SubsystemPlayers;

        public virtual Action<PlayerData> PlayerAdded { get; set; }

        public virtual Action<PlayerData> PlayerRemoved { get; set; }

        public bool IsPlayer(Entity entity) {
            foreach (ComponentPlayer componentPlayer in m_componentPlayers) {
                if (entity == componentPlayer.Entity) {
                    return true;
                }
            }
            return false;
        }

        public ComponentPlayer FindNearestPlayer(Vector3 position) {
            ComponentPlayer result = null;
            float num = float.MaxValue;
            foreach (ComponentPlayer componentPlayer in ComponentPlayers) {
                float num2 = Vector3.DistanceSquared(componentPlayer.ComponentBody.Position, position);
                if (num2 < num) {
                    num = num2;
                    result = componentPlayer;
                }
            }
            return result;
        }

        public void AddPlayerData(PlayerData playerData) {
            if (m_playersData.Count >= MaxPlayers) {
                throw new InvalidOperationException("Too many players.");
            }
            if (m_playersData.Contains(playerData)) {
                throw new InvalidOperationException("Player already added.");
            }
            m_playersData.Add(playerData);
            playerData.PlayerIndex = ++m_nextPlayerIndex;
            PlayerAdded?.Invoke(playerData);
        }

        public void RemovePlayerData(PlayerData playerData) {
            if (!m_playersData.Contains(playerData)) {
                throw new InvalidOperationException("Player does not exist.");
            }
            m_playersData.Remove(playerData);
            if (playerData.ComponentPlayer != null) {
                Project.RemoveEntity(playerData.ComponentPlayer.Entity, true);
            }
            PlayerRemoved?.Invoke(playerData);
            playerData.Dispose();
        }

        public virtual void Update(float dt) {
            if (m_playersData.Count == 0) {
                ScreensManager.SwitchScreen("Player", PlayerScreen.Mode.Initial, Project);
            }
            foreach (PlayerData playersDatum in m_playersData) {
                playersDatum.Update();
            }
        }

        public override void Dispose() {
            foreach (PlayerData playersDatum in m_playersData) {
                playersDatum.Dispose();
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_nextPlayerIndex = valuesDictionary.GetValue<int>("NextPlayerIndex");
            GlobalSpawnPosition = valuesDictionary.GetValue<Vector3>("GlobalSpawnPosition");
            foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue<ValuesDictionary>("Players")) {
                PlayerData playerData = new(Project);
                playerData.Load((ValuesDictionary)item.Value);
                playerData.PlayerIndex = int.Parse(item.Key, CultureInfo.InvariantCulture);
                m_playersData.Add(playerData);
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            valuesDictionary.SetValue("NextPlayerIndex", m_nextPlayerIndex);
            valuesDictionary.SetValue("GlobalSpawnPosition", GlobalSpawnPosition);
            ValuesDictionary valuesDictionary2 = new();
            valuesDictionary.SetValue("Players", valuesDictionary2);
            foreach (PlayerData playersDatum in m_playersData) {
                ValuesDictionary valuesDictionary3 = new();
                valuesDictionary2.SetValue(playersDatum.PlayerIndex.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
                playersDatum.Save(valuesDictionary3);
            }
        }

        public override void OnEntityAdded(Entity entity) {
            foreach (PlayerData playersDatum in m_playersData) {
                playersDatum.OnEntityAdded(entity);
            }
            UpdateComponentPlayers();
        }

        public override void OnEntityRemoved(Entity entity) {
            foreach (PlayerData playersDatum in m_playersData) {
                playersDatum.OnEntityRemoved(entity);
            }
            UpdateComponentPlayers();
        }

        public virtual void UpdateComponentPlayers() {
            m_componentPlayers.Clear();
            foreach (PlayerData playersDatum in m_playersData) {
                if (playersDatum.ComponentPlayer != null) {
                    m_componentPlayers.Add(playersDatum.ComponentPlayer);
                }
            }
        }
    }
}