using System.Xml.Linq;
using Engine;
#if !ANDROID
#endif

namespace Game {
    public class SettingsCompatibilityScreen : Screen {
        //public ButtonWidget m_singlethreadedTerrainUpdateButton;

        public ButtonWidget m_viewGameLogButton;
        public ButtonWidget m_openGameLogButton;
        public ContainerWidget m_shareGameLogButtonPanel;
        public ButtonWidget m_shareGameLogButton;
        public ButtonWidget m_reportButton;
        public ButtonWidget m_fileAssociationEnabledButton;
        public ButtonWidget m_manageClassSubstitutesButton;
        public ButtonWidget m_safeModeButton;
        public ButtonWidget m_resetDefaultsButton;
        public LabelWidget m_descriptionLabel;
        public const string fName = "SettingsCompatibilityScreen";

        public SettingsCompatibilityScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/SettingsCompatibilityScreen");
            LoadContents(this, node);
            //m_singlethreadedTerrainUpdateButton = Children.Find<ButtonWidget>("SinglethreadedTerrainUpdateButton");
            m_viewGameLogButton = Children.Find<ButtonWidget>("ViewGameLogButton");
            m_openGameLogButton = Children.Find<ButtonWidget>("OpenGameLogButton");
            m_shareGameLogButton = Children.Find<ButtonWidget>("ShareGameLogButton");
            m_shareGameLogButtonPanel = Children.Find<ContainerWidget>("ShareGameLogButtonPanel");
            m_reportButton = Children.Find<ButtonWidget>("ReportButton");
            m_fileAssociationEnabledButton = Children.Find<ButtonWidget>("FileAssociationEnabledButton");
            m_manageClassSubstitutesButton = Children.Find<ButtonWidget>("ManageClassSubstitutesButton");
            m_safeModeButton = Children.Find<ButtonWidget>("SafeModeButton");
            m_resetDefaultsButton = Children.Find<ButtonWidget>("ResetDefaultsButton");
            m_descriptionLabel = Children.Find<LabelWidget>("Description");
#if !WINDOWS
            m_fileAssociationEnabledButton.IsEnabled = false;
#endif
#if ANDROID
            m_shareGameLogButtonPanel.IsVisible = true;
#endif
            m_safeModeButton.Text = SettingsManager.SafeMode ? LanguageControl.Enable : LanguageControl.Disable;
        }

        public override void Enter(object[] parameters) {
            m_descriptionLabel.Text = LanguageControl.Get(fName, "1");
        }

        public override void Update() {
            //if (m_singlethreadedTerrainUpdateButton.IsClicked)
            //{
            //	SettingsManager.MultithreadedTerrainUpdate = !SettingsManager.MultithreadedTerrainUpdate;
            //	m_descriptionLabel.Text = StringsManager.GetString("Settings.Compatibility.SinglethreadedTerrainUpdate.Description");
            //}
            if (m_viewGameLogButton.IsClicked) {
                m_descriptionLabel.Text = LanguageControl.Get(fName, "1");
                DialogsManager.ShowDialog(null, new ViewGameLogDialog());
            }
            if (m_openGameLogButton.IsClicked) {
#if BROWSER
                DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Warning, LanguageControl.Get(fName, "5"), LanguageControl.Ok, null, null));
#else
                m_descriptionLabel.Text = LanguageControl.Get(fName, "1");
                string path = Storage.CombinePaths(ModsManager.LogPath, "Game.log");
                if (Storage.FileExists(path)) {
                    try {
                        Storage.OpenFileWithExternalApplication(
                            path,
                            LanguageControl.GetContentWidgets("SettingsCompatibilityScreen", "13"),
                            "text/plain"
                        );
                    }
                    catch (Exception e) {
                        DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, e.Message, LanguageControl.Ok, null, null));
                    }
                }
#endif
            }
            if (m_shareGameLogButton.IsClicked) {
                m_descriptionLabel.Text = LanguageControl.Get(fName, "1");
                string path = Storage.CombinePaths(ModsManager.LogPath, "Game.log");
                if (Storage.FileExists(path)) {
                    Task.Run(async () => {
                            try {
                                await Storage.ShareFile(path, LanguageControl.GetContentWidgets("SettingsCompatibilityScreen", "15"), "text/plain");
                            }
                            catch (Exception e) {
                                Dispatcher.Dispatch(() => DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, e.Message, LanguageControl.Ok, null, null)));
                            }
                        }
                    );
                }
            }
            if (m_reportButton.IsClicked) {
                m_descriptionLabel.Text = LanguageControl.Get(fName, "1");
                WebBrowserManager.LaunchBrowser(ModsManager.ReportLink);
            }
#if WINDOWS
            if (m_fileAssociationEnabledButton.IsClicked) {
                m_descriptionLabel.Text = LanguageControl.Get(fName, "2");
                if (SettingsManager.FileAssociationEnabled) {
                    FileAssociationManager.Unregister();
                    SettingsManager.FileAssociationEnabled = false;
                }
                else {
                    SettingsManager.FileAssociationEnabled = FileAssociationManager.Register();
                }
            }
#endif
            if (m_manageClassSubstitutesButton.IsClicked) {
                ScreensManager.SwitchScreen("ManageClassSubstitutes");
            }
            if (m_safeModeButton.IsClicked) {
                m_descriptionLabel.Text = LanguageControl.Get(fName, "3");
                SettingsManager.SafeMode = !SettingsManager.SafeMode;
                m_safeModeButton.Text = SettingsManager.SafeMode ? LanguageControl.Enable : LanguageControl.Disable;
                DialogsManager.ShowDialog(
                    null,
                    new MessageDialog(LanguageControl.Warning, LanguageControl.Get(fName, "4"), LanguageControl.Ok, null, null)
                );
            }
            if (m_resetDefaultsButton.IsClicked) {
                SettingsManager.MultithreadedTerrainUpdate = true;
            }
            m_fileAssociationEnabledButton.Text = SettingsManager.FileAssociationEnabled ? LanguageControl.Enable : LanguageControl.Disable;
            //m_singlethreadedTerrainUpdateButton.Text = "已弃用";
            m_resetDefaultsButton.IsEnabled = !SettingsManager.MultithreadedTerrainUpdate;
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.GoBack();
            }
        }
    }
}