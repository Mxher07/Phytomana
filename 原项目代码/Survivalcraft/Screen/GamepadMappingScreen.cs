using System.Xml.Linq;
using Engine;
using Engine.Input;
using Engine.Serialization;
using TemplatesDatabase;

namespace Game {
    public class GamepadMappingScreen : Screen {
        public Widget GamepadKeyInfoWidget(object item) {
            XElement node = ContentManager.Get<XElement>("Widgets/KeyboardMappingItem");
            node.SetAttributeValue("Name", $"GamepadMappingItem_{item}");
            ContainerWidget containerWidget = (ContainerWidget)LoadWidget(this, node, null);
            LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("Name");
            LabelWidget labelWidget2 = containerWidget.Children.Find<LabelWidget>("BoundKey");
            string itemString = item.ToString();
            if (itemString != null) {
                string translated = LanguageControl.Get(out bool r, fName, itemString);
                labelWidget.Text = r ? translated : itemString;
                object value = SettingsManager.GetGamepadMapping(itemString);
                if (value is GamePadButton valueKey
                    && valueKey == GamePadButton.Null) {
                    labelWidget2.Text = string.Empty;
                }
                else if (value is ValuesDictionary valuesDictionary) {
                    labelWidget2.Text = ConvertGamepadName(valuesDictionary.GetValue<object>("ModifierKey", null))
                        + " + "
                        + ConvertGamepadName(valuesDictionary.GetValue<object>("ActionKey", null));
                }
                else {
                    labelWidget2.Text = ConvertGamepadName(value);
                }
                m_widgetsByString[itemString] = containerWidget;
            }
            return containerWidget;
        }

        public static string ConvertGamepadName(object obj) {
            if (obj == null) {
                return string.Empty;
            }
            string text = HumanReadableConverter.ConvertToString(obj);
            string translated = LanguageControl.Get(out bool r, keyName, text);
            return r ? translated : text;
        }

        public static string fName => typeof(KeyboardMappingScreen).Name;
        public static string keyName => "GamepadMappingScreenKeys";

        public ListPanelWidget m_keysList;
        public BevelledButtonWidget m_resetButton;
        public BevelledButtonWidget m_setKeyButton;
        public BevelledButtonWidget m_disableKeyButton;
        public BevelledButtonWidget m_gameHelpButton;
        public bool IsWaitingForKeyInput;
        public Dictionary<string, ContainerWidget> m_widgetsByString = [];
        public Dictionary<object, List<string>> m_conflicts = [];

        public GamepadMappingScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/KeyboardMappingScreen");
            LoadContents(this, node);
            m_keysList = Children.Find<ListPanelWidget>("KeysList");
            m_keysList.ItemWidgetFactory = (Func<object, Widget>)Delegate.Combine(m_keysList.ItemWidgetFactory, GamepadKeyInfoWidget);
            m_keysList.ScrollPosition = 0f;
            m_keysList.ScrollSpeed = 0f;
            m_keysList.ItemClicked += item => { m_keysList.SelectedItem = m_keysList.SelectedItem == item ? null : item; };
            m_resetButton = Children.Find<BevelledButtonWidget>("Reset");
            m_setKeyButton = Children.Find<BevelledButtonWidget>("SetKey");
            m_disableKeyButton = Children.Find<BevelledButtonWidget>("DisableKey");
            m_gameHelpButton = Children.Find<BevelledButtonWidget>("GameHelp");
        }

        public override void Update() {
            string selectedKeyName = m_keysList.SelectedItem?.ToString() ?? string.Empty;
            m_setKeyButton.IsEnabled = !string.IsNullOrEmpty(selectedKeyName);
            m_disableKeyButton.IsEnabled = !string.IsNullOrEmpty(selectedKeyName);
            if (Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
                return;
            }
            foreach (KeyValuePair<string, ContainerWidget> item in m_widgetsByString) {
                string key = item.Key;
                LabelWidget labelWidget = item.Value.Children.Find<LabelWidget>("BoundKey");
                object value = SettingsManager.GetGamepadMapping(key);
                if (value is GamePadButton valueKey
                    && valueKey == GamePadButton.Null) {
                    labelWidget.Text = string.Empty;
                }
                else {
                    if (value is ValuesDictionary valuesDictionary) {
                        labelWidget.Text = ConvertGamepadName(valuesDictionary.GetValue<object>("ModifierKey", null))
                            + " + "
                            + ConvertGamepadName(valuesDictionary.GetValue<object>("ActionKey", null));
                    }
                    else {
                        labelWidget.Text = ConvertGamepadName(value); // r ? translated : text;
                    }
                    bool hasConflict = false;
                    if (m_conflicts.TryGetValue(value, out List<string> valueList)) {
                        hasConflict = KeyCompatibleGroupsManager.HasConflict(valueList);
                    }
                    labelWidget.Color = hasConflict ? Color.Red : Color.White;
                }
            }
            if (m_disableKeyButton.IsClicked) {
                SetGamepadMapping(selectedKeyName, GamePadButton.Null);
                IsWaitingForKeyInput = false;
            }
            if (m_resetButton.IsClicked) {
                MessageDialog dialog = new(
                    LanguageControl.Get("ContentWidgets", fName, "ResetTitle"),
                    LanguageControl.Get("ContentWidgets", fName, "ResetText"),
                    LanguageControl.Yes,
                    LanguageControl.No,
                    delegate(MessageDialogButton button) {
                        if (button == MessageDialogButton.Button1) { //重设所有按键
                            ResetAll();
                        }
                    }
                );
                DialogsManager.ShowDialog(null, dialog);
                IsWaitingForKeyInput = false;
            }
            if (IsWaitingForKeyInput) {
                m_setKeyButton.IsChecked = true;
                if (Input.Back) {
                    IsWaitingForKeyInput = false;
                    return;
                }
                object holdingModifierKey = null;
                if (Input.IsPadButtonDown(GamePadButton.LeftShoulder)) {
                    holdingModifierKey = GamePadButton.LeftShoulder;
                }
                else if (Input.IsPadButtonDown(GamePadButton.RightShoulder)) {
                    holdingModifierKey = GamePadButton.RightShoulder;
                }
                else if (Input.IsPadTriggerDown(GamePadTrigger.Left, 0f, SettingsManager.GamepadTriggerThreshold)) {
                    holdingModifierKey = GamePadTrigger.Left;
                }
                else if (Input.IsPadTriggerDown(GamePadTrigger.Right, 0f, SettingsManager.GamepadTriggerThreshold)) {
                    holdingModifierKey = GamePadTrigger.Right;
                }
                foreach (GamePadButton button in EnumUtils.GetEnumValues<GamePadButton>().Select(v => (GamePadButton)v)) {
                    if (button != GamePadButton.Null
                        && Input.IsPadButtonDownOnce(button)) {
                        if (holdingModifierKey != null
                            && !GamePad.IsModifierKey(button)) {
                            ValuesDictionary combinedKey = [];
                            combinedKey.SetValue("ModifierKey", holdingModifierKey);
                            combinedKey.SetValue("ActionKey", button);
                            SetGamepadMapping(selectedKeyName, combinedKey);
                        }
                        else {
                            SetGamepadMapping(selectedKeyName, button);
                        }
                        IsWaitingForKeyInput = false;
                        return;
                    }
                }
                foreach (GamePadTrigger trigger in EnumUtils.GetEnumValues<GamePadTrigger>().Select(v => (GamePadTrigger)v)) {
                    if (Input.IsTriggerDownOnce(trigger)) {
                        SetGamepadMapping(selectedKeyName, trigger);
                        IsWaitingForKeyInput = false;
                        return;
                    }
                }
            }
            else {
                m_setKeyButton.IsChecked = false;
            }
            if (m_setKeyButton.IsClicked) {
                IsWaitingForKeyInput = true;
            }
            if (m_gameHelpButton.IsClicked) {
                ScreensManager.SwitchScreen("Help");
            }
            if (!IsWaitingForKeyInput
                && (Input.Back || Input.Cancel)) {
                ScreensManager.GoBack();
            }
        }

        public override void Enter(object[] parameters) {
            m_gameHelpButton.IsVisible = ScreensManager.PreviousScreen is GameScreen;
            m_keysList.ClearItems();
            foreach (string keyName1 in ModSettingsManager.CombinedGamepadMappingSettings.Keys) {
                m_keysList.AddItem(keyName1);
            }
            RefreshConflicts();
        }

        public void SetGamepadMapping(string keyName1, object value) {
            SettingsManager.SetGamepadMapping(keyName1, value);
            RefreshConflicts();
        }

        public void ResetAll() {
            SettingsManager.InitializeGamepadMappingSettings();
            ModSettingsManager.ResetModsGamepadMappingSettings();
            RefreshConflicts();
        }

        public void RefreshConflicts() {
            m_conflicts.Clear();
            foreach (KeyValuePair<string, object> item in ModSettingsManager.CombinedGamepadMappingSettings) {
                string name = item.Key;
                object obj = item.Value;
                if (!m_conflicts.TryGetValue(obj, out List<string> value)) {
                    value = [];
                    m_conflicts[obj] = value;
                }
                if (!value.Contains(name)) {
                    value.Add(name);
                }
            }
        }
    }
}