using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemGameWidgets : Subsystem, IUpdateable {
        public int MaxGameWidgets => SubsystemPlayers.MaxPlayers;

        public SubsystemPlayers m_subsystemPlayers;

        public List<GameWidget> m_gameWidgets = [];

        public virtual GamesWidget GamesWidget { get; set; }

        public virtual ReadOnlyList<GameWidget> GameWidgets => new(m_gameWidgets);

        public virtual SubsystemTerrain SubsystemTerrain { get; set; }

        public virtual UpdateOrder UpdateOrder => UpdateOrder.Views;

        public virtual float CalculateSquaredDistanceFromNearestView(Vector3 p) {
            float num = float.MaxValue;
            foreach (GameWidget gameWidget in m_gameWidgets) {
                float num2 = Vector3.DistanceSquared(p, gameWidget.ActiveCamera.ViewPosition);
                if (num2 < num) {
                    num = num2;
                }
            }
            return num;
        }

        public virtual float CalculateDistanceFromNearestView(Vector3 p) => MathF.Sqrt(CalculateSquaredDistanceFromNearestView(p));

        public virtual void Update(float dt) {
            foreach (GameWidget gameWidget in GameWidgets) {
                gameWidget.ActiveCamera.Update(Time.FrameDuration);
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
            SubsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemPlayers.PlayerAdded += delegate(PlayerData playerData) { AddGameWidgetForPlayer(playerData); };
            m_subsystemPlayers.PlayerRemoved += delegate(PlayerData playerData) {
                if (playerData.GameWidget != null) {
                    RemoveGameWidget(playerData.GameWidget);
                }
            };
            GamesWidget = valuesDictionary.GetValue<GamesWidget>("GamesWidget");
            foreach (PlayerData playersDatum in m_subsystemPlayers.PlayersData) {
                AddGameWidgetForPlayer(playersDatum);
            }
        }

        public override void Dispose() {
            GameWidget[] array = GameWidgets.ToArray();
            foreach (GameWidget gameWidget in array) {
                RemoveGameWidget(gameWidget);
                gameWidget.Dispose();
            }
        }

        public virtual void AddGameWidgetForPlayer(PlayerData playerData) {
            int index = 0;
            while (index < MaxGameWidgets
                && m_gameWidgets.FirstOrDefault(v => v.GameWidgetIndex == index) != null) {
                index++;
            }
            if (index >= MaxGameWidgets) {
                throw new InvalidOperationException("Too many GameWidgets.");
            }
            GameWidget gameWidget = new(playerData, index);
            m_gameWidgets.Add(gameWidget);
            GamesWidget.Children.Add(gameWidget);
        }

        public virtual void RemoveGameWidget(GameWidget gameWidget) {
            m_gameWidgets.Remove(gameWidget);
            GamesWidget.Children.Remove(gameWidget);
        }
    }
}