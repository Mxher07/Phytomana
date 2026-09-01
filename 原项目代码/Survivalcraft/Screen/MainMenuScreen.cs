using System.Xml.Linq;
using Engine;
using Engine.Input;

namespace Game {
    public class MainMenuScreen : Screen {
        public string m_versionString = string.Empty;

        public bool m_versionStringTrial;
        public bool m_disabledModsNotShowed = true;

        public ButtonWidget m_disableSafeModeButton;
        public ButtonWidget m_showBulletinButton;
        public LabelWidget m_copyrightLabel;
        public ButtonWidget m_languageSwitchButton;
        public ButtonWidget m_updateCheckButton;
        public Subtexture m_needToUpdateIcon;
        public Subtexture m_dontNeedUpdateIcon;
        public RectangleWidget m_updateButtonIcon;
        public ButtonWidget m_fullscreenButton;
        public ButtonWidget m_dashboardButton;
        public StackPanelWidget m_leftBottomBar;
        public StackPanelWidget m_rightBottomBar;

        public const string fName = "MainMenuScreen";

        public MainMenuScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/MainMenuScreen");
            LoadContents(this, node);
            m_disableSafeModeButton = Children.Find<ButtonWidget>("DisableSafeModeButton");
            m_showBulletinButton = Children.Find<ButtonWidget>("BulletinButton", false);
            m_copyrightLabel = Children.Find<LabelWidget>("CopyrightLabel", false);
            m_languageSwitchButton = Children.Find<ButtonWidget>("LanguageSwitchButton", false);
            m_leftBottomBar = Children.Find<StackPanelWidget>("LeftBottomBar", false);
            m_rightBottomBar = Children.Find<StackPanelWidget>("RightBottomBar", false);
            m_updateCheckButton = Children.Find<ButtonWidget>("UpdateCheckButton", false);
            m_updateButtonIcon = Children.Find<RectangleWidget>("UpdateIcon", false);
            m_needToUpdateIcon = ContentManager.Get<Subtexture>("Textures/Gui/NeedToUpdate");
            m_dontNeedUpdateIcon = ContentManager.Get<Subtexture>("Textures/Gui/UpdateChecking");
            m_fullscreenButton = Children.Find<ButtonWidget>("FullscreenButton", false);
            m_dashboardButton = Children.Find<ButtonWidget>("DashboardButton", false);
#if BROWSER
            m_fullscreenButton.IsVisible = true;
            m_dashboardButton.IsVisible = true;
#endif
            ModsManager.HookAction(
                "OnMainMenuScreenCreated",
                loader => {
                    loader.OnMainMenuScreenCreated(this, m_leftBottomBar, m_rightBottomBar);
                    return false;
                }
            );
        }

        public override void Enter(object[] parameters) {
            MusicManager.CurrentMix = MusicManager.Mix.Menu;
            Children.Find<MotdWidget>().Restart();
            if (SettingsManager.IsolatedStorageMigrationCounter < 3) {
                SettingsManager.IsolatedStorageMigrationCounter++;
                VersionConverter126To127.MigrateDataFromIsolatedStorageWithDialog();
            }
            if (MotdManager.CanShowBulletin) {
                MotdManager.ShowBulletin();
            }
            if (SettingsManager.SafeMode) {
                m_disableSafeModeButton.IsVisible = true;
            }
            m_leftBottomBar.MarginLeft = SettingsManager.AdaptEdgeToEdgeDisplay ? Window.DisplayCutoutInsets.X * ScreensManager.FinalUiScale : 0f;
            m_rightBottomBar.MarginRight = SettingsManager.AdaptEdgeToEdgeDisplay ? Window.DisplayCutoutInsets.Z * ScreensManager.FinalUiScale : 0f;
            if (m_disabledModsNotShowed) {
                m_disabledModsNotShowed = false;
                ModsManager.ShowDisabledModsDialog();
            }
        }

        public override void Leave() {
            Keyboard.BackButtonQuitsApp = false;
        }

        public override void Update() {
            Keyboard.BackButtonQuitsApp = !MarketplaceManager.IsTrialMode;
            if (string.IsNullOrEmpty(m_versionString)
                || MarketplaceManager.IsTrialMode != m_versionStringTrial) {
                m_versionString = $"Version {VersionsManager.Version}{(MarketplaceManager.IsTrialMode ? " (Day One)" : string.Empty)}";
                m_versionStringTrial = MarketplaceManager.IsTrialMode;
            }
            Children.Find("Buy").IsVisible = MarketplaceManager.IsTrialMode;
            Children.Find<LabelWidget>("Version").Text = $"{m_versionString} -  API {ModsManager.APIVersionString}";
            RectangleWidget rectangleWidget = Children.Find<RectangleWidget>("Logo");
            float num = 1f + 0.02f * MathF.Sin(1.5f * (float)MathUtils.Remainder(Time.FrameStartTime, 10000.0));
            rectangleWidget.RenderTransform =
                Matrix.CreateTranslation((0f - rectangleWidget.ActualSize.X) / 2f, (0f - rectangleWidget.ActualSize.Y) / 2f, 0f)
                * Matrix.CreateScale(num, num, 1f)
                * Matrix.CreateTranslation(rectangleWidget.ActualSize.X / 2f, rectangleWidget.ActualSize.Y / 2f, 0f);
            if (m_languageSwitchButton?.IsClicked ?? false) {
                LanguageControl.CreateLanguageSelectionDialog(null);
            }
            //更新控制
            if (!APIUpdateManager.IsNeedUpdate.HasValue) {
                float angle = (float)Time.RealTime * 2; //获取更新时旋转图标
                float scale = (angle + MathF.PI / 4) / (MathF.PI / 2);
                scale -= MathF.Round(scale);
                scale *= MathF.PI / 2;
                scale = new Vector2(1, MathF.Tan(scale)).Length() / MathF.Sqrt(2);
                if (m_updateButtonIcon != null) {
                    m_updateButtonIcon.LayoutTransform = Matrix.CreateRotationZ(angle) * Matrix.CreateScale(scale);
                    m_updateButtonIcon.FillColor = Color.White;
                }
            }
            else {
                if (m_updateButtonIcon != null) {
                    m_updateButtonIcon.LayoutTransform = Matrix.CreateRotationZ(0) * Matrix.CreateScale(1);
                    m_updateButtonIcon.Subtexture = APIUpdateManager.IsNeedUpdate.Value ? m_needToUpdateIcon : m_dontNeedUpdateIcon;
                    m_updateButtonIcon.FillColor = APIUpdateManager.IsNeedUpdate.Value ? Color.Yellow : Color.White;
                }
            }
            if (m_updateCheckButton?.IsClicked ?? false) {
                ScreensManager.SwitchScreen("Releases", ModsManager.APIReleasesLink_API, "API");
                //TODO 原版的获取更新逻辑我不知道要咋处理
                //ScreensManager.SwitchScreen("Releases", "https://gitee.com/api/v5/repos/yhuse/SunnyUI/releases", "SunnyUI", new Test());
                //uint versionInt = APIUpdateManager.ParseVersionFromString(ModsManager.APIVersionString);
                //Engine.Log.Information($"The current version is: {ModsManager.APIVersionString}, with uint 0x{versionInt:X8}");
                //if (!APIUpdateManager.IsNeedUpdate.HasValue) DialogsManager.ShowDialog(this, new MessageDialog(LanguageControl.Get(fName,7), LanguageControl.Get(fName, 6), LanguageControl.Ok, LanguageControl.Get(fName, 8), (button) => {
                //	if(button == MessageDialogButton.Button2)
                //	{
                //		WebBrowserManager.LaunchBrowser(ModsManager.APIReleasesLink_Client);
                //	}
                //}));
                //else
                //{
                //	if(APIUpdateManager.IsNeedUpdate.Value)
                //		DialogsManager.ShowDialog(this,new MessageDialog(LanguageControl.Get(fName,7),string.Format(LanguageControl.Get(fName,4),APIUpdateManager.LatestVersion,APIUpdateManager.CurrentVersion),LanguageControl.Get(fName,5),LanguageControl.Cancel,
                //				(button) => {
                //					if(button == MessageDialogButton.Button1)
                //					{
                //						WebBrowserManager.LaunchBrowser(ModsManager.APIReleasesLink_Client);
                //					}
                //				}));
                //	else DialogsManager.ShowDialog(this,new MessageDialog(LanguageControl.Get(fName,7),LanguageControl.Get(fName,3),LanguageControl.Ok,null,null));
                //}
            }
#if BROWSER
            if (m_fullscreenButton.IsClicked) {
                Window.WindowMode = Window.WindowMode == WindowMode.Fullscreen ? WindowMode.Fixed : WindowMode.Fullscreen;
            }
            if (m_dashboardButton.IsClicked) {
                WebBrowserManager.LaunchBrowser("./dashboard.html");
            }
#endif
            if (Children.Find<ButtonWidget>("Play").IsClicked) {
                ScreensManager.SwitchScreen("Play");
            }
            if (Children.Find<ButtonWidget>("Help").IsClicked) {
                ScreensManager.SwitchScreen("Help");
            }
            if (Children.Find<ButtonWidget>("Content").IsClicked) {
                ScreensManager.SwitchScreen("Content");
            }
            if (Children.Find<ButtonWidget>("Settings").IsClicked) {
                ScreensManager.SwitchScreen("Settings");
            }
            if (Children.Find<ButtonWidget>("Buy").IsClicked) {
                MarketplaceManager.ShowMarketplace();
            }
            if (m_disableSafeModeButton.IsClicked) {
                SettingsManager.SafeMode = false;
                DialogsManager.ShowDialog(
                    null,
                    new MessageDialog(
                        LanguageControl.Warning,
                        LanguageControl.Get("SettingsCompatibilityScreen", "4"),
                        LanguageControl.Ok,
                        null,
                        null
                    )
                );
            }
            if (m_showBulletinButton?.IsClicked ?? false) {
                if (MotdManager.m_bulletin != null
                    && !MotdManager.m_bulletin.Title.Equals("null", StringComparison.CurrentCultureIgnoreCase)) {
                    MotdManager.ShowBulletin();
                }
                else {
                    DialogsManager.ShowDialog(
                        null,
                        new MessageDialog(LanguageControl.Get(fName, "1"), LanguageControl.Get(fName, "2"), LanguageControl.Ok, null, null)
                    );
                }
            }
            if ((Input.Back || Input.IsKeyDownOnce(Key.Escape)) && !Keyboard.BackButtonQuitsApp) {
                if (MarketplaceManager.IsTrialMode) {
                    ScreensManager.SwitchScreen("Nag");
                }
                else {
                    Window.Close();
                }
            }
            /*if (!string.IsNullOrEmpty(ExternalContentManager.openFilePath)) {
                ScreensManager.SwitchScreen("ExternalContent");
            }*/
        }
    }
}