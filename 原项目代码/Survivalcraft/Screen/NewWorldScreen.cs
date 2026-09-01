using System.Xml.Linq;

namespace Game {
    public class NewWorldScreen : Screen {
        public TextBoxWidget m_nameTextBox;

        public TextBoxWidget m_seedTextBox;

        public ButtonWidget m_gameModeButton;

        public ButtonWidget m_startingPositionButton;

        public ButtonWidget m_worldOptionsButton;

        public LabelWidget m_blankSeedLabel;

        public ButtonWidget m_seedButton;

        public LabelWidget m_descriptionLabel;

        public LabelWidget m_errorLabel;

        public ButtonWidget m_playButton;

        public Random m_random = new();

        public WorldSettings m_worldSettings;

        public const string fName = "NewWorldScreen";

        public NewWorldScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/NewWorldScreen");
            LoadContents(this, node);
            m_nameTextBox = Children.Find<TextBoxWidget>("Name");
            m_seedTextBox = Children.Find<TextBoxWidget>("Seed");
            m_gameModeButton = Children.Find<ButtonWidget>("GameMode");
            m_startingPositionButton = Children.Find<ButtonWidget>("StartingPosition");
            m_worldOptionsButton = Children.Find<ButtonWidget>("WorldOptions");
            m_blankSeedLabel = Children.Find<LabelWidget>("BlankSeed");
            m_seedButton = Children.Find<ButtonWidget>("SeedButton");
            m_descriptionLabel = Children.Find<LabelWidget>("Description");
            m_errorLabel = Children.Find<LabelWidget>("Error");
            m_playButton = Children.Find<ButtonWidget>("Play");
            m_nameTextBox.TextChanged += delegate { m_worldSettings.Name = m_nameTextBox.Text; };
            m_nameTextBox.MaximumLength = 128;
            m_seedTextBox.TextChanged += delegate { m_worldSettings.Seed = m_seedTextBox.Text; };
        }

        public override void Enter(object[] parameters) {
            if (!(ScreensManager.PreviousScreen is IWorldOptionsScreen)) {
                m_worldSettings = new WorldSettings {
                    Name = WorldsManager.NewWorldNames[m_random.Int(0, WorldsManager.NewWorldNames.Count - 1)],
                    OriginalSerializationVersion = VersionsManager.SerializationVersion
                };
            }
        }

        public override void Update() {
            if (m_seedButton.IsClicked) {
                DialogsManager.ShowDialog(
                    null,
                    new EditWorldSeedDialog(
                        m_worldSettings.CustomWorldSeed ? null : m_seedTextBox.Text,
                        m_worldSettings.WorldSeed,
                        (seed, trueSeed) => {
                            if (seed == null) {
                                m_worldSettings.CustomWorldSeed = true;
                                m_worldSettings.WorldSeed = trueSeed;
                                m_seedTextBox.IsEnabled = false;
                                m_worldSettings.Seed = String.Empty;
                            }
                            else {
                                m_worldSettings.CustomWorldSeed = false;
                                m_worldSettings.WorldSeed = 0;
                                m_seedTextBox.IsEnabled = true;
                                m_seedTextBox.Text = seed;
                            }
                        }
                    )
                );
            }
            if (m_gameModeButton.IsClicked) {
                DialogsManager.ShowDialog(
                    null,
                    new SelectGameModeDialog(string.Empty, false, true, delegate(GameMode gameMode) { m_worldSettings.GameMode = gameMode; })
                );
            }
            if (m_startingPositionButton.IsClicked) {
                IList<int> enumValues2 = EnumUtils.GetEnumValues<StartingPositionMode>();
                m_worldSettings.StartingPositionMode = (StartingPositionMode)((enumValues2.IndexOf((int)m_worldSettings.StartingPositionMode) + 1)
                    % enumValues2.Count);
            }
            bool flag = WorldsManager.ValidateWorldName(m_worldSettings.Name);
            m_nameTextBox.Text = m_worldSettings.Name;
            m_seedTextBox.Text = m_worldSettings.Seed;
            m_gameModeButton.Text = LanguageControl.Get("GameMode", m_worldSettings.GameMode.ToString());
            m_startingPositionButton.Text = LanguageControl.Get("StartingPositionMode", m_worldSettings.StartingPositionMode.ToString());
            m_playButton.IsVisible = flag;
            m_errorLabel.IsVisible = !flag;
            m_blankSeedLabel.IsVisible = string.IsNullOrEmpty(m_worldSettings.Seed) && !m_seedTextBox.HasFocus;
            m_blankSeedLabel.Text = m_worldSettings.CustomWorldSeed
                ? string.Format(LanguageControl.Get(fName, "2"), m_worldSettings.WorldSeed)
                : LanguageControl.Get(fName, "1");
            m_descriptionLabel.Text = StringsManager.GetString($"GameMode.{m_worldSettings.GameMode}.Description");
            if (m_worldOptionsButton.IsClicked) {
                ScreensManager.SwitchScreen("WorldOptions", m_worldSettings, false);
            }
            if (m_playButton.IsClicked
                && WorldsManager.ValidateWorldName(m_nameTextBox.Text)) {
                if (m_worldSettings.GameMode != 0) {
                    m_worldSettings.ResetOptionsForNonCreativeMode(null);
                }
                WorldInfo worldInfo = WorldsManager.CreateWorld(m_worldSettings);
                ScreensManager.SwitchScreen("GameLoading", worldInfo, null);
            }
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen("Play");
            }
        }
    }
}