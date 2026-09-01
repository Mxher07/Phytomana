using System.Text.Json;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Game.IContentReader;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = Engine.Color;

namespace Game {
    public class CommunityContentScreen : Screen {
        public enum Order {
            ByRank = 0,
            ByTime = 1,
            ByBoutique = 2,
            ByHide = 3
        }

        public enum SearchType {
            ByName = 0,
            ByAuthor = 1,
            ByUserId = 2
        }

        public TreeViewWidget m_treePanel;
        public LabelWidget m_orderLabel;
        public ButtonWidget m_changeOrderButton;
        public LabelWidget m_filterLabel;
        public ButtonWidget m_changeFilterButton;
        public ButtonWidget m_downloadButton;
        public ButtonWidget m_actionButton;
        public ButtonWidget m_action2Button;
        public ButtonWidget m_webPageButton;
        public ButtonWidget m_loginButton;
        public ButtonWidget m_searchKey;
        public ButtonWidget m_searchTypeButton;
        public TextBoxWidget m_inputKey;
        public LabelWidget m_placeHolder;
        public LinkWidget m_clearSearchLink;

        public object m_filter;
        public Order m_order;
        public SearchType m_searchType;
        public double m_contentExpiryTime;

        public bool m_isOwn;
        public bool m_isAdmin;
        public bool m_isCNLanguageType;

        public Dictionary<string, IEnumerable<object>> m_itemsCache = [];

        public SchubExternalContentProvider m_provider;

        public const string fName = "CommunityContentScreen";

        public CommunityContentScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/CommunityContentScreen");
            LoadContents(this, node);
            m_treePanel = Children.Find<TreeViewWidget>("Tree");
            m_orderLabel = Children.Find<LabelWidget>("Order");
            m_changeOrderButton = Children.Find<ButtonWidget>("ChangeOrder");
            m_filterLabel = Children.Find<LabelWidget>("Filter");
            m_changeFilterButton = Children.Find<ButtonWidget>("ChangeFilter");
            m_downloadButton = Children.Find<ButtonWidget>("Download");
            m_actionButton = Children.Find<ButtonWidget>("Action");
            m_action2Button = Children.Find<ButtonWidget>("Action2");
            m_webPageButton = Children.Find<ButtonWidget>("WebPage");
            m_loginButton = Children.Find<ButtonWidget>("Login");
            m_inputKey = Children.Find<TextBoxWidget>("key");
            m_placeHolder = Children.Find<LabelWidget>("placeholder");
            m_clearSearchLink = Children.Find<LinkWidget>("ClearSearchLink");
            m_searchKey = Children.Find<ButtonWidget>("Search");
            m_searchTypeButton = Children.Find<ButtonWidget>("SearchType");
            m_searchType = SearchType.ByName;
            /*
            m_treePanel.ItemWidgetFactory = delegate (object item)
            {
                var communityContentEntry = item as CommunityContentEntry;
                if (communityContentEntry != null)
                {
                    XElement node2 = ContentManager.Get<XElement>("Widgets/CommunityContentItem");
                    var obj = (ContainerWidget)LoadWidget(this, node2, null);
                    communityContentEntry.IconInstance = obj.Children.Find<RectangleWidget>("CommunityContentItem.Icon");
                    communityContentEntry.IconInstance.Subtexture = communityContentEntry.Icon == null ? ExternalContentManager.GetEntryTypeIcon(communityContentEntry.Type) : new Subtexture(communityContentEntry.Icon, Vector2.Zero, Vector2.One);
                    obj.Children.Find<LabelWidget>("CommunityContentItem.Text").Text = communityContentEntry.Name;
                    Color txtColor = Color.White;
                    if (communityContentEntry.Boutique > 0)
                    {
                        txtColor = new Color(255, 215, 0);
                    }
                    if (m_isOwn && communityContentEntry.IsShow == 0)
                    {
                        txtColor = Color.Gray;
                    }
                    obj.Children.Find<LabelWidget>("CommunityContentItem.Text").Color = txtColor;
                    obj.Children.Find<LabelWidget>("CommunityContentItem.Details").Text = $"{ExternalContentManager.GetEntryTypeDescription(communityContentEntry.Type)} {DataSizeFormatter.Format(communityContentEntry.Size)}";
                    obj.Children.Find<StarRatingWidget>("CommunityContentItem.Rating").Rating = communityContentEntry.RatingsAverage;
                    obj.Children.Find<StarRatingWidget>("CommunityContentItem.Rating").IsVisible = communityContentEntry.RatingsAverage > 0f;
                    obj.Children.Find<LabelWidget>("CommunityContentItem.ExtraText").Text = communityContentEntry.ExtraText;
                    return obj;
                }
                XElement node3 = ContentManager.Get<XElement>("Widgets/CommunityContentItemMore");
                var containerWidget = (ContainerWidget)LoadWidget(this, node3, null);
                m_moreLink = containerWidget.Children.Find<LinkWidget>("CommunityContentItemMore.Link");
                m_moreLink.Tag = item as string;
                return containerWidget;
            };
            m_treePanel.SelectionChanged += delegate
            {
                if (m_treePanel.SelectedItem != null && !(m_treePanel.SelectedItem is CommunityContentEntry))
                {
                    m_treePanel.SelectedItem = null;
                }
            };
            */
        }

        public override void Enter(object[] parameters) {
            foreach (IExternalContentProvider provider in ExternalContentManager.m_providers) {
                if (provider is SchubExternalContentProvider contentProvider) {
                    m_provider = contentProvider;
                    break;
                }
            }
            if (parameters.Length > 0
                && parameters[0].ToString() == "Mod") {
                m_filter = ExternalContentType.Mod;
            }
            else {
                m_filter = string.Empty;
            }
            m_order = Order.ByRank;
            m_inputKey.Text = string.Empty;
            m_isOwn = false;
            string languageType = !ModsManager.Configs.TryGetValue("Language", out string value) ? "zh-CN" : value;
            m_isCNLanguageType = languageType == "zh-CN";
            CommunityContentManager.IsAdmin(
                new CancellableProgress(),
                delegate(bool isAdmin) { m_isAdmin = isAdmin; },
                delegate(Exception e) {
                    DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, e.Message, LanguageControl.Ok, null, null));
                }
            );
            PopulateList(null);
        }

        public override void Update() {
            m_placeHolder.IsVisible = string.IsNullOrEmpty(m_inputKey.Text);
            m_clearSearchLink.IsVisible = !string.IsNullOrEmpty(m_inputKey.Text) || m_inputKey.HasFocus;
            m_actionButton.IsVisible = m_isAdmin || m_isOwn;
            m_action2Button.IsVisible = m_isAdmin || m_isOwn;
            if (!m_isCNLanguageType) {
                m_actionButton.IsVisible = false;
                m_action2Button.IsVisible = false;
                m_webPageButton.IsVisible = false;
            }
            CommunityContentEntry communityContentEntry = m_treePanel.SelectedNode?.Tag as CommunityContentEntry;
            m_downloadButton.IsEnabled = communityContentEntry != null;
            if (communityContentEntry != null) {
                m_actionButton.IsEnabled = m_isAdmin || m_isOwn;
                m_actionButton.Text = m_order == Order.ByHide || m_isOwn ? LanguageControl.Get(fName, 23) :
                    communityContentEntry.Boutique == 0 ? LanguageControl.Get(fName, 15) : LanguageControl.Get(fName, 16);
                m_action2Button.IsEnabled = m_filter.ToString() != "Mod" && (m_isAdmin || m_isOwn);
            }
            else {
                m_actionButton.IsEnabled = false;
                m_action2Button.IsEnabled = false;
                m_actionButton.Text = LanguageControl.Get(fName, 17);
            }
            if (m_isOwn) {
                m_searchType = SearchType.ByName;
                m_searchTypeButton.IsEnabled = false;
            }
            else {
                m_searchTypeButton.IsEnabled = true;
            }
            m_action2Button.Text = communityContentEntry != null && communityContentEntry.IsShow == 0
                ? LanguageControl.Get(fName, 24)
                : LanguageControl.Get(fName, 25);
            m_orderLabel.Text = GetOrderDisplayName(m_order);
            m_filterLabel.Text = GetFilterDisplayName(m_filter);
            m_searchTypeButton.Text = GetSearchTypeDisplayName(m_searchType);
            if (m_changeOrderButton.IsClicked) {
                List<Order> items = EnumUtils.GetEnumValues<Order>().Cast<Order>().ToList();
                if (!m_isAdmin) {
                    items.Remove(Order.ByHide);
                }
                DialogsManager.ShowDialog(
                    null,
                    new ListSelectionDialog(
                        LanguageControl.Get(fName, "Order Type"),
                        items,
                        60f,
                        item => GetOrderDisplayName((Order)item),
                        delegate(object item) {
                            m_order = (Order)item;
                            PopulateList(null, true);
                        }
                    )
                );
            }
            if (m_searchKey.IsClicked) {
                PopulateList(null);
            }
            if (m_changeFilterButton.IsClicked) {
                List<object> list = [string.Empty];
                foreach (ExternalContentType item in from ExternalContentType t in EnumUtils.GetEnumValues<ExternalContentType>()
                    where ExternalContentManager.IsEntryTypeDownloadSupported(t)
                    select t) {
                    list.Add(item);
                }
                if (!string.IsNullOrEmpty(SettingsManager.ScpboxAccessToken)) {
                    list.Add(SettingsManager.ScpboxAccessToken);
                }
                DialogsManager.ShowDialog(
                    null,
                    new ListSelectionDialog(
                        LanguageControl.Get(fName, "Filter"),
                        list,
                        60f,
                        GetFilterDisplayName,
                        delegate(object item) {
                            m_filter = item;
                            m_isOwn = item is string str && !string.IsNullOrEmpty(str);
                            PopulateList(null, true);
                        }
                    )
                );
            }
            if (m_clearSearchLink.IsClicked) {
                m_inputKey.Text = string.Empty;
                PopulateList(null);
            }
            if (m_downloadButton.IsClicked
                && communityContentEntry != null) {
                DownloadEntry(communityContentEntry);
            }
            if (m_actionButton.IsClicked
                && communityContentEntry != null) {
                if (m_order == Order.ByHide || m_isOwn) {
                    DialogsManager.ShowDialog(
                        null,
                        new MessageDialog(
                            LanguageControl.Get(fName, 26),
                            communityContentEntry.Name,
                            LanguageControl.Ok,
                            LanguageControl.Cancel,
                            delegate(MessageDialogButton button) {
                                if (button == MessageDialogButton.Button1) {
                                    CancellableBusyDialog busyDialog = new(LanguageControl.Get(fName, 2), false);
                                    DialogsManager.ShowDialog(null, busyDialog);
                                    CommunityContentManager.DeleteFile(
                                        communityContentEntry.Index,
                                        busyDialog.Progress,
                                        delegate(byte[] data) {
                                            DialogsManager.HideDialog(busyDialog);
                                            m_treePanel.RemoveAtTag(communityContentEntry);
                                            JsonElement result = JsonDocument.Parse(data, JsonDocumentReader.DefaultJsonOptions).RootElement;
                                            string msg = result[0].GetInt32() == 200
                                                ? LanguageControl.Get(fName, 27) + communityContentEntry.Name
                                                : result[1].GetString();
                                            DialogsManager.ShowDialog(
                                                null,
                                                new MessageDialog(LanguageControl.Get(fName, 20), msg, LanguageControl.Ok, null, null)
                                            );
                                        },
                                        delegate(Exception e) {
                                            DialogsManager.HideDialog(busyDialog);
                                            DialogsManager.ShowDialog(
                                                null,
                                                new MessageDialog(LanguageControl.Error, e.Message, LanguageControl.Ok, null, null)
                                            );
                                        }
                                    );
                                }
                            }
                        )
                    );
                }
                else {
                    if (communityContentEntry.Boutique == 0) {
                        DialogsManager.ShowDialog(
                            null,
                            new TextBoxDialog(
                                LanguageControl.Get(fName, 18),
                                "5",
                                4,
                                delegate(string s) {
                                    if (!string.IsNullOrEmpty(s)) {
                                        int boutique = 5;
                                        try {
                                            boutique = int.Parse(s);
                                        }
                                        catch {
                                            // ignored
                                        }
                                        CancellableBusyDialog busyDialog = new(LanguageControl.Get(fName, 2), false);
                                        DialogsManager.ShowDialog(null, busyDialog);
                                        CommunityContentManager.UpdateBoutique(
                                            communityContentEntry.Type.ToString(),
                                            communityContentEntry.Index,
                                            boutique,
                                            busyDialog.Progress,
                                            delegate(byte[] data) {
                                                DialogsManager.HideDialog(busyDialog);
                                                m_order = Order.ByBoutique;
                                                PopulateList(null, true);
                                                JsonElement result = JsonDocument.Parse(data, JsonDocumentReader.DefaultJsonOptions).RootElement;
                                                string msg = result[0].GetInt32() == 200
                                                    ? LanguageControl.Get(fName, 19) + communityContentEntry.Name
                                                    : result[1].GetString();
                                                DialogsManager.ShowDialog(
                                                    null,
                                                    new MessageDialog(LanguageControl.Get(fName, 20), msg, LanguageControl.Ok, null, null)
                                                );
                                            },
                                            delegate(Exception e) {
                                                DialogsManager.HideDialog(busyDialog);
                                                DialogsManager.ShowDialog(
                                                    null,
                                                    new MessageDialog(LanguageControl.Error, e.Message, LanguageControl.Ok, null, null)
                                                );
                                            }
                                        );
                                    }
                                }
                            )
                        );
                    }
                    else {
                        DialogsManager.ShowDialog(
                            null,
                            new MessageDialog(
                                LanguageControl.Get(fName, 21),
                                communityContentEntry.Name,
                                LanguageControl.Ok,
                                LanguageControl.Cancel,
                                delegate(MessageDialogButton button) {
                                    if (button == MessageDialogButton.Button1) {
                                        CancellableBusyDialog busyDialog = new(LanguageControl.Get(fName, 2), false);
                                        DialogsManager.ShowDialog(null, busyDialog);
                                        CommunityContentManager.UpdateBoutique(
                                            communityContentEntry.Type.ToString(),
                                            communityContentEntry.Index,
                                            0,
                                            busyDialog.Progress,
                                            delegate(byte[] data) {
                                                DialogsManager.HideDialog(busyDialog);
                                                PopulateList(null, true);
                                                JsonElement result = JsonDocument.Parse(data, JsonDocumentReader.DefaultJsonOptions).RootElement;
                                                string msg = result[0].GetInt32() == 200
                                                    ? LanguageControl.Get(fName, 22) + communityContentEntry.Name
                                                    : result[1].GetString();
                                                DialogsManager.ShowDialog(
                                                    null,
                                                    new MessageDialog(LanguageControl.Get(fName, 20), msg, LanguageControl.Ok, null, null)
                                                );
                                            },
                                            delegate(Exception e) {
                                                DialogsManager.HideDialog(busyDialog);
                                                DialogsManager.ShowDialog(
                                                    null,
                                                    new MessageDialog(LanguageControl.Error, e.Message, LanguageControl.Ok, null, null)
                                                );
                                            }
                                        );
                                    }
                                }
                            )
                        );
                    }
                }
            }
            if (m_action2Button.IsClicked
                && communityContentEntry != null) {
                CancellableBusyDialog busyDialog = new(LanguageControl.Get(fName, 2), false);
                DialogsManager.ShowDialog(null, busyDialog);
                int isShow = (communityContentEntry.IsShow + 1) % 2;
                string sucessMsg = isShow == 1 ? LanguageControl.Get(fName, 28) : LanguageControl.Get(fName, 29);
                CommunityContentManager.UpdateHidePara(
                    communityContentEntry.Index,
                    isShow,
                    busyDialog.Progress,
                    delegate(byte[] data) {
                        DialogsManager.HideDialog(busyDialog);
                        if (!m_isOwn) {
                            m_treePanel.RemoveAtTag(communityContentEntry);
                        }
                        else {
                            PopulateList(null, true);
                        }
                        JsonElement result = JsonDocument.Parse(data, JsonDocumentReader.DefaultJsonOptions).RootElement;
                        string msg = result[0].GetInt32() == 200 ? sucessMsg + communityContentEntry.Name : result[1].GetString();
                        DialogsManager.ShowDialog(
                            null,
                            new MessageDialog(LanguageControl.Get(fName, 20), msg, LanguageControl.Ok, null, null)
                        );
                    },
                    delegate(Exception e) {
                        DialogsManager.HideDialog(busyDialog);
                        DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, e.Message, LanguageControl.Ok, null, null));
                    }
                );
            }
            if (m_webPageButton.IsClicked) {
                WebBrowserManager.LaunchBrowser(CommunityServerManager.CurrentChineseInfo.WebsiteUrl);
            }
            if (m_searchTypeButton.IsClicked) {
                if (m_isAdmin) {
                    if (m_searchType == SearchType.ByName) {
                        m_searchType = SearchType.ByAuthor;
                    }
                    else if (m_searchType == SearchType.ByAuthor) {
                        m_searchType = SearchType.ByUserId;
                    }
                    else if (m_searchType == SearchType.ByUserId) {
                        m_searchType = SearchType.ByName;
                    }
                }
                else {
                    if (m_searchType == SearchType.ByName) {
                        m_searchType = SearchType.ByAuthor;
                    }
                    else if (m_searchType == SearchType.ByAuthor) {
                        m_searchType = SearchType.ByName;
                    }
                    else if (m_searchType == SearchType.ByUserId) {
                        m_searchType = SearchType.ByName;
                    }
                }
            }
            if (m_loginButton.IsClicked) {
                //DialogsManager.ShowDialog(null, new MoreCommunityLinkDialog());
                if (m_provider.IsLoggedIn) {
                    string info = string.IsNullOrEmpty(SettingsManager.ScpboxUserInfo) ? "暂无用户信息" : SettingsManager.ScpboxUserInfo;
                    DialogsManager.ShowDialog(
                        null,
                        new MessageDialog(
                            "账号已登录,是否登出?",
                            info,
                            LanguageControl.Yes,
                            LanguageControl.No,
                            delegate(MessageDialogButton button) {
                                if (button == MessageDialogButton.Button1) {
                                    m_provider.Logout();
                                }
                            }
                        )
                    );
                }
                else {
                    ExternalContentManager.ShowLoginUiIfNeeded(m_provider, false, delegate { });
                }
            }
            if (Input.Back
                || Children.Find<BevelledButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen("Content");
            }
            if (Input.Hold.HasValue
                && Input.HoldTime > 2f
                && Input.Hold.Value.Y < 20f) {
                m_contentExpiryTime = 0.0;
                Task.Delay(250).Wait();
            }
        }

        public void PopulateList(string cursor, bool force = false) {
            string text = string.Empty;
            if (SettingsManager.CommunityContentMode == CommunityContentMode.Strict) {
                text = "1";
            }
            if (SettingsManager.CommunityContentMode == CommunityContentMode.Normal) {
                text = "0";
            }
            string text2 = m_filter is string s ? s : string.Empty;
            string text3 = m_filter is ExternalContentType ? m_filter.ToString() : string.Empty;
            string text4 = m_order.ToString();
            string cacheKey = $"{text2}\n{text3}\n{text4}\n{text}\n{m_inputKey.Text}";
            if (string.IsNullOrEmpty(cursor)
                && !force) {
                m_treePanel.ScrollPosition = 0f;
                if (m_contentExpiryTime != 0.0
                    && Time.RealTime < m_contentExpiryTime
                    && m_itemsCache.TryGetValue(cacheKey, out IEnumerable<object> value)) {
                    m_treePanel.Clear(false);
                    foreach (object item in value) //添加
                    {
                        if (item is TreeViewNode treeViewNode) {
                            m_treePanel.AddRoot(treeViewNode);
                        }
                    }
                    return;
                }
                m_treePanel.Clear();
            }
            if (force) {
                m_treePanel.Clear();
            }
            CancellableBusyDialog busyDialog = new(LanguageControl.Get(fName, 2), false);
            DialogsManager.ShowDialog(null, busyDialog);
            CommunityContentManager.List(
                cursor,
                text2,
                text3,
                text,
                text4,
                m_inputKey.Text,
                m_searchType.ToString(),
                busyDialog.Progress,
                delegate(List<CommunityContentEntry> list, string nextCursor) {
                    DialogsManager.HideDialog(busyDialog);
                    m_contentExpiryTime = Time.RealTime + 300.0;
                    while (m_treePanel.Nodes.Count > 0
                        && m_treePanel.Nodes[^1].Tag is "Load More") //移除原先添加的"加载更多"节点
                    {
                        m_treePanel.Nodes.RemoveAt(m_treePanel.Nodes.Count - 1);
                    }
                    foreach (CommunityContentEntry item2 in list) {
                        TreeViewNode rootNode = m_treePanel.Nodes.FirstOrDefault(x => x.Tag is int id && id == item2.CollectionID);
                        if (rootNode == null) {
                            rootNode = CreateCollectionNode(item2);
                            m_treePanel.AddRoot(rootNode);
                        }
                        rootNode.AddChild(ContentToNode(item2));
                        m_treePanel.m_widgetsDirty = true;
                        if (item2.Icon == null
                            && !string.IsNullOrEmpty(item2.IconSrc)) {
                            WebManager.Get(
                                item2.IconSrc,
                                null,
                                null,
                                new CancellableProgress(),
                                delegate(byte[] data) {
                                    Dispatcher.Dispatch(
                                        delegate {
                                            if (data.Length > 0) {
                                                try {
                                                    Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
                                                    bool flag = image.Width > image.Height;
                                                    if (image.Width > 256
                                                        || image.Height > 256) {
                                                        image.Mutate(x => x.Resize(flag ? 256 : 0, flag ? 0 : 256, KnownResamplers.Bicubic));
                                                    }
                                                    Vector2 iconMargin = Vector2.Zero;
                                                    if (flag) {
                                                        float ratio = (float)image.Height / image.Width;
                                                        iconMargin = new Vector2(0f, (1f - ratio) * 32f);
                                                    }
                                                    else if (image.Width < image.Height) {
                                                        float ratio = (float)image.Width / image.Height;
                                                        iconMargin = new Vector2((1f - ratio) * 32f, 0f);
                                                    }
                                                    Texture2D texture = Texture2D.Load(image);
                                                    item2.Icon = texture;
                                                    TreeViewNode linkedNode = item2.LinkedNode;
                                                    if (linkedNode != null) {
                                                        linkedNode.Icon = texture; //资源节点的图标
                                                        linkedNode.IconMargin = iconMargin;
                                                        TreeViewNode parentNode = item2.LinkedNode.ParentNode;
                                                        if (parentNode is { Tag: int }
                                                            && parentNode.Nodes.Last(item3 => item3.Icon != null) == linkedNode) //合集节点的图标
                                                        {
                                                            parentNode.Icon = texture;
                                                            parentNode.IconMargin = iconMargin;
                                                        }
                                                    }
                                                }
                                                catch (Exception) {
                                                    //System.Diagnostics.Debug.WriteLine(e.Message);
                                                }
                                            }
                                        }
                                    );
                                },
                                delegate { }
                            );
                        }
                        else if (item2.LinkedNode != null) {
                            item2.LinkedNode.Icon = item2.Icon;
                            item2.LinkedNode.IconMargin = Vector2.Zero;
                        }
                    }
                    if (list.Count > 0
                        && !string.IsNullOrEmpty(nextCursor)) {
                        //加载更多节点
                        TreeViewNode loadMoreNode = new(LanguageControl.Get(fName, "35"), new Color(64, 192, 64), string.Empty, Color.Transparent);
                        loadMoreNode.Selectable = false;
                        loadMoreNode.Tag = "Load More";
                        loadMoreNode.OnClicked = () => PopulateList(nextCursor);
                        m_treePanel.AddRoot(loadMoreNode);
                    }
                    m_itemsCache[cacheKey] = new List<object>(m_treePanel.Nodes);
                },
                delegate(Exception error) {
                    DialogsManager.HideDialog(busyDialog);
                    DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, error.Message, LanguageControl.Ok, null, null));
                }
            );
        }

        public void DownloadEntry(CommunityContentEntry entry) {
            string userId = UserManager.ActiveUser != null ? UserManager.ActiveUser.UniqueId : string.Empty;
            CancellableBusyDialog busyDialog = new(string.Format(LanguageControl.Get(fName, 1), entry.Name), false);
            DialogsManager.ShowDialog(null, busyDialog);
            CommunityContentManager.Download(
                entry.Address,
                entry.Name,
                entry.Type,
                userId,
                busyDialog.Progress,
                delegate { DialogsManager.HideDialog(busyDialog); },
                delegate(Exception error) {
                    DialogsManager.HideDialog(busyDialog);
                    DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, error.Message, LanguageControl.Ok, null, null));
                }
            );
        }

        public void DeleteEntry(CommunityContentEntry entry) {
            if (UserManager.ActiveUser != null) {
                DialogsManager.ShowDialog(
                    null,
                    new MessageDialog(
                        LanguageControl.Get(fName, 4),
                        LanguageControl.Get(fName, 5),
                        LanguageControl.Yes,
                        LanguageControl.No,
                        delegate(MessageDialogButton button) {
                            if (button == MessageDialogButton.Button1) {
                                CancellableBusyDialog busyDialog = new(string.Format(LanguageControl.Get(fName, 3), entry.Name), false);
                                DialogsManager.ShowDialog(null, busyDialog);
                                CommunityContentManager.Delete(
                                    entry.Address,
                                    UserManager.ActiveUser.UniqueId,
                                    busyDialog.Progress,
                                    delegate {
                                        DialogsManager.HideDialog(busyDialog);
                                        DialogsManager.ShowDialog(
                                            null,
                                            new MessageDialog(
                                                LanguageControl.Get(fName, 6),
                                                LanguageControl.Get(fName, 7),
                                                LanguageControl.Ok,
                                                null,
                                                null
                                            )
                                        );
                                    },
                                    delegate(Exception error) {
                                        DialogsManager.HideDialog(busyDialog);
                                        DialogsManager.ShowDialog(
                                            null,
                                            new MessageDialog(LanguageControl.Error, error.Message, LanguageControl.Ok, null, null)
                                        );
                                    }
                                );
                            }
                        }
                    )
                );
            }
        }

        public string GetFilterDisplayName(object filter) => filter is string s ? !string.IsNullOrEmpty(s)
                ? LanguageControl.Get(fName, 8)
                : LanguageControl.Get(fName, 9) :
            filter is ExternalContentType externalContentType ? ExternalContentManager.GetEntryTypeDescription(externalContentType) :
            throw new InvalidOperationException(LanguageControl.Get(fName, 10));

        public string GetOrderDisplayName(Order order) {
            return order switch {
                Order.ByRank => LanguageControl.Get(fName, "31"),
                Order.ByTime => LanguageControl.Get(fName, "32"),
                Order.ByBoutique => LanguageControl.Get(fName, "33"),
                Order.ByHide => LanguageControl.Get(fName, "34"),
                _ => throw new InvalidOperationException(LanguageControl.Get(fName, 13))
            };
        }

        public string GetSearchTypeDisplayName(SearchType searchType) {
            return searchType switch {
                SearchType.ByName => m_isCNLanguageType ? "资源名" : "Name",
                SearchType.ByAuthor => m_isCNLanguageType ? "用户名" : "User",
                SearchType.ByUserId => m_isCNLanguageType ? "用户ID" : "UID",
                _ => "null"
            };
        }

        public TreeViewNode ContentToNode(CommunityContentEntry contentEntry) {
            string title = contentEntry.Name;
            string desc =
                $"{ExternalContentManager.GetEntryTypeDescription(contentEntry.Type)} {DataSizeFormatter.Format(contentEntry.Size)} {contentEntry.ExtraText}";
            TreeViewNode node = new(title, Color.White, desc, new Color(128, 128, 128), contentEntry.Icon) { Tag = contentEntry };
            contentEntry.LinkedNode = node;
            return node;
        }

        public TreeViewNode CreateCollectionNode(CommunityContentEntry contentEntry) {
            TreeViewNode node =
                new(contentEntry.CollectionName, Color.White, contentEntry.CollectionDetails, new Color(128, 128, 128)) {
                    Tag = contentEntry.CollectionID
                };
            return node;
        }
    }
}