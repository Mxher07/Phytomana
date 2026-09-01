using System.Xml.Linq;
using Engine;

namespace Game {
    public class ContentScreen : Screen {
        public enum CommunityType {
            OriginalCommunity = 0,
            ChineseCommunity = 1,
            ChineseCommunityWebsite = 2
        }
        public static string fName = "ContentScreen";

        public ButtonWidget m_externalContentButton;
        public ButtonWidget m_deviceButton;
        public ButtonWidget m_communityContentButton;
        public ButtonWidget m_linkButton;
        public ButtonWidget m_manageButton;
        public ButtonWidget m_manageModButton;

        public bool m_isAdmin;

        public ContentScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/ContentScreen");
            LoadContents(this, node);
            m_externalContentButton = Children.Find<ButtonWidget>("External");
            m_deviceButton = Children.Find<ButtonWidget>("Device");
            m_communityContentButton = Children.Find<ButtonWidget>("Community");
            m_linkButton = Children.Find<ButtonWidget>("Link");
            m_manageButton = Children.Find<BevelledButtonWidget>("Manage");
            m_manageModButton = Children.Find<BevelledButtonWidget>("ManageMod");
        }

        public override void Enter(object[] parameters) {
            base.Enter(parameters);
#if RELEASE
            CommunityContentManager.IsAdmin(new CancellableProgress(), delegate(bool isAdmin) { m_isAdmin = isAdmin; }, _ => { });
#endif
        }

        public void OpenManageSelectDialog() {
            List<string> list = [LanguageControl.Get(fName, 1), LanguageControl.Get(fName, 2)];
            if (m_isAdmin) {
                list = [LanguageControl.Get(fName, 1), LanguageControl.Get(fName, 2), "用户管理"];
            }
            DialogsManager.ShowDialog(
                null,
                new ListSelectionDialog(
                    null,
                    list,
                    70f,
                    item => (string)item,
                    item => {
                        string selectionResult = (string)item;
                        if (selectionResult == LanguageControl.Get(fName, 1)) {
                            ScreensManager.SwitchScreen("ModsManageContent");
                        }
                        else if (selectionResult == LanguageControl.Get(fName, 2)) {
                            ScreensManager.SwitchScreen("ManageContent");
                        }
                        else {
                            ScreensManager.SwitchScreen("ManageUser");
                        }
                    }
                )
            );
        }

        public override void Update() {
            m_communityContentButton.IsEnabled = SettingsManager.CommunityContentMode != CommunityContentMode.Disabled && SettingsManager.OriginalCommunityContentMode != CommunityContentMode.Disabled;
            if (m_externalContentButton.IsClicked) {
                ScreensManager.SwitchScreen("ExternalContent");
            }
            if (m_deviceButton.IsClicked) {
                Task.Run(async () => {
                        try {
#if WINDOWS
                            KeyValuePair<string, string[]>[] filters = [
                                new(LanguageControl.Get(fName, "ExtensionName", ".scworld"), ["*.scworld"]),
                                new(LanguageControl.Get(fName, "ExtensionName", ".scbtex"), ["*.scbtex", "*.png", "*.webp", "*.astc", "*.astcsrgb"]),
                                new(LanguageControl.Get(fName, "ExtensionName", ".scskin"), ["*.scskin"]),
                                new(LanguageControl.Get(fName, "ExtensionName", ".scfpack"), ["*.scfpack"]),
                                new(LanguageControl.Get(fName, "ExtensionName", ".scmod"), ["*.scmod"])
                            ];
                            (Stream stream, string fileName) = await Storage.ChooseFile(LanguageControl.Get(fName, "3"), filters);
#else
                            KeyValuePair<string, string[]>[] filters = [
                                new(LanguageControl.Get(fName, "ExtensionName", ".scworld"), [".scworld"]),
                                new(LanguageControl.Get(fName, "ExtensionName", ".scbtex"), [".scbtex", ".png", ".webp", ".astc", ".astcsrgb"]),
                                new(LanguageControl.Get(fName, "ExtensionName", ".scskin"), [".scskin"]),
                                new(LanguageControl.Get(fName, "ExtensionName", ".scfpack"), [".scfpack"]),
                                new(LanguageControl.Get(fName, "ExtensionName", ".scmod"), [".scmod"])
                            ];
                            (Stream stream, string fileName) = await Storage.ChooseFile(LanguageControl.Get(fName, "3"), filters);
#endif
                            if (stream == null) {
                                if (!string.IsNullOrEmpty(fileName)) {
                                    Dispatcher.Dispatch(() => {
                                            DialogsManager.ShowDialog(
                                                null,
                                                new MessageDialog(
                                                    LanguageControl.Error,
                                                    string.Format(LanguageControl.Get(fName, "6"), fileName),
                                                    LanguageControl.Ok,
                                                    null,
                                                    null
                                                )
                                            );
                                        }
                                    );
                                }
                                return;
                            }
                            await using (stream) {
                                if (fileName != null) {
                                    ExternalContentType type = ExternalContentManager.ExtensionToType(Storage.GetExtension(fileName));
                                    ExternalContentManager.ImportExternalContentSync(stream, type, fileName);
                                    Dispatcher.Dispatch(() => {
                                            DialogsManager.ShowDialog(
                                                null,
                                                new MessageDialog(
                                                    LanguageControl.Success,
                                                    string.Format(LanguageControl.Get(fName, "4"), fileName),
                                                    LanguageControl.Ok,
                                                    null,
                                                    null
                                                )
                                            );
                                        }
                                    );
                                }
                            }
                        }
                        catch (Exception e) {
                            Log.Error(e);
                            Dispatcher.Dispatch(() => {
                                    DialogsManager.ShowDialog(
                                        null,
                                        new MessageDialog(LanguageControl.Error, e.Message, LanguageControl.Ok, null, null)
                                    );
                                }
                            );
                        }
                    }
                );
            }
            if (m_communityContentButton.IsClicked) {
                DialogsManager.ShowDialog(null, new SelectCommunityServerDialog());
                /*List<CommunityType> items = [];
                if (SettingsManager.OriginalCommunityContentMode != CommunityContentMode.Disabled) {
                    items.Add(CommunityType.OriginalCommunity);
                }
                if (SettingsManager.CommunityContentMode != CommunityContentMode.Disabled) {
                    items.Add(CommunityType.ChineseCommunity);
                    items.Add(CommunityType.ChineseCommunityWebsite);
                }
                DialogsManager.ShowDialog(
                    null,
                    new ListSelectionDialog(
                        LanguageControl.Get(fName, "5"),
                        items,
                        70f,
                        item => item is CommunityType type ? LanguageControl.Get(fName, "CommunityType", type.ToString()) : null,
                        item => {
                            if (item is CommunityType type) {
                                switch (type) {
                                    case CommunityType.OriginalCommunity:
                                        ScreensManager.SwitchScreen("OriginalCommunityContent");
                                        break;
                                    case CommunityType.ChineseCommunity:
                                        ScreensManager.SwitchScreen("CommunityContent");
                                        break;
                                    case CommunityType.ChineseCommunityWebsite:
                                        WebBrowserManager.LaunchBrowser("https://test.suancaixianyu.cn/");
                                        break;
                                }
                            }
                        }
                    )
                );*/
            }
            if (m_linkButton.IsClicked) {
                DialogsManager.ShowDialog(null, new DownloadContentFromLinkDialog());
            }
            if (m_manageButton.IsClicked) {
                ScreensManager.SwitchScreen("ManageContent");
            }
            if (m_manageModButton.IsClicked) {
                ScreensManager.SwitchScreen("ModsManageContent");
            }
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen("MainMenu");
            }
        }
    }
}