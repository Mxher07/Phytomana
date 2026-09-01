using Engine;
using System.Xml.Linq;

namespace Game {
    public class SelectCommunityServerDialog: Dialog {
        public ListPanelWidget m_listPanel;
        public ButtonWidget m_openButton;
        public ButtonWidget m_addButton;
        public ButtonWidget m_editButton;

        public SelectCommunityServerDialog() {
            XElement node = ContentManager.Get<XElement>("Dialogs/SelectCommunityServerDialog");
            LoadContents(this, node);
            m_listPanel = Children.Find<ListPanelWidget>("SelectCommunityServerDialog.ListPanel");
            m_openButton = Children.Find<ButtonWidget>("SelectCommunityServerDialog.Open");
            m_addButton = Children.Find<ButtonWidget>("SelectCommunityServerDialog.Add");
            m_editButton = Children.Find<ButtonWidget>("SelectCommunityServerDialog.Edit");
            m_listPanel.ItemWidgetFactory = o => {
                if (o is not CommunityServerManager.Info info) {
                    return null;
                }
                LabelWidget labelWidget = new () {
                    HorizontalAlignment = WidgetAlignment.Center,
                    VerticalAlignment = WidgetAlignment.Center
                };
                if (info == CommunityServerManager.DefaultOriginalInfo) {
                    labelWidget.Text = LanguageControl.Get("ContentScreen", "CommunityType", "OriginalCommunity");
                }
                else if (info == CommunityServerManager.DefaultChineseInfo) {
                    labelWidget.Text = LanguageControl.Get("ContentScreen", "CommunityType", "ChineseCommunity");
                }
                else {
                    labelWidget.Text = info.Name;
                }
                return labelWidget;
            };
            m_listPanel.ItemClicked = o => {
                if (o == m_listPanel.SelectedItem
                    && o is CommunityServerManager.Info info) {
                    OpenServerScreen(info);
                    DialogsManager.HideDialog(this);
                }
            };
            UpdateServerList();
        }

        public void UpdateServerList() {
            m_listPanel.ClearItems();
            m_listPanel.AddItems(CommunityServerManager.GetAllInfos());
            m_listPanel.ScrollPosition = 0f;
        }

        public void AddServer(CommunityServerManager.Info info) {
            CommunityServerManager.UserInfos.Add(info);
            m_listPanel.AddItem(info);
            m_listPanel.SelectedItem = info;
        }

        public void RemoveServer(CommunityServerManager.Info info) {
            CommunityServerManager.UserInfos.Remove(info);
            m_listPanel.RemoveItem(info);
        }

        public void OpenServerScreen(CommunityServerManager.Info info) {
            if (info == null) {
                return;
            }
            switch (info.Type) {
                case CommunityServerManager.Type.Original:
                    CommunityServerManager.ChangeOriginalInfo(info);
                    ScreensManager.SwitchScreen("OriginalCommunityContent");
                    break;
                case CommunityServerManager.Type.Chinese:
                    CommunityServerManager.ChangeChineseInfo(info);
                    ScreensManager.SwitchScreen("CommunityContent");
                    break;
            }
        }

        public override void Update() {
            if (m_listPanel.SelectedItem is CommunityServerManager.Info selected) {
                if (m_openButton.IsClicked) {
                    OpenServerScreen(selected);
                    DialogsManager.HideDialog(this);
                }
                if (m_editButton.IsClicked) {
                    DialogsManager.ShowDialog(null, new EditCommunityServerInfoDialog(this, selected));
                }
            }
            if (m_addButton.IsClicked) {
                DialogsManager.ShowDialog(null, new EditCommunityServerInfoDialog(this));
            }
            if (Input.Back
                || Input.Cancel
                || (Input.Tap.HasValue && !HitTest(Input.Tap.Value))) {
                DialogsManager.HideDialog(this);
            }
        }
    }
}