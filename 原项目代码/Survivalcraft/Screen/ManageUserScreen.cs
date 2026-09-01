//abandoned
/*using Engine;
using Engine.Graphics;
using Game;
using System.Text.Json;
using System.Xml.Linq;

public class ManageUserScreen : Screen
{
    public class ComUserInfo
    {
        public int Id;
        public string UserNo;
        public string Name;
        public string Token;
        public string LastLoginTime;
        public int ErrCount;
        public int IsLock;
        public string LockTime;
        public string UnlockTime;
        public int LockDuration;
        public int Money;
        public string Authority;
        public string HeadImg;
        public int IsAdmin;
        public string RegTime;
        public string LoginIP;
        public string MGroup;
        public string PawToken;
        public string Email;
        public int Status = 1;
        public string LockReason;
        public int EmailCount;
        public string EmailTime;
        public int Die;
        public string Moblie;
        public string AreaCode;
        public Texture2D ImgTexture;
    }

    public enum Filter
    {
        All = 0,
        Blacklisted = 1,
        Admin = 2,
        Inactive = 3,
        Die = 4
    }

    public enum SearchType
    {
        ByUserId = 0,
        ByUserNo = 1,
        ByName = 2,
        ByEmail = 3,
        ByToken = 4,
        ByLoginIP = 5,
        ByLockReason = 6
    }

    public static string fName = "ManageUserScreen";

    public ListPanelWidget m_contentList;

    public ButtonWidget m_lockButton;

    public ButtonWidget m_resetButton;

    public ButtonWidget m_orderButton;

    public ButtonWidget m_filterButton;

    public ButtonWidget m_searchButton;

    public ButtonWidget m_searchTypeButton;

    public LabelWidget m_filterLabel;

    public Filter m_filter;

    public SearchType m_searchType;

    public TextBoxWidget m_searchKeyTextBox;

    public LinkWidget m_moreLink;

    public bool m_order;

    public ManageUserScreen()
    {
        XElement node = ContentManager.Get<XElement>("Screens/ManageUserScreen");
        LoadContents(this, node);
        m_contentList = Children.Find<ListPanelWidget>("ContentList");
        m_lockButton = Children.Find<ButtonWidget>("Lock");
        m_resetButton = Children.Find<ButtonWidget>("Reset");
        m_orderButton = Children.Find<ButtonWidget>("Order");
        m_filterLabel = Children.Find<LabelWidget>("Filter");
        m_filterButton = Children.Find<ButtonWidget>("ChangeFilter");
        m_searchButton = Children.Find<ButtonWidget>("Search");
        m_searchTypeButton = Children.Find<ButtonWidget>("SearchType");
        m_searchKeyTextBox = Children.Find<TextBoxWidget>("SearchKey");
        m_contentList.ItemWidgetFactory = delegate (object obj)
        {
            ComUserInfo listItem = obj as ComUserInfo;
            if (listItem != null)
            {
                XElement node2 = ContentManager.Get<XElement>("Widgets/BlocksTextureItem");
                ContainerWidget containerWidget = (ContainerWidget)LoadWidget(this, node2, null);
                RectangleWidget rectangleWidget = containerWidget.Children.Find<RectangleWidget>("BlocksTextureItem.Icon");
                LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("BlocksTextureItem.Text");
                LabelWidget labelWidget2 = containerWidget.Children.Find<LabelWidget>("BlocksTextureItem.Details");
                rectangleWidget.Subtexture = listItem.ImgTexture == null ? ContentManager.Get<Subtexture>("Textures/headimg") : new Subtexture(listItem.ImgTexture, Vector2.Zero, Vector2.One);
                rectangleWidget.TextureLinearFilter = true;
                labelWidget.Text = $"{listItem.Name}   ID:{listItem.Id}   账号:{listItem.UserNo}";
                if (listItem.IsLock == 1)
                {
                    labelWidget2.Text = "锁定时长: " + ((int)(listItem.LockDuration / 8.64f) / 10000f) + "天";
                    labelWidget2.Text += "  解锁时间: " + GetMsg(listItem.UnlockTime);
                    labelWidget2.Text += "  锁定原因:" + GetMsg(listItem.LockReason);
                }
                else
                {
                    labelWidget2.Text = $"经验:{GetMsg(listItem.Money)}  邮箱:{GetMsg(listItem.Email)} IP地址:{GetMsg(listItem.LoginIP)}";
                }
                labelWidget.Color = Color.White;
                if (listItem.IsLock == 1)
                {
                    labelWidget.Color = Color.LightBlue;
                }
                else if (listItem.IsAdmin == 1)
                {
                    labelWidget.Color = Color.Green;
                }
                else if (listItem.Die == 1)
                {
                    labelWidget.Color = Color.Red;
                }
                else if (listItem.Status == 0)
                {
                    labelWidget.Color = Color.Gray;
                }
                return containerWidget;
            }
            else
            {
                XElement node2 = ContentManager.Get<XElement>("Widgets/CommunityContentItemMore");
                ContainerWidget containerWidget = (ContainerWidget)LoadWidget(this, node2, null);
                m_moreLink = containerWidget.Children.Find<LinkWidget>("CommunityContentItemMore.Link");
                m_moreLink.Tag = obj as string;
                return containerWidget;
            }
        };
        m_contentList.ItemClicked += obj =>
        {
            ComUserInfo listItem = obj as ComUserInfo;
            if (listItem != null && m_contentList.SelectedItem == listItem)
            {
                string msg = $"用户ID: {listItem.Id}\n用户名: {GetMsg(listItem.UserNo)}\n昵称: {GetMsg(listItem.Name)}\n邮箱{GetMsg(listItem.Email)}\nIP:{GetMsg(listItem.LoginIP)}";
                msg += $"\n用户Token: {GetMsg(listItem.Token)}\n状态: " + (listItem.IsLock == 1 ? "锁定" : (listItem.Status == 0 ? "未激活" : (listItem.Die == 1 ? "死鱼" : "正常")));
                msg += "\n是否为管理: " + (listItem.IsAdmin == 1 ? "是" : "否") + "  " + "权限等级: " + listItem.Authority;
                msg += $"\n找回密码Token: {GetMsg(listItem.PawToken)}\n称号组: {GetMsg(listItem.MGroup)}\n注册时间: {GetMsg(listItem.RegTime)}\n最后登录时间: {GetMsg(listItem.LastLoginTime)}";
                msg += $"\n当天发送邮件次数: {GetMsg(listItem.EmailCount)}\n邮箱锁定时间: {GetMsg(listItem.EmailTime)}";
                msg += $"\n手机号: {GetMsg(listItem.Moblie)}\n区号: {GetMsg(listItem.AreaCode)}";
                msg += "\n上次锁定时间: " + GetMsg(listItem.LockTime) + "\n锁定原因: " + GetMsg(listItem.LockReason);
                msg += "\n锁定时长: " + ((int)(listItem.LockDuration / 8.64f) / 10000f) + "天\n解锁时间: " + GetMsg(listItem.UnlockTime);
                MessageDialog messageDialog = new("详细信息:" + listItem.Name, msg, LanguageControl.Ok, null, null);
                DialogsManager.ShowDialog(null, messageDialog);
            }
        };
    }

    public override void Enter(object[] parameters)
    {
        m_filter = Filter.All;
        m_searchType = SearchType.ByName;
        m_order = false;
        UpdateList(null);
    }

    public override void Update()
    {
        if (m_contentList.SelectedItem != null && m_contentList.SelectedItem is ComUserInfo)
        {
            ComUserInfo item = (ComUserInfo)m_contentList.SelectedItem;
            if (item.IsAdmin == 1)
            {
                m_lockButton.IsEnabled = false;
                m_resetButton.IsEnabled = false;
            }
            else
            {
                m_lockButton.IsEnabled = true;
                m_resetButton.IsEnabled = true;
            }
            m_lockButton.Text = (item.IsLock == 1) ? "解锁" : "锁定";
        }
        else
        {
            m_lockButton.IsEnabled = false;
            m_resetButton.IsEnabled = false;
        }
        m_filterLabel.Text = GetFilterDisplayName(m_filter);
        if (m_filterButton.IsClicked)
        {
            List<int> filters = [.. EnumUtils.GetEnumValues<Filter>()];
            DialogsManager.ShowDialog(null, new ListSelectionDialog("请选择", filters, 60f, item => GetFilterDisplayName((Filter)item), delegate (object result)
            {
                m_filter = (Filter)result;
                UpdateList(null);
            }));
        }
        m_searchTypeButton.Text = GetSearchTypeName(m_searchType);
        if (m_searchTypeButton.IsClicked)
        {
            List<int> searchTypes = [.. EnumUtils.GetEnumValues<SearchType>()];
            DialogsManager.ShowDialog(null, new ListSelectionDialog("请选择", searchTypes, 60f, item => GetSearchTypeName((SearchType)item), delegate (object result)
            {
                m_searchType = (SearchType)result;
            }));
        }
        if (m_searchButton.IsClicked)
        {
            UpdateList(null);
        }
        if (m_lockButton.IsClicked && m_contentList.SelectedItem != null && m_contentList.SelectedItem is ComUserInfo)
        {
            ComUserInfo item = (ComUserInfo)m_contentList.SelectedItem;
            if (item.IsLock == 0)
            {
                DialogsManager.ShowDialog(null, new TextBoxDialog("请输入锁定原因", item.LockReason, 1024, delegate (string reason)
                {
                    DialogsManager.ShowDialog(null, new TextBoxDialog("请输入锁定时长，单位为天", "1", 1024, delegate (string duration)
                    {
                        if (!string.IsNullOrEmpty(reason) && !string.IsNullOrEmpty(duration))
                        {
                            CancellableBusyDialog busyDialog = new("操作等待中", autoHideOnCancel: false);
                            DialogsManager.ShowDialog(null, busyDialog);
                            int s_duration = (int)(double.Parse(duration) * 86400);
                            CommunityContentManager.UpdateLockState(item.Id, 1, reason, s_duration, busyDialog.Progress, delegate (byte[] data)
                            {
                                DialogsManager.HideDialog(busyDialog);
                                UpdateList(null);
                                JsonElement result = JsonDocument.Parse(data).RootElement;
                                string msg = result[0].GetInt32() == 200 ? "成功锁定：" + item.Name : result[1].GetString();
                                DialogsManager.ShowDialog(null, new MessageDialog("操作成功", msg, LanguageControl.Ok, null, null));
                            }, delegate (Exception e)
                            {
                                DialogsManager.HideDialog(busyDialog);
                                DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, e.Message, LanguageControl.Ok, null, null));
                            });
                        }
                    }));
                }));
            }
            else
            {
                DialogsManager.ShowDialog(null, new MessageDialog("确认解锁？", item.Name, LanguageControl.Ok, LanguageControl.Cancel, delegate (MessageDialogButton button)
                {
                    if (button == MessageDialogButton.Button1)
                    {
                        CancellableBusyDialog busyDialog = new("操作等待中", autoHideOnCancel: false);
                        DialogsManager.ShowDialog(null, busyDialog);
                        CommunityContentManager.UpdateLockState(item.Id, 0, "", 0, busyDialog.Progress, delegate (byte[] data)
                        {
                            DialogsManager.HideDialog(busyDialog);
                            UpdateList(null);
                            JsonElement result = JsonDocument.Parse(data).RootElement;
                            string msg = result[0].GetInt32() == 200 ? "成功解锁：" + item.Name : result[1].GetString();
                            DialogsManager.ShowDialog(null, new MessageDialog("操作成功", msg, LanguageControl.Ok, null, null));
                        }, delegate (Exception e)
                        {
                            DialogsManager.HideDialog(busyDialog);
                            DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, e.Message, LanguageControl.Ok, null, null));
                        });
                    }
                }));
            }
        }
        if (m_resetButton.IsClicked && m_contentList.SelectedItem != null && m_contentList.SelectedItem is ComUserInfo)
        {
            ComUserInfo item = (ComUserInfo)m_contentList.SelectedItem;
            DialogsManager.ShowDialog(null, new MessageDialog("确认重置密码？", item.Name, LanguageControl.Ok, LanguageControl.Cancel, delegate (MessageDialogButton button)
            {
                if (button == MessageDialogButton.Button1)
                {
                    CancellableBusyDialog busyDialog = new("操作等待中", autoHideOnCancel: false);
                    DialogsManager.ShowDialog(null, busyDialog);
                    CommunityContentManager.ResetPassword(item.Id, busyDialog.Progress, delegate (byte[] data)
                    {
                        DialogsManager.HideDialog(busyDialog);
                        JsonElement result = JsonDocument.Parse(data).RootElement;
                        string msg = result[0].GetInt32() == 200 ? "成功重置密码，密码为123456" : result[1].GetString();
                        DialogsManager.ShowDialog(null, new MessageDialog("操作成功", msg, LanguageControl.Ok, null, null));
                    }, delegate (Exception e)
                    {
                        DialogsManager.HideDialog(busyDialog);
                        DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, e.Message, LanguageControl.Ok, null, null));
                    });
                }
            }));
        }
        if (m_orderButton.IsClicked)
        {
            m_order = !m_order;
            UpdateList(null);
        }
        if (m_moreLink != null && m_moreLink.IsClicked)
        {
            UpdateList((string)m_moreLink.Tag);
        }
        if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
        {
            ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
        }
    }

    public string GetMsg(object msg)
    {
        if (string.IsNullOrEmpty(msg.ToString()))
        {
            return "Null";
        }
        return msg.ToString();
    }

    public virtual void UpdateList(string cursor)
    {
        if (string.IsNullOrEmpty(cursor))
        {
            m_contentList.ClearItems();
            m_contentList.ScrollPosition = 0f;
        }
        CancellableBusyDialog busyDialog = new(LanguageControl.Get("CommunityContentScreen", 2), autoHideOnCancel: false);
        DialogsManager.ShowDialog(null, busyDialog);
        int order = m_order ? 1 : 0;
        //CommunityContentManager.UserList(cursor, m_searchKeyTextBox.Text, m_searchType.ToString(), m_filter.ToString(), order, busyDialog.Progress, delegate (List<ComUserInfo> list, string nextCursor)
        //{
            //DialogsManager.HideDialog(busyDialog);
            //while (m_contentList.Items.Count > 0 && !(m_contentList.Items[^1] is ComUserInfo))
            //{
                //m_contentList.RemoveItemAt(m_contentList.Items.Count - 1);
            //}
            //foreach (ComUserInfo item in list)
            //{
                //m_contentList.AddItem(item);
            //}
            //if (list.Count > 0 && !string.IsNullOrEmpty(nextCursor))
            //{
                //m_contentList.AddItem(nextCursor);
            //}
        //}, delegate (Exception error)
        //{
            //DialogsManager.HideDialog(busyDialog);
            //DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, error.Message, LanguageControl.Ok, null, null));
        //});
    }

    public string GetFilterDisplayName(Filter filter)
    {
        switch (filter)
        {
            case Filter.All: return "全部名单";
            case Filter.Admin: return "管理员名单";
            case Filter.Blacklisted: return "封禁名单";
            case Filter.Inactive: return "未激活名单";
            case Filter.Die: return "死鱼名单";
        }
        return "";
    }

    public string GetSearchTypeName(SearchType searchType)
    {
        switch (searchType)
        {
            case SearchType.ByName: return "昵称";
            case SearchType.ByEmail: return "邮箱";
            case SearchType.ByUserId: return "用户ID";
            case SearchType.ByUserNo: return "用户名";
            case SearchType.ByToken: return "Token";
            case SearchType.ByLoginIP: return "登录IP";
            case SearchType.ByLockReason: return "锁定原因";
        }
        return "";
    }
}
*/

