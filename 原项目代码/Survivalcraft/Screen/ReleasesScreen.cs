using System.Text.Json;
using System.Xml.Linq;
using Engine;

namespace Game {
    public class ReleasesScreen : Screen {
        #region 用户自定义区

        public string m_releasesName = string.Empty; //此界面发布版系列的名称

        public string m_releasesURL = string.Empty; //此界面发布版系列的Releases链接

        public IComparer<ReleaseInfo> m_versionComparer;

        #endregion

        public List<ReleaseInfo> Releases { get; set; } = new(); //所有发布版的信息
        public LabelWidget m_titleLabel;
        public LabelWidget m_textLabel;
        public LabelWidget m_infoLabel;
        public ListPanelWidget m_releasesListPanel;
        public StackPanelWidget m_releaseInfoPanel;
        public Dictionary<BevelledButtonWidget, Asset> m_assetButtons = new(); //点击就下载资源的按钮
        public ScrollPanelWidget m_scrollPanel;
        public BusyDialog m_busyDialog; //进入时展示的"获取发布版..."的对话框
        public const string fName = "ReleasesScreen";

        public ReleasesScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/ReleasesScreen");
            LoadContents(this, node);
            m_titleLabel = Children.Find<LabelWidget>("ReleaseTitle");
            m_textLabel = Children.Find<LabelWidget>("ReleaseText");
            m_scrollPanel = Children.Find<ScrollPanelWidget>("ScrollPanel");
            m_releasesListPanel = Children.Find<ListPanelWidget>("ReleasesList");
            m_infoLabel = Children.Find<LabelWidget>("ReleaseInfo");
            m_releaseInfoPanel = Children.Find<StackPanelWidget>("ReleaseInfoPanel");
            m_releasesListPanel.ItemWidgetFactory = item => new LabelWidget {
                Text = item is ReleaseInfo releaseInfo
                    ? releaseInfo.name + GetVersionSuffix(releaseInfo.tag_name, APIUpdateManager.CurrentVersion)
                    : string.Empty,
                HorizontalAlignment = WidgetAlignment.Center,
                VerticalAlignment = WidgetAlignment.Center
            };
            m_releasesListPanel.ItemClicked += DisplayReleaseInfo;
        }

        public override void Enter(object[] parameters) {
            string previousName = m_releasesName;
            m_releasesURL = (string)parameters[0];
            m_releasesName = (string)parameters[1];
            if (parameters.Length >= 3) {
                m_versionComparer = parameters[2] as IComparer<ReleaseInfo> ?? new ReleaseInfoComparer();
            }
            else {
                m_versionComparer = new ReleaseInfoComparer();
            }
            m_busyDialog = new BusyDialog(
                LanguageControl.GetContentWidgets(fName, 3),
                string.Format(LanguageControl.GetContentWidgets(fName, 2), m_releasesName)
            );
            Children.Find<LabelWidget>("TopBar.Label").Text = string.Format(
                LanguageControl.GetContentWidgets(fName, 1),
                m_releasesName
            );
            if (Releases.Count == 0
                || m_releasesName != previousName) {
                Task.Run(() => Task.FromResult(GetReleasesAsync()));
            }
            m_scrollPanel.ScrollPosition = 0f;
        }

        public async Task GetReleasesAsync() //异步获取发布版
        {
            Exception e = null;
            if (m_busyDialog.RootWidget != ScreensManager.RootWidget) {
                DialogsManager.ShowDialog(null, m_busyDialog);
            }
            try {
                JsonDocument document = await OnlineJsonReader.GetJsonFromUrlAsync(m_releasesURL);
                //Releases = JsonSerializer.Deserialize<List<ReleaseInfo>>(document.RootElement.GetRawText());
                Releases = JsonSerializer.Deserialize(document.RootElement.GetRawText(), GiteeReleaseInfoJsonContext.Default.ListReleaseInfo);
                Releases.Sort(m_versionComparer);
                PopulateReleasesList();
            }
            catch (Exception exception) {
                e = exception;
            }
            finally {
                if (m_busyDialog.RootWidget == ScreensManager.RootWidget) {
                    DialogsManager.HideDialog(m_busyDialog);
                }
                if (e != null) {
                    DialogsManager.ShowDialog(
                        null,
                        new MessageDialog(
                            LanguageControl.GetContentWidgets(fName, 3),
                            string.Format(LanguageControl.GetContentWidgets(fName, 20), m_releasesName, e.Message),
                            LanguageControl.Ok,
                            null,
                            null
                        )
                    );
                    Log.Error(LanguageControl.GetContentWidgets(fName, 20), m_releasesName, e.Message);
                }
            }
        }

        public void PopulateReleasesList() //向左侧版本列表中加入Release条目
        {
            m_releasesListPanel.ClearItems();
            foreach (ReleaseInfo releaseInfo in Releases) {
                m_releasesListPanel.AddItem(releaseInfo);
            }
            if (Releases.Count > 0) {
                DisplayReleaseInfo(Releases[0]);
            }
        }

        public string GetVersionSuffix(string currentVersion, string targetVersion) {
            if (currentVersion == APIUpdateManager.LatestVersion) {
                return LanguageControl.GetContentWidgets(fName, 6);
            }
            int firstNumberIndex = currentVersion.IndexOfAny(
                [
                    '0',
                    '1',
                    '2',
                    '3',
                    '4',
                    '5',
                    '6',
                    '7',
                    '8',
                    '9'
                ]
            );
            if (firstNumberIndex >= 0 && currentVersion.Substring(firstNumberIndex) == targetVersion) {
                return LanguageControl.GetContentWidgets(fName, "5");
            }
            return string.Empty;
        }

        public void PopulateAssetsList(ReleaseInfo releaseInfo) {
            foreach (BevelledButtonWidget assetButton in m_assetButtons.Keys) {
                m_releaseInfoPanel.Children.Remove(assetButton);
            }
            m_assetButtons.Clear();
            foreach (Asset asset in releaseInfo.assets) {
                BevelledButtonWidget button = new() { Size = new Vector2(float.PositiveInfinity, 56), Text = asset.name, FontScale = 0.85f };
                m_assetButtons.Add(button, asset);
                m_releaseInfoPanel.Children.Add(button);
            }
        }

        public void DisplayReleaseInfo(object item) //档左侧版本列表中某条目点击时
        {
            ReleaseInfo releaseInfo = (ReleaseInfo)item;
            m_titleLabel.Text = releaseInfo.name;
            m_infoLabel.Text = string.Format(
                LanguageControl.GetContentWidgets(fName, 4),
                releaseInfo.author.name,
                releaseInfo.created_at
            );
            m_textLabel.Text = releaseInfo.body;
            PopulateAssetsList(releaseInfo);
        }

        public override void Update() {
            foreach (KeyValuePair<BevelledButtonWidget, Asset> assetButton in m_assetButtons) {
                if (assetButton.Key.IsClicked) {
                    WebBrowserManager.LaunchBrowser(assetButton.Value.browser_download_url);
                }
            }
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
            }
        }

        //用于给版本列表排序
        public class ReleaseInfoComparer : IComparer<ReleaseInfo> {
            public int Compare(ReleaseInfo x, ReleaseInfo y) {
                if (ReferenceEquals(x, y)) {
                    return 0;
                }
                if (y is null) {
                    return 1;
                }
                if (x is null) {
                    return -1;
                }
                return -APIUpdateManager.CompareVersion(x.tag_name, y.tag_name);
            }
        }
    }
}