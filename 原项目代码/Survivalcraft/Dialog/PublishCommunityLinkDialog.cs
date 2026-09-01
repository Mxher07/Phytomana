using System.Xml.Linq;
using Engine;

namespace Game {
    public class PublishCommunityLinkDialog : Dialog {
        public TextBoxWidget m_linkTextBoxWidget;

        public TextBoxWidget m_nameTextBoxWidget;

        public RectangleWidget m_typeIconWidget;

        public LabelWidget m_typeLabelWidget;

        public ButtonWidget m_changeTypeButtonWidget;

        public ButtonWidget m_publishButtonWidget;

        public ButtonWidget m_cancelButtonWidget;

        public string m_user;

        public ExternalContentType m_type = ExternalContentType.BlocksTexture;

        public PublishCommunityLinkDialog(string user, string address, string name) {
            XElement node = ContentManager.Get<XElement>("Dialogs/PublishCommunityLinkDialog");
            LoadContents(this, node);
            m_linkTextBoxWidget = Children.Find<TextBoxWidget>("PublishCommunityLinkDialog.Link");
            m_nameTextBoxWidget = Children.Find<TextBoxWidget>("PublishCommunityLinkDialog.Name");
            m_typeIconWidget = Children.Find<RectangleWidget>("PublishCommunityLinkDialog.TypeIcon");
            m_typeLabelWidget = Children.Find<LabelWidget>("PublishCommunityLinkDialog.Type");
            m_changeTypeButtonWidget = Children.Find<ButtonWidget>("PublishCommunityLinkDialog.ChangeType");
            m_publishButtonWidget = Children.Find<ButtonWidget>("PublishCommunityLinkDialog.Publish");
            m_cancelButtonWidget = Children.Find<ButtonWidget>("PublishCommunityLinkDialog.Cancel");
            m_linkTextBoxWidget.TextChanged += delegate {
                m_nameTextBoxWidget.Text = Storage.GetFileNameWithoutExtension(GetFilenameFromLink(m_linkTextBoxWidget.Text));
            };
            if (!string.IsNullOrEmpty(address)) {
                m_linkTextBoxWidget.Text = address;
            }
            if (!string.IsNullOrEmpty(name)) {
                m_nameTextBoxWidget.Text = name;
            }
            m_user = user;
        }

        public override void Update() {
            string text = m_linkTextBoxWidget.Text.Trim();
            string text2 = m_nameTextBoxWidget.Text.Trim();
            m_typeLabelWidget.Text = ExternalContentManager.GetEntryTypeDescription(m_type);
            m_typeIconWidget.Subtexture = ExternalContentManager.GetEntryTypeIcon(m_type);
            m_publishButtonWidget.IsEnabled = text.Length > 0 && text2.Length > 0;
            if (m_changeTypeButtonWidget.IsClicked) {
                DialogsManager.ShowDialog(
                    ParentWidget,
                    new SelectExternalContentTypeDialog("Select Content Type", delegate(ExternalContentType item) { m_type = item; })
                );
            }
            else if (Input.Cancel
                || m_cancelButtonWidget.IsClicked) {
                DialogsManager.HideDialog(this);
            }
            else if (m_publishButtonWidget.IsClicked) {
                CancellableBusyDialog busyDialog = new("Publishing", false);
                DialogsManager.ShowDialog(ParentWidget, busyDialog);
                CommunityContentManager.Publish(
                    text,
                    text2,
                    m_type,
                    m_user,
                    busyDialog.Progress,
                    delegate {
                        DialogsManager.HideDialog(busyDialog);
                        DialogsManager.ShowDialog(
                            ParentWidget,
                            new MessageDialog(
                                "Link Published Successfully",
                                "It should start appearing in the listings after it is moderated. Please keep the file accessible through this link, so that other community members can download it.",
                                "OK",
                                null,
                                delegate { DialogsManager.HideDialog(this); }
                            )
                        );
                    },
                    delegate(Exception error) {
                        DialogsManager.HideDialog(busyDialog);
                        DialogsManager.ShowDialog(ParentWidget, new MessageDialog("Error", error.Message, "OK", null, null));
                    }
                );
            }
        }

        public static string GetFilenameFromLink(string address) {
            try {
                string text = address;
                int num = text.IndexOf('&');
                if (num > 0) {
                    text = text.Remove(num);
                }
                int num2 = text.IndexOf('?');
                if (num2 > 0) {
                    text = text.Remove(num2);
                }
                text = Uri.UnescapeDataString(text);
                return Storage.GetFileName(text);
            }
            catch (Exception) {
                return string.Empty;
            }
        }
    }
}