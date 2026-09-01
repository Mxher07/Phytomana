using System.Xml.Linq;

namespace Game {
    public class MoreCommunityLinkDialog : Dialog {
        public LabelWidget m_userLabel;

        public ButtonWidget m_changeUserButton;

        public LabelWidget m_userIdLabel;

        public ButtonWidget m_copyUserIdButton;

        public ButtonWidget m_publishButton;

        public ButtonWidget m_closeButton;

        public const string fName = "MoreCommunityLinkDialog";

        public MoreCommunityLinkDialog() {
            XElement node = ContentManager.Get<XElement>("Dialogs/MoreCommunityLinkDialog");
            LoadContents(this, node);
            m_userLabel = Children.Find<LabelWidget>("MoreCommunityLinkDialog.User");
            m_changeUserButton = Children.Find<ButtonWidget>("MoreCommunityLinkDialog.ChangeUser");
            m_userIdLabel = Children.Find<LabelWidget>("MoreCommunityLinkDialog.UserId");
            m_copyUserIdButton = Children.Find<ButtonWidget>("MoreCommunityLinkDialog.CopyUserId");
            m_publishButton = Children.Find<ButtonWidget>("MoreCommunityLinkDialog.Publish");
            m_closeButton = Children.Find<ButtonWidget>("MoreCommunityLinkDialog.Close");
        }

        public override void Update() {
            string text = UserManager.ActiveUser != null ? UserManager.ActiveUser.DisplayName : LanguageControl.Get(fName, "1");
            if (text.Length > 15) {
                text = $"{text.Substring(0, 15)}...";
            }
            m_userLabel.Text = text;
            string text2 = UserManager.ActiveUser != null ? UserManager.ActiveUser.UniqueId : LanguageControl.Get(fName, "1");
            if (text2.Length > 15) {
                text2 = $"{text2.Substring(0, 15)}...";
            }
            m_userIdLabel.Text = text2;
            m_publishButton.IsEnabled = UserManager.ActiveUser != null;
            m_copyUserIdButton.IsEnabled = UserManager.ActiveUser != null;
            if (m_changeUserButton.IsClicked) {
                DialogsManager.ShowDialog(
                    ParentWidget,
                    new ListSelectionDialog(
                        LanguageControl.Get(fName, "2"),
                        UserManager.GetUsers(),
                        60f,
                        item => ((UserInfo)item).DisplayName,
                        delegate(object item) { UserManager.ActiveUser = (UserInfo)item; }
                    )
                );
            }
            if (m_copyUserIdButton.IsClicked
                && UserManager.ActiveUser != null) {
                ClipboardManager.ClipboardString = UserManager.ActiveUser.UniqueId;
            }
            if (m_publishButton.IsClicked
                && UserManager.ActiveUser != null) {
                DialogsManager.ShowDialog(ParentWidget, new PublishCommunityLinkDialog(UserManager.ActiveUser.UniqueId, null, null));
            }
            if (Input.Cancel
                || m_closeButton.IsClicked) {
                DialogsManager.HideDialog(this);
            }
        }
    }
}