namespace Game {
    public static class MarketplaceManager {
        public static bool m_isInitialized;

        public static bool m_isTrialMode;

        public static bool IsTrialMode {
            get => m_isTrialMode;
            set => m_isTrialMode = value;
        }

        public static void Initialize() {
            m_isInitialized = true;
        }

        public static void ShowMarketplace() {
            switch (VersionsManager.PlatformString) {
                case "Windows": WebBrowserManager.LaunchBrowser("https://apps.microsoft.com/detail/9PHC48P58NB2"); break;
                case "Android":
                    WebBrowserManager.LaunchBrowser("http://play.google.com/store/apps/details?id=com.candyrufusgames.survivalcraft2"); break;
            }
        }
    }
}