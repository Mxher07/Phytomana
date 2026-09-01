using System.Xml.Linq;

namespace Game {
    public class TrialEndedScreen : Screen {
        public ButtonWidget m_buyButton;

        public ButtonWidget m_quitButton;

        public ButtonWidget m_newWorldButton;

        public TrialEndedScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/TrialEndedScreen");
            LoadContents(this, node);
            m_buyButton = Children.Find<ButtonWidget>("Buy", false);
            m_quitButton = Children.Find<ButtonWidget>("Quit", false);
            m_newWorldButton = Children.Find<ButtonWidget>("NewWorld", false);
        }

        public override void Update() {
            if (m_buyButton != null
                && m_buyButton.IsClicked) {
                MarketplaceManager.ShowMarketplace();
                ScreensManager.SwitchScreen("MainMenu");
            }
            if ((m_quitButton != null && m_quitButton.IsClicked)
                || Input.Back) {
                ScreensManager.SwitchScreen("MainMenu");
            }
            if (m_newWorldButton != null
                && m_newWorldButton.IsClicked) {
                ScreensManager.SwitchScreen("NewWorld");
            }
        }
    }
}