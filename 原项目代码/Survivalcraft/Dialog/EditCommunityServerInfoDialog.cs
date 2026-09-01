using System.Xml.Linq;

namespace Game {
    public class EditCommunityServerInfoDialog : Dialog {
        public SelectCommunityServerDialog m_parentDialog;
        public CommunityServerManager.Info m_info;
        public bool m_changed;
        public bool m_toAdd;//false: edit, true: add

        public CheckboxWidget m_originalCheckbox;
        public CheckboxWidget m_chineseCheckbox;
        public TextBoxWidget m_nameTextBox;
        public TextBoxWidget m_apiUrlTextBox;
        public TextBoxWidget m_websiteUrlTextBox;
        public ButtonWidget m_okButton;
        public ButtonWidget m_removeButton;
        public ButtonWidget m_cancelButton;

        public EditCommunityServerInfoDialog(SelectCommunityServerDialog parentDialog, CommunityServerManager.Info info = null) {
            m_parentDialog = parentDialog;
            if (info == null) {
                m_info = CommunityServerManager.DefaultOriginalInfo.Clone();
                m_changed = true;
                m_toAdd = true;
            }
            else if (info == CommunityServerManager.DefaultOriginalInfo || info == CommunityServerManager.DefaultChineseInfo) {
                m_info = info.Clone();
                m_changed = true;
                m_toAdd = true;
            }
            else {
                m_info = info;
                m_changed = false;
                m_toAdd = false;
            }
            if (m_info.Name == string.Empty) {
                switch (m_info.Type) {
                    case CommunityServerManager.Type.Original:
                        m_info.Name =
                            $"{LanguageControl.Get("ContentScreen", "CommunityType", "OriginalCommunity")} ({LanguageControl.Get("ContentWidgets", "ViewGameLogDialog", "1")})";
                        break;
                    case CommunityServerManager.Type.Chinese:
                        m_info.Name =
                            $"{LanguageControl.Get("ContentScreen", "CommunityType", "ChineseCommunity")} ({LanguageControl.Get("ContentWidgets", "ViewGameLogDialog", "1")})";
                        break;
                }
            }
            XElement node = ContentManager.Get<XElement>("Dialogs/EditCommunityServerInfoDialog");
            LoadContents(this, node);
            m_originalCheckbox = Children.Find<CheckboxWidget>("EditCommunityServerInfoDialog.OriginalCheckbox");
            m_chineseCheckbox = Children.Find<CheckboxWidget>("EditCommunityServerInfoDialog.ChineseCheckbox");
            m_nameTextBox = Children.Find<TextBoxWidget>("EditCommunityServerInfoDialog.Name");
            m_apiUrlTextBox = Children.Find<TextBoxWidget>("EditCommunityServerInfoDialog.ApiUrl");
            m_websiteUrlTextBox = Children.Find<TextBoxWidget>("EditCommunityServerInfoDialog.WebsiteUrl");
            m_okButton = Children.Find<ButtonWidget>("EditCommunityServerInfoDialog.Ok");
            m_removeButton = Children.Find<ButtonWidget>("EditCommunityServerInfoDialog.Remove");
            m_cancelButton = Children.Find<ButtonWidget>("EditCommunityServerInfoDialog.Cancel");
            if (m_info.Type == CommunityServerManager.Type.Chinese) {
                m_chineseCheckbox.IsChecked = true;
            }else {
                m_originalCheckbox.IsChecked = true;
            }
            m_nameTextBox.Text = m_info.Name;
            m_apiUrlTextBox.Text = m_info.ApiUrl;
            m_websiteUrlTextBox.Text = m_info.WebsiteUrl;
        }

        public override void Update() {
            if (m_originalCheckbox.IsClicked && !m_originalCheckbox.IsChecked) {
                m_originalCheckbox.IsChecked = true;
                m_chineseCheckbox.IsChecked = false;
                m_changed = true;
                AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
            }
            if (m_chineseCheckbox.IsClicked && !m_chineseCheckbox.IsChecked) {
                m_originalCheckbox.IsChecked = false;
                m_chineseCheckbox.IsChecked = true;
                m_changed = true;
                AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
            }
            if (m_nameTextBox.Text != m_info.Name) {
                m_changed = true;
            }
            if (m_apiUrlTextBox.Text != m_info.ApiUrl) {
                m_changed = true;
            }
            if (m_websiteUrlTextBox.Text != m_info.WebsiteUrl) {
                m_changed = true;
            }
            m_removeButton.IsEnabled = !m_changed;
            if (m_okButton.IsClicked) {
                if (m_changed) {
                    if (m_toAdd) {
                        m_parentDialog.AddServer(m_info);
                    }
                    else {
                        m_info.Type = m_chineseCheckbox.IsChecked ? CommunityServerManager.Type.Chinese : CommunityServerManager.Type.Original;
                        m_info.Name = m_nameTextBox.Text;
                        m_info.ApiUrl = m_apiUrlTextBox.Text;
                        m_info.WebsiteUrl = m_websiteUrlTextBox.Text;
                        m_parentDialog.UpdateServerList();
                        m_parentDialog.m_listPanel.SelectedItem = m_info;
                    }
                }
                DialogsManager.HideDialog(this);
            }
            if (m_removeButton.IsClicked && !m_changed) {
                m_parentDialog.RemoveServer(m_info);
                DialogsManager.HideDialog(this);
            }
            if (Input.Back
                || Input.Cancel
                || (Input.Tap.HasValue && !HitTest(Input.Tap.Value))
                || m_cancelButton.IsClicked) {
                DialogsManager.HideDialog(this);
            }
        }
    }
}