using System.Xml.Linq;
using Engine;

namespace Game {
    public class SettingsScreen : Screen {
        [Obsolete]
        public Screen m_previousScreen;

        public ButtonWidget m_performanceButton;
        public ButtonWidget m_graphicsButton;
        public ButtonWidget m_uiButton;
        public ButtonWidget m_compatibilityButton;
        public ButtonWidget m_audioButton;
        public ButtonWidget m_controlsButton;
        public ButtonWidget m_modSettingsButton;

        public StackPanelWidget m_leftStack, m_rightPanel;
        readonly Dictionary<ButtonWidget, Action> m_buttonActions = new();

        public SettingsScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/SettingsScreen");
            LoadContents(this, node);
            m_performanceButton = Children.Find<ButtonWidget>("Performance");
            m_graphicsButton = Children.Find<ButtonWidget>("Graphics");
            m_uiButton = Children.Find<ButtonWidget>("Ui");
            m_compatibilityButton = Children.Find<ButtonWidget>("Compatibility");
            m_audioButton = Children.Find<ButtonWidget>("Audio");
            m_controlsButton = Children.Find<ButtonWidget>("Controls");
            m_modSettingsButton = Children.Find<ButtonWidget>("ModSettings");
            m_leftStack = Children.Find<StackPanelWidget>("LeftStack");
            m_rightPanel = Children.Find<StackPanelWidget>("RightStack");
            ModsManager.HookAction(
                "OnSettingsScreenCreated",
                loader => {
                    loader.OnSettingsScreenCreated(this, out Dictionary<ButtonWidget, Action> buttonsToAdd);
                    if (buttonsToAdd != null) {
                        foreach (KeyValuePair<ButtonWidget, Action> child in buttonsToAdd) {
                            AddSettingButton(child.Key, child.Value);
                        }
                    }
                    return false;
                }
            );
        }

        /*public override void Enter(object[] parameters) {
            if (m_previousScreen == null) {
                m_previousScreen = ScreensManager.PreviousScreen;
            }
        }*/

        public override void Update() {
            if (m_performanceButton.IsClicked) {
                ScreensManager.SwitchScreen("SettingsPerformance");
            }
            if (m_graphicsButton.IsClicked) {
                ScreensManager.SwitchScreen("SettingsGraphics");
            }
            if (m_uiButton.IsClicked) {
                ScreensManager.SwitchScreen("SettingsUi");
            }
            if (m_compatibilityButton.IsClicked) {
                ScreensManager.SwitchScreen("SettingsCompatibility");
            }
            if (m_audioButton.IsClicked) {
                ScreensManager.SwitchScreen("SettingsAudio");
            }
            if (m_controlsButton.IsClicked) {
                ScreensManager.SwitchScreen("SettingsControls");
            }
            if (m_modSettingsButton.IsClicked) {
                ScreensManager.SwitchScreen("ModSettings");
            }
            foreach (KeyValuePair<ButtonWidget, Action> buttonAction in m_buttonActions) {
                if (buttonAction.Key.IsClicked) {
                    buttonAction.Value?.Invoke();
                }
            }
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                //ScreensManager.SwitchScreen(m_previousScreen);
                //m_previousScreen = null;
                ScreensManager.GoBack();
            }
        }

        /// <summary>
        ///     添加新的设置按钮
        /// </summary>
        /// <param name="button"></param>
        /// <param name="onClicked"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AddSettingButton(ButtonWidget button, Action onClicked) {
            ArgumentNullException.ThrowIfNull(button);
            if (!m_buttonActions.TryAdd(button, onClicked)) {
                throw new InvalidOperationException("Button already has an action assigned");
            }
            int index = m_buttonActions.Count - 1;
            if (index % 2 == 0) {
                m_rightPanel.Children.Add(button);
            }
            else {
                m_leftStack.Children.Add(button);
            }
        }

        /// <summary>
        ///     添加新的设置按钮。使用标准的设置按钮样式，若需要自定义样式请使用另一个重载
        /// </summary>
        /// <param name="text"></param>
        /// <param name="onClicked"></param>
        public void AddSettingButton(string text, Action onClicked) {
            ButtonWidget button = new BevelledButtonWidget {
                Name = text,
                Text = text,
                Style = ContentManager.Get<XElement>("Styles/ButtonStyle_310x60"),
                HorizontalAlignment = WidgetAlignment.Center,
                VerticalAlignment = WidgetAlignment.Center,
                Margin = new Vector2(0f, 5f)
            };
            AddSettingButton(button, onClicked);
        }
    }
}