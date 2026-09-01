using System.Xml.Linq;

namespace Game {
    public class PlayerWidget : CanvasWidget {
        public PlayerModelWidget m_playerModel;
        public LabelWidget m_nameLabel;
        public LabelWidget m_detailsLabel;
        public ButtonWidget m_editButton;

        public PlayerData m_playerData;
        public SubsystemTimeOfDay m_subsystemTimeOfDay;
        public const string fName = "PlayerWidget";

        public PlayerWidget(PlayerData playerData, CharacterSkinsCache characterSkinsCache) {
            XElement node = ContentManager.Get<XElement>("Widgets/PlayerWidget");
            LoadContents(this, node);
            m_playerModel = Children.Find<PlayerModelWidget>("PlayerModel");
            m_nameLabel = Children.Find<LabelWidget>("Name");
            m_detailsLabel = Children.Find<LabelWidget>("Details");
            m_editButton = Children.Find<ButtonWidget>("EditButton");
            m_playerModel.CharacterSkinsCache = characterSkinsCache;
            m_playerData = playerData;
        }

        public override void Update() {
            SubsystemGameInfo subsystemGameInfo = m_playerData.SubsystemPlayers.Project.FindSubsystem<SubsystemGameInfo>(true);
            m_playerModel.PlayerClass = m_playerData.PlayerClass;
            m_playerModel.CharacterSkinName = m_playerData.CharacterSkinName;
            m_playerModel.PlayerData = m_playerData;
            m_nameLabel.Text = m_playerData.Name;
            m_detailsLabel.Text = m_playerData.PlayerClass.ToString();
            m_detailsLabel.Text += "\n";
            LabelWidget detailsLabel = m_detailsLabel;
            detailsLabel.Text = string.Format(
                LanguageControl.Get(fName, 0),
                detailsLabel.Text,
                PlayerScreen.GetDeviceDisplayName(m_playerData.InputDevice)
            );
            m_detailsLabel.Text += "\n";
            float time = 1200f;
            if (m_subsystemTimeOfDay == null
                && m_playerData.m_subsystemGameInfo != null) {
                m_subsystemTimeOfDay = m_playerData.m_subsystemGameInfo.Project.FindSubsystem<SubsystemTimeOfDay>();
            }
            if (m_subsystemTimeOfDay != null) {
                time = m_subsystemTimeOfDay.DayDuration;
            }
            m_detailsLabel.Text += m_playerData.LastSpawnTime >= 0.0
                ? string.Format(
                    LanguageControl.Get(fName, 1),
                    $"{(subsystemGameInfo.TotalElapsedGameTime - m_playerData.LastSpawnTime) / time:N1}"
                )
                : LanguageControl.Get(fName, 2);
            if (m_editButton.IsClicked) {
                ScreensManager.SwitchScreen("Player", PlayerScreen.Mode.Edit, m_playerData);
            }
        }
    }
}