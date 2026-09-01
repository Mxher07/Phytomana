using System.Xml.Linq;
using Engine;

namespace Game {
    public class OriginalCommunityContentScreen : Screen {
        public enum Order {
            ByRank = 0,
            ByTime = 1,
            ByDownloads = 2,
            BySize = 3,
            ByVersionNewest = 4,
            ByVersionOldest = 5
        }

        public ListPanelWidget m_listPanel;

        public LinkWidget m_moreLink;

        public LabelWidget m_orderLabel;

        public ButtonWidget m_changeOrderButton;

        public LabelWidget m_filterLabel;

        public ButtonWidget m_changeFilterButton;

        public TextBoxWidget m_searchTextBox;

        public LabelWidget m_searchLabel;

        public LinkWidget m_clearSearchLink;

        public ButtonWidget m_downloadButton;

        public ButtonWidget m_deleteButton;

        public ButtonWidget m_moreOptionsButton;

        public const string fName = "OriginalCommunityContentScreen";
        public const string fName1 = "CommunityContentScreen";

        public CancellableBusyDialog m_busyDialog = new($"[{fName1}:2]", false);

        public object m_filter;

        public Order m_order;

        public string m_search;

        public double m_itemsCacheExpiryTime;

        public int m_populatingListCount;

        public Dictionary<string, IEnumerable<object>> m_itemsCache = new();

        public OriginalCommunityContentScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/OriginalCommunityContentScreen");
            LoadContents(this, node);
            m_listPanel = Children.Find<ListPanelWidget>("List");
            m_orderLabel = Children.Find<LabelWidget>("Order");
            m_changeOrderButton = Children.Find<ButtonWidget>("ChangeOrder");
            m_filterLabel = Children.Find<LabelWidget>("Filter");
            m_changeFilterButton = Children.Find<ButtonWidget>("ChangeFilter");
            m_searchTextBox = Children.Find<TextBoxWidget>("Search");
            m_searchLabel = Children.Find<LabelWidget>("SearchLabel");
            m_clearSearchLink = Children.Find<LinkWidget>("ClearSearchLink");
            m_downloadButton = Children.Find<ButtonWidget>("Download");
            m_deleteButton = Children.Find<ButtonWidget>("Delete");
            m_moreOptionsButton = Children.Find<ButtonWidget>("MoreOptions");
            m_listPanel.ItemWidgetFactory = delegate(object item) {
                if (item is OriginalCommunityContentEntry communityContentEntry) {
                    XElement node2 = ContentManager.Get<XElement>("Widgets/CommunityContentItem");
                    ContainerWidget containerWidget = (ContainerWidget)LoadWidget(this, node2, null);
                    containerWidget.Children.Find<RectangleWidget>("CommunityContentItem.Icon").Subtexture =
                        ExternalContentManager.GetEntryTypeIcon(communityContentEntry.Type);
                    containerWidget.Children.Find<LabelWidget>("CommunityContentItem.Text").Text = communityContentEntry.Name;
                    containerWidget.Children.Find<LabelWidget>("CommunityContentItem.Details").Text =
                        $"{ExternalContentManager.GetEntryTypeDescription(communityContentEntry.Type)} {DataSizeFormatter.Format(communityContentEntry.Size)}";
                    containerWidget.Children.Find<StarRatingWidget>("CommunityContentItem.Rating").Rating = communityContentEntry.RatingsAverage;
                    containerWidget.Children.Find<StarRatingWidget>("CommunityContentItem.Rating").IsVisible =
                        communityContentEntry.RatingsAverage > 0f;
                    containerWidget.Children.Find<LabelWidget>("CommunityContentItem.ExtraText").Text = communityContentEntry.ExtraText;
                    return containerWidget;
                }
                XElement node3 = ContentManager.Get<XElement>("Widgets/CommunityContentItemMore");
                ContainerWidget containerWidget2 = (ContainerWidget)LoadWidget(this, node3, null);
                m_moreLink = containerWidget2.Children.Find<LinkWidget>("CommunityContentItemMore.Link");
                m_moreLink.Tag = item as string;
                return containerWidget2;
            };
            m_searchTextBox.TextChanged += delegate {
                m_search = m_searchTextBox.Text.Replace("\n", "").Trim();
                PopulateList(null);
            };
            m_listPanel.SelectionChanged += delegate {
                if (m_listPanel.SelectedItem != null
                    && !(m_listPanel.SelectedItem is OriginalCommunityContentEntry)) {
                    m_listPanel.SelectedItem = null;
                }
            };
        }

        public override void Enter(object[] parameters) {
            m_filter = string.Empty;
            m_order = Order.ByRank;
            m_search = string.Empty;
            PopulateList(null);
        }

        public override void Update() {
            OriginalCommunityContentEntry communityContentEntry = m_listPanel.SelectedItem as OriginalCommunityContentEntry;
            m_downloadButton.IsEnabled = communityContentEntry != null;
            m_deleteButton.IsEnabled = UserManager.ActiveUser != null
                && communityContentEntry != null
                && m_filter as string == UserManager.ActiveUser.UniqueId;
            m_orderLabel.Text = GetOrderDisplayName(m_order);
            m_filterLabel.Text = GetFilterDisplayName(m_filter);
            if (!m_searchTextBox.HasFocus) {
                m_searchTextBox.Text = m_search;
            }
            m_searchLabel.IsVisible = string.IsNullOrEmpty(m_searchTextBox.Text) && !m_searchTextBox.HasFocus;
            m_clearSearchLink.IsVisible = !string.IsNullOrEmpty(m_searchTextBox.Text) || !string.IsNullOrEmpty(m_search) || m_searchTextBox.HasFocus;
            if (m_populatingListCount > 0
                && m_busyDialog.RootWidget != ScreensManager.RootWidget) {
                DialogsManager.ShowDialog(null, m_busyDialog);
            }
            if (m_populatingListCount <= 0
                && m_busyDialog.RootWidget == ScreensManager.RootWidget) {
                DialogsManager.HideDialog(m_busyDialog);
            }
            if (m_changeOrderButton.IsClicked) {
                List<Order> items = EnumUtils.GetEnumValues<Order>().Cast<Order>().ToList();
                DialogsManager.ShowDialog(
                    null,
                    new ListSelectionDialog(
                        LanguageControl.Get(fName, "1"),
                        items,
                        60f,
                        item => GetOrderDisplayName((Order)item),
                        delegate(object item) {
                            m_order = (Order)item;
                            PopulateList(null);
                        }
                    )
                );
            }
            if (m_changeFilterButton.IsClicked) {
                List<object> list = [string.Empty];
                foreach (OriginalExternalContentType item in from OriginalExternalContentType t in EnumUtils
                        .GetEnumValues<OriginalExternalContentType>()
                    where IsEntryTypeDownloadSupported(t)
                    select t) {
                    list.Add(item);
                }
                if (UserManager.ActiveUser != null) {
                    list.Add(UserManager.ActiveUser.UniqueId);
                }
                if (m_listPanel.SelectedItem is OriginalCommunityContentEntry communityContentEntry2) {
                    list.Add(communityContentEntry2.Url);
                }
                DialogsManager.ShowDialog(
                    null,
                    new ListSelectionDialog(
                        LanguageControl.Get(fName, "2"),
                        list,
                        60f,
                        item => GetFilterDisplayName(item),
                        delegate(object item) {
                            m_filter = item;
                            PopulateList(null);
                        }
                    )
                );
            }
            if (m_clearSearchLink.IsClicked) {
                m_search = string.Empty;
                PopulateList(null);
            }
            if (m_downloadButton.IsClicked
                && communityContentEntry != null) {
                DownloadEntry(communityContentEntry);
            }
            if (m_deleteButton.IsClicked
                && communityContentEntry != null) {
                DeleteEntry(communityContentEntry);
            }
            if (m_moreOptionsButton.IsClicked) {
                DialogsManager.ShowDialog(null, new MoreCommunityLinkDialog());
            }
            if (m_moreLink != null
                && m_moreLink.IsClicked) {
                PopulateList((string)m_moreLink.Tag);
            }
            if (Input.Back
                || Children.Find<BevelledButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen("Content");
            }
            if (Input.Hold.HasValue
                && Input.HoldTime > 2f
                && Input.Hold.Value.Y < 20f) {
                m_itemsCacheExpiryTime = 0.0;
                Task.Delay(250).Wait();
            }
        }

        public void PopulateList(string cursor) {
            string text = string.Empty;
            string text2 = string.Empty;
            if (m_filter is string) {
                string text3 = (string)m_filter;
                if (text3.StartsWith("http")) {
                    text2 = text3;
                }
                else {
                    text = text3;
                }
            }
            string text4 = m_filter is OriginalExternalContentType ? m_filter.ToString() : string.Empty;
            string text5 = string.Empty;
            if (SettingsManager.OriginalCommunityContentMode == CommunityContentMode.Strict) {
                text5 = "1";
            }
            if (SettingsManager.OriginalCommunityContentMode == CommunityContentMode.Normal) {
                text5 = "0";
            }
            string text6 = m_order.ToString();
            string cacheKey = $"{text}\n{text2}\n{text4}\n{text5}\n{m_search}\n{text6}";
            m_moreLink = null;
            if (string.IsNullOrEmpty(cursor)
                && m_itemsCacheExpiryTime != 0.0
                && Time.RealTime < m_itemsCacheExpiryTime
                && m_itemsCache.TryGetValue(cacheKey, out IEnumerable<object> value)) {
                m_listPanel.ClearItems();
                m_listPanel.AddItems(value);
                m_listPanel.ScrollPosition = 0f;
                return;
            }
            object[] prefixItems = !string.IsNullOrEmpty(cursor) ? m_listPanel.Items.Where(i => i is OriginalCommunityContentEntry).ToArray() : [];
            m_populatingListCount++;
            OriginalCommunityContentManager.List(
                cursor,
                text,
                text2,
                text4,
                text5,
                m_search,
                text6,
                m_busyDialog.Progress,
                delegate(List<OriginalCommunityContentEntry> list, string nextCursor) {
                    m_populatingListCount--;
                    m_listPanel.ClearItems();
                    m_listPanel.AddItems(prefixItems);
                    m_listPanel.AddItems(list);
                    if (prefixItems.Length == 0) {
                        m_listPanel.ScrollPosition = 0f;
                    }
                    if (list.Count > 0
                        && !string.IsNullOrEmpty(nextCursor)) {
                        m_listPanel.AddItem(nextCursor);
                    }
                    m_itemsCache[cacheKey] = new List<object>(m_listPanel.Items);
                    m_itemsCacheExpiryTime = Time.RealTime + 120.0;
                },
                delegate(Exception error) {
                    m_populatingListCount--;
                    DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, error.Message, LanguageControl.Ok, null, null));
                }
            );
        }

        public void DownloadEntry(OriginalCommunityContentEntry entry) {
            string userId = UserManager.ActiveUser != null ? UserManager.ActiveUser.UniqueId : string.Empty;
            CancellableBusyDialog busyDialog = new(string.Format(LanguageControl.Get(fName1, "1"), entry.Name), false);
            DialogsManager.ShowDialog(null, busyDialog);
            OriginalCommunityContentManager.Download(
                entry.Url,
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

        public void DeleteEntry(OriginalCommunityContentEntry entry) {
            if (UserManager.ActiveUser == null) {
                return;
            }
            DialogsManager.ShowDialog(
                null,
                new MessageDialog(
                    LanguageControl.Get(fName1, "4"),
                    LanguageControl.Get(fName1, "5"),
                    LanguageControl.Yes,
                    LanguageControl.No,
                    delegate(MessageDialogButton button) {
                        if (button == MessageDialogButton.Button1) {
                            CancellableBusyDialog busyDialog = new(string.Format(LanguageControl.Get(fName1, "3"), entry.Name), false);
                            DialogsManager.ShowDialog(null, busyDialog);
                            OriginalCommunityContentManager.Delete(
                                entry.Url,
                                UserManager.ActiveUser.UniqueId,
                                busyDialog.Progress,
                                delegate {
                                    DialogsManager.HideDialog(busyDialog);
                                    DialogsManager.ShowDialog(
                                        null,
                                        new MessageDialog(
                                            LanguageControl.Get(fName1, "6"),
                                            LanguageControl.Get(fName1, "7"),
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

        public static string GetFilterDisplayName(object filter) {
            if (filter is string) {
                string text = (string)filter;
                return text.StartsWith("http") ? LanguageControl.Get(fName, "3") :
                    string.IsNullOrEmpty(text) ? LanguageControl.Get(fName1, "9") : LanguageControl.Get(fName1, "8");
            }
            if (filter is OriginalExternalContentType) {
                return GetEntryTypeDescription((OriginalExternalContentType)filter);
            }
            throw new InvalidOperationException(LanguageControl.Get(fName1, "10"));
        }

        public static string GetOrderDisplayName(Order order) {
            return order switch {
                Order.ByRank => LanguageControl.Get(fName, "4"),
                Order.ByTime => LanguageControl.Get(fName, "5"),
                Order.ByDownloads => LanguageControl.Get(fName, "6"),
                Order.BySize => LanguageControl.Get(fName, "7"),
                Order.ByVersionNewest => LanguageControl.Get(fName, "8"),
                Order.ByVersionOldest => LanguageControl.Get(fName, "9"),
                _ => throw new InvalidOperationException(LanguageControl.Get(fName1, "13"))
            };
        }

        public static bool IsEntryTypeDownloadSupported(OriginalExternalContentType type) {
            return type switch {
                OriginalExternalContentType.World => true,
                OriginalExternalContentType.BlocksTexture => true,
                OriginalExternalContentType.CharacterSkin => true,
                OriginalExternalContentType.FurniturePack => true,
                _ => false
            };
        }

        public static string GetEntryTypeDescription(OriginalExternalContentType type) {
            const string fName2 = "ExternalContentManager";
            return type switch {
                OriginalExternalContentType.Directory => LanguageControl.Get(fName2, "Directory"),
                OriginalExternalContentType.World => LanguageControl.Get(fName2, "World"),
                OriginalExternalContentType.BlocksTexture => LanguageControl.Get(fName2, "Blocks Texture"),
                OriginalExternalContentType.CharacterSkin => LanguageControl.Get(fName2, "Character Skin"),
                OriginalExternalContentType.FurniturePack => LanguageControl.Get(fName2, "Furniture Pack"),
                _ => string.Empty
            };
        }
    }
}