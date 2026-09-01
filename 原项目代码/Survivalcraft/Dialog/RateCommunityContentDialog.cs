using System.Xml.Linq;

namespace Game {
    public class RateCommunityContentDialog : Dialog {
        public string m_address;

        public string m_displayName;

        public string m_userId;

        public LabelWidget m_nameLabel;

        public StarRatingWidget m_starRating;

        public ButtonWidget m_rateButton;

        public LinkWidget m_reportLink;

        public ButtonWidget m_cancelButton;

        public RateCommunityContentDialog(string address, string displayName, string userId) {
            m_address = address;
            m_displayName = displayName;
            m_userId = userId;
            XElement node = ContentManager.Get<XElement>("Dialogs/RateCommunityContentDialog");
            LoadContents(this, node);
            m_nameLabel = Children.Find<LabelWidget>("RateCommunityContentDialog.Name");
            m_starRating = Children.Find<StarRatingWidget>("RateCommunityContentDialog.StarRating");
            m_rateButton = Children.Find<ButtonWidget>("RateCommunityContentDialog.Rate");
            m_reportLink = Children.Find<LinkWidget>("RateCommunityContentDialog.Report");
            m_cancelButton = Children.Find<ButtonWidget>("RateCommunityContentDialog.Cancel");
            m_nameLabel.Text = displayName;
            m_rateButton.IsEnabled = false;
        }

        public override void Update() {
            m_rateButton.IsEnabled = m_starRating.Rating != 0f;
            if (m_rateButton.IsClicked) {
                DialogsManager.HideDialog(this);
                CancellableBusyDialog busyDialog = new("Sending Rating", false);
                DialogsManager.ShowDialog(ParentWidget, busyDialog);
                CommunityContentManager.Rate(
                    m_address,
                    m_userId,
                    (int)m_starRating.Rating,
                    busyDialog.Progress,
                    delegate { DialogsManager.HideDialog(busyDialog); },
                    delegate { DialogsManager.HideDialog(busyDialog); }
                );
            }
            if (m_reportLink.IsClicked
                && UserManager.ActiveUser != null) {
                DialogsManager.HideDialog(this);
                DialogsManager.ShowDialog(ParentWidget, new ReportCommunityContentDialog(m_address, m_displayName, UserManager.ActiveUser.UniqueId));
            }
            if (Input.Cancel
                || m_cancelButton.IsClicked) {
                DialogsManager.HideDialog(this);
            }
        }
    }
}