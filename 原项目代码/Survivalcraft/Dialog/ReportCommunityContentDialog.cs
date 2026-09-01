using System.Xml.Linq;

namespace Game {
    public class ReportCommunityContentDialog : Dialog {
        public string m_address;

        public string m_userId;

        public LabelWidget m_nameLabel;

        public ContainerWidget m_container;

        public ButtonWidget m_reportButton;

        public ButtonWidget m_cancelButton;

        public List<CheckboxWidget> m_reasonWidgetsList = [];

        public ReportCommunityContentDialog(string address, string displayName, string userId) {
            m_address = address;
            m_userId = userId;
            XElement node = ContentManager.Get<XElement>("Dialogs/ReportCommunityContentDialog");
            LoadContents(this, node);
            m_nameLabel = Children.Find<LabelWidget>("ReportCommunityContentDialog.Name");
            m_container = Children.Find<ContainerWidget>("ReportCommunityContentDialog.Container");
            m_reportButton = Children.Find<ButtonWidget>("ReportCommunityContentDialog.Report");
            m_cancelButton = Children.Find<ButtonWidget>("ReportCommunityContentDialog.Cancel");
            m_reasonWidgetsList.Add(new CheckboxWidget { Text = "Cruelty", Tag = "cruelty" });
            m_reasonWidgetsList.Add(new CheckboxWidget { Text = "Dating", Tag = "dating" });
            m_reasonWidgetsList.Add(new CheckboxWidget { Text = "Drugs / Alcohol", Tag = "drugs" });
            m_reasonWidgetsList.Add(new CheckboxWidget { Text = "Hate Speech", Tag = "hate" });
            m_reasonWidgetsList.Add(new CheckboxWidget { Text = "Plagiarism", Tag = "plagiarism" });
            m_reasonWidgetsList.Add(new CheckboxWidget { Text = "Racism", Tag = "racism" });
            m_reasonWidgetsList.Add(new CheckboxWidget { Text = "Sex / Nudity", Tag = "sex" });
            m_reasonWidgetsList.Add(new CheckboxWidget { Text = "Excessive Swearing", Tag = "swearing" });
            Random random = new();
            m_reasonWidgetsList.RandomShuffle(max => random.Int(0, max - 1));
            m_reasonWidgetsList.Add(new CheckboxWidget { Text = "Other", Tag = "other" });
            foreach (CheckboxWidget reasonWidgets in m_reasonWidgetsList) {
                m_container.Children.Add(reasonWidgets);
            }
            m_nameLabel.Text = displayName;
            m_reportButton.IsEnabled = false;
        }

        public override void Update() {
            m_reportButton.IsEnabled = m_reasonWidgetsList.Count(w => w.IsChecked) == 1;
            if (m_reportButton.IsClicked) {
                DialogsManager.HideDialog(this);
                DialogsManager.ShowDialog(
                    ParentWidget,
                    new MessageDialog(
                        "Are you sure?",
                        "Reporting offensive content is a serious matter. Please make sure you checked the right box. Do not report content which is not offensive.",
                        "Proceed",
                        "Cancel",
                        delegate(MessageDialogButton b) {
                            if (b == MessageDialogButton.Button1) {
                                string report = string.Empty;
                                foreach (CheckboxWidget reasonWidgets in m_reasonWidgetsList) {
                                    if (reasonWidgets.IsChecked) {
                                        report = (string)reasonWidgets.Tag;
                                        break;
                                    }
                                }
                                CancellableBusyDialog busyDialog = new("Sending Report", false);
                                DialogsManager.ShowDialog(ParentWidget, busyDialog);
                                CommunityContentManager.Report(
                                    m_address,
                                    m_userId,
                                    report,
                                    busyDialog.Progress,
                                    delegate { DialogsManager.HideDialog(busyDialog); },
                                    delegate { DialogsManager.HideDialog(busyDialog); }
                                );
                            }
                        }
                    )
                );
            }
            if (Input.Cancel
                || m_cancelButton.IsClicked) {
                DialogsManager.HideDialog(this);
            }
        }
    }
}