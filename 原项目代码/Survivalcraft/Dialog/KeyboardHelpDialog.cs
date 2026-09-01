using System.Xml.Linq;
using Engine.Input;

namespace Game {
    public class KeyboardHelpDialog : Dialog {
        public ButtonWidget m_okButton;

        public ButtonWidget m_helpButton;

        public KeyboardHelpDialog() {
            XElement node = ContentManager.Get<XElement>("Dialogs/KeyboardHelpDialog");
            LoadContents(this, node);
            m_okButton = Children.Find<ButtonWidget>("OkButton");
            m_helpButton = Children.Find<ButtonWidget>("HelpButton");
        }

        public override void Update() {
            m_helpButton.IsVisible = !(ScreensManager.CurrentScreen is HelpScreen);
            if (m_okButton.IsClicked
                || Input.Cancel
                || Input.IsKeyDownOnce(Key.H)) {
                DialogsManager.HideDialog(this);
            }
            if (m_helpButton.IsClicked) {
                DialogsManager.HideDialog(this);
                ScreensManager.SwitchScreen("Help");
            }
        }
    }
}