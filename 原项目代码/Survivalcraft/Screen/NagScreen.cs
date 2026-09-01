using System.Xml.Linq;
using Engine;
using Engine.Input;

namespace Game {
    public class NagScreen : Screen {
        public NagScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/NagScreen");
            LoadContents(this, node);
        }

        public override void Enter(object[] parameters) {
            Keyboard.BackButtonQuitsApp = true;
            Children.Find<Widget>("Quit").IsVisible = true;
            Children.Find<Widget>("QuitLabel_Wp81").IsVisible = false;
            Children.Find<Widget>("QuitLabel_Win81").IsVisible = false;
        }

        public override void Leave() {
            Keyboard.BackButtonQuitsApp = false;
        }

        public override void Update() {
            if (Children.Find<ButtonWidget>("Buy").IsClicked) {
                MarketplaceManager.ShowMarketplace();
                ScreensManager.SwitchScreen("MainMenu");
            }
            if (Children.Find<ButtonWidget>("Quit").IsClicked
                || Input.Back) {
                Window.Close();
            }
        }
    }
}