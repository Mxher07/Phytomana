using System.Xml.Linq;
using Engine.Input;

namespace Game {
    public class GamepadHelpDialog : Dialog {
        public ButtonWidget m_okButton;

        public ButtonWidget m_helpButton;

        public GamepadHelpDialog() {
            XElement node = ContentManager.Get<XElement>("Dialogs/GamepadHelpDialog");
            LoadContents(this, node);
            m_okButton = Children.Find<ButtonWidget>("OkButton");
            m_helpButton = Children.Find<ButtonWidget>("HelpButton");
        }

        public override void Update() {
            m_helpButton.IsVisible = !(ScreensManager.CurrentScreen is HelpScreen);
            if (m_okButton.IsClicked
                || Input.Cancel
                || Input.IsPadButtonDownOnce(GamePadButton.Start)) {
                DialogsManager.HideDialog(this);
            }
            if (m_helpButton.IsClicked) {
                DialogsManager.HideDialog(this);
                ScreensManager.SwitchScreen("Help");
            }
        }
    }
}