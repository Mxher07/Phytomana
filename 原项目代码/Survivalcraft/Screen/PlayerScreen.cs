using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Input;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class PlayerScreen : Screen {
        public enum Mode {
            Initial = 0,
            Add = 1,
            Edit = 2
        }

        public class InputDeviceWidget : LabelWidget {
            public WidgetInputDevice Device;

            public InputDeviceWidget() {
                HorizontalAlignment = WidgetAlignment.Center;
                VerticalAlignment = WidgetAlignment.Center;
            }

            public override void Update() {
                Text = GetDeviceDisplayName(Device);
            }
        }

        public PlayerData m_playerData;
        public Mode m_mode;
        public CharacterSkinsCache m_characterSkinsCache;
        public bool m_nameWasInvalid;
        public const string fName = "PlayerScreen";

        public PlayerModelWidget m_playerModel;
        public ButtonWidget m_playerClassButton;
        public TextBoxWidget m_nameTextBox;
        public LabelWidget m_characterSkinLabel;
        public ButtonWidget m_characterSkinButton;
        public LabelWidget m_controlsLabel;
        public ButtonWidget m_controlsButton;
        public LabelWidget m_descriptionLabel;
        public ButtonWidget m_addButton;
        public ButtonWidget m_addAnotherButton;
        public ButtonWidget m_deleteButton;
        public ButtonWidget m_playButton;

        public static WidgetInputDevice[] m_allInputDevices = [
            WidgetInputDevice.None,
            WidgetInputDevice.Keyboard | WidgetInputDevice.Mouse,
            WidgetInputDevice.GamePad1,
            WidgetInputDevice.GamePad2,
            WidgetInputDevice.GamePad3,
            WidgetInputDevice.GamePad4,
            WidgetInputDevice.VrControllers
        ];

        public static ReadOnlyList<WidgetInputDevice> AllInputDevices => new(m_allInputDevices);

        public PlayerScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/PlayerScreen");
            LoadContents(this, node);
            m_playerModel = Children.Find<PlayerModelWidget>("Model");
            m_playerClassButton = Children.Find<ButtonWidget>("PlayerClassButton");
            m_nameTextBox = Children.Find<TextBoxWidget>("Name");
            m_characterSkinLabel = Children.Find<LabelWidget>("CharacterSkinLabel");
            m_characterSkinButton = Children.Find<ButtonWidget>("CharacterSkinButton");
            m_controlsLabel = Children.Find<LabelWidget>("ControlsLabel");
            m_controlsButton = Children.Find<ButtonWidget>("ControlsButton");
            m_descriptionLabel = Children.Find<LabelWidget>("DescriptionLabel");
            m_addButton = Children.Find<ButtonWidget>("AddButton");
            m_addAnotherButton = Children.Find<ButtonWidget>("AddAnotherButton");
            m_deleteButton = Children.Find<ButtonWidget>("DeleteButton");
            m_playButton = Children.Find<ButtonWidget>("PlayButton");
            m_characterSkinsCache = new CharacterSkinsCache();
            m_playerModel.CharacterSkinsCache = m_characterSkinsCache;
            m_nameTextBox.FocusLost += delegate {
                if (VerifyName()) {
                    m_playerData.Name = m_nameTextBox.Text.Trim();
                }
                else {
                    m_nameWasInvalid = true;
                }
            };
        }

        public override void Enter(object[] parameters) {
            m_mode = (Mode)parameters[0];
            if (m_mode == Mode.Edit) {
                m_playerData = (PlayerData)parameters[1];
            }
            else {
                m_playerData = new PlayerData((Project)parameters[1]);
            }
            if (m_mode == Mode.Initial) {
                m_playerClassButton.IsEnabled = true;
                m_addButton.IsVisible = false;
                m_deleteButton.IsVisible = false;
                m_playButton.IsVisible = true;
                m_addAnotherButton.IsVisible = m_playerData.SubsystemPlayers.PlayersData.Count < 3;
            }
            else if (m_mode == Mode.Add) {
                m_playerClassButton.IsEnabled = true;
                m_addButton.IsVisible = true;
                m_deleteButton.IsVisible = false;
                m_playButton.IsVisible = false;
                m_addAnotherButton.IsVisible = false;
            }
            else if (m_mode == Mode.Edit) {
                m_playerClassButton.IsEnabled = false;
                m_addButton.IsVisible = false;
                m_deleteButton.IsVisible = m_playerData.SubsystemPlayers.PlayersData.Count > 1;
                m_playButton.IsVisible = false;
                m_addAnotherButton.IsVisible = false;
            }
        }

        public override void Leave() {
            m_characterSkinsCache.Clear();
            m_playerData = null;
        }

        public override void Update() {
            m_characterSkinsCache.GetTexture(m_playerData.CharacterSkinName);
            m_playerModel.PlayerClass = m_playerData.PlayerClass;
            m_playerModel.CharacterSkinName = m_playerData.CharacterSkinName;
            m_playerModel.PlayerData = m_playerData;
            m_playerClassButton.Text = m_playerData.PlayerClass.ToString();
            if (!m_nameTextBox.HasFocus) {
                m_nameTextBox.Text = m_playerData.Name;
            }
            m_characterSkinLabel.Text = CharacterSkinsManager.GetDisplayName(m_playerData.CharacterSkinName);
            m_controlsLabel.Text = GetDeviceDisplayName(m_allInputDevices.FirstOrDefault(id => (id & m_playerData.InputDevice) != 0));
            ValuesDictionary valuesDictionary = DatabaseManager.FindValuesDictionaryForComponent(
                DatabaseManager.FindEntityValuesDictionary(m_playerData.GetEntityTemplateName(), true),
                typeof(ComponentCreature)
            );
            string description = valuesDictionary.GetValue<string>("Description");
            if (description.StartsWith('[')
                && description.EndsWith(']')) {
                string[] lp = description.Substring(1, description.Length - 2).Split([":"], StringSplitOptions.RemoveEmptyEntries);
                description = LanguageControl.GetDatabase("Description", lp[1]);
            }
            m_descriptionLabel.Text = description;
            if (m_playerClassButton.IsClicked) {
                m_playerData.PlayerClass = m_playerData.PlayerClass == PlayerClass.Male ? PlayerClass.Female : PlayerClass.Male;
                m_playerData.RandomizeCharacterSkin();
                if (m_playerData.IsDefaultName) {
                    m_playerData.ResetName();
                }
            }
            if (m_characterSkinButton.IsClicked) {
                CharacterSkinsManager.UpdateCharacterSkinsList();
                IEnumerable<string> items = CharacterSkinsManager.CharacterSkinsNames.Where(n =>
                    CharacterSkinsManager.GetPlayerClass(n) == m_playerData.PlayerClass || !CharacterSkinsManager.GetPlayerClass(n).HasValue
                );
                ListSelectionDialog dialog = new(
                    LanguageControl.Get(fName, "1"),
                    items,
                    64f,
                    delegate(object item) {
                        XElement node = ContentManager.Get<XElement>("Widgets/CharacterSkinItem");
                        ContainerWidget containerWidget = (ContainerWidget)LoadWidget(this, node, null);
                        Texture2D texture = m_characterSkinsCache.GetTexture((string)item);
                        containerWidget.Children.Find<LabelWidget>("CharacterSkinItem.Text").Text =
                            CharacterSkinsManager.GetDisplayName((string)item);
                        containerWidget.Children.Find<LabelWidget>("CharacterSkinItem.Details").Text = $"{texture?.Width ?? 0}x{texture?.Height ?? 0}";
                        PlayerModelWidget playerModelWidget = containerWidget.Children.Find<PlayerModelWidget>("CharacterSkinItem.Model");
                        playerModelWidget.PlayerClass = m_playerData.PlayerClass;
                        playerModelWidget.CharacterSkinTexture = texture;
                        return containerWidget;
                    },
                    delegate(object item) {
                        m_playerData.CharacterSkinName = (string)item;
                        if (m_playerData.IsDefaultName) {
                            m_playerData.ResetName();
                        }
                    }
                );
                DialogsManager.ShowDialog(null, dialog);
            }
            if (m_controlsButton.IsClicked) {
                DialogsManager.ShowDialog(
                    null,
                    new ListSelectionDialog(
                        LanguageControl.Get(fName, "2"),
                        m_allInputDevices,
                        56f,
                        d => new InputDeviceWidget { Device = (WidgetInputDevice)d },
                        delegate(object d) {
                            WidgetInputDevice widgetInputDevice = (WidgetInputDevice)d;
                            m_playerData.InputDevice = widgetInputDevice;
                            foreach (PlayerData playersDatum in m_playerData.SubsystemPlayers.PlayersData) {
                                if (playersDatum != m_playerData
                                    && (playersDatum.InputDevice & widgetInputDevice) != 0) {
                                    playersDatum.InputDevice &= ~widgetInputDevice;
                                }
                            }
                        }
                    )
                );
            }
            if (m_addButton.IsClicked
                && VerifyName()) {
                m_playerData.SubsystemPlayers.AddPlayerData(m_playerData);
                ScreensManager.SwitchScreen("Players", m_playerData.SubsystemPlayers);
            }
            if (m_deleteButton.IsClicked) {
                DialogsManager.ShowDialog(
                    null,
                    new MessageDialog(
                        LanguageControl.Warning,
                        LanguageControl.Get(fName, "3"),
                        LanguageControl.Ok,
                        LanguageControl.Cancel,
                        delegate(MessageDialogButton b) {
                            if (b == MessageDialogButton.Button1) {
                                m_playerData.SubsystemPlayers.RemovePlayerData(m_playerData);
                                ScreensManager.SwitchScreen("Players", m_playerData.SubsystemPlayers);
                            }
                        }
                    )
                );
            }
            if (m_playButton.IsClicked
                && VerifyName()) {
                m_playerData.SubsystemPlayers.AddPlayerData(m_playerData);
                ScreensManager.SwitchScreen("Game");
            }
            if (m_addAnotherButton.IsClicked
                && VerifyName()) {
                m_playerData.SubsystemPlayers.AddPlayerData(m_playerData);
                Project project = m_playerData.SubsystemPlayers.Project;
                Leave();
                Enter([Mode.Initial, project]);
            }
            if ((Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
                && VerifyName()) {
                if (m_mode == Mode.Initial) {
                    GameManager.SaveProject(true, true);
                    GameManager.DisposeProject();
                    ScreensManager.SwitchScreen("MainMenu");
                }
                else if (m_mode == Mode.Add
                    || m_mode == Mode.Edit) {
                    ScreensManager.SwitchScreen("Players", m_playerData.SubsystemPlayers);
                }
            }
            m_nameWasInvalid = false;
        }

        public static string GetDeviceDisplayName(WidgetInputDevice device) {
            switch (device) {
                case WidgetInputDevice.Keyboard | WidgetInputDevice.Mouse: return LanguageControl.Get(fName, 4);
                case WidgetInputDevice.GamePad1:
                    return LanguageControl.Get(fName, 5)
                        + (GamePad.IsConnected(0) ? "" : LanguageControl.Get(fName, 9));
                case WidgetInputDevice.GamePad2:
                    return LanguageControl.Get(fName, 6)
                        + (GamePad.IsConnected(1) ? "" : LanguageControl.Get(fName, 9));
                case WidgetInputDevice.GamePad3:
                    return LanguageControl.Get(fName, 7)
                        + (GamePad.IsConnected(2) ? "" : LanguageControl.Get(fName, 9));
                case WidgetInputDevice.GamePad4:
                    return LanguageControl.Get(fName, 8)
                        + (GamePad.IsConnected(3) ? "" : LanguageControl.Get(fName, 9));
                case WidgetInputDevice.VrControllers:
                    return "Virtual Reality" + (VrManager.IsVrAvailable ? "" : " (not found)");
                default: return LanguageControl.Get(fName, 10);
            }
        }

        public bool VerifyName() {
            if (m_nameWasInvalid) {
                return false;
            }
            if (PlayerData.VerifyName(m_nameTextBox.Text.Trim())) {
                return true;
            }
            DialogsManager.ShowDialog(
                null,
                new MessageDialog(
                    LanguageControl.Error,
                    LanguageControl.Get(fName, "12"),
                    LanguageControl.Ok,
                    null,
                    null
                )
            );
            return false;
        }
    }
}