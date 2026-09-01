using Foundation;
using GLKit;
using UIKit;

namespace SurvivalCraft.IOS {
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate {
        // class-level declarations

        public override UIWindow Window {
            get;
            set;
        }

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions) {
            Window = new UIWindow();
            Window.RootViewController = new GameViewController();
            Window.MakeKeyAndVisible();

            return true;
        }
        public override bool HandleOpenURL(UIApplication application, NSUrl url) {
            return base.HandleOpenURL(application, url);
        }
    }
}
