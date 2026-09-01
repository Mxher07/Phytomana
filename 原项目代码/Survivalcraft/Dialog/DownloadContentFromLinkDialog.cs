using System.Xml.Linq;
using Engine;

namespace Game {
    public class DownloadContentFromLinkDialog : Dialog {
        public TextBoxWidget m_linkTextBoxWidget;

        public TextBoxWidget m_nameTextBoxWidget;

        public RectangleWidget m_typeIconWidget;

        public LabelWidget m_typeLabelWidget;

        public ButtonWidget m_changeTypeButtonWidget;

        public ButtonWidget m_downloadButtonWidget;

        public ButtonWidget m_cancelButtonWidget;

        public bool m_updateContentName;

        public bool m_updateContentType;

        public ExternalContentType m_type;

        public DownloadContentFromLinkDialog() {
            XElement node = ContentManager.Get<XElement>("Dialogs/DownloadContentFromLinkDialog");
            LoadContents(this, node);
            m_linkTextBoxWidget = Children.Find<TextBoxWidget>("DownloadContentFromLinkDialog.Link");
            m_nameTextBoxWidget = Children.Find<TextBoxWidget>("DownloadContentFromLinkDialog.Name");
            m_typeIconWidget = Children.Find<RectangleWidget>("DownloadContentFromLinkDialog.TypeIcon");
            m_typeLabelWidget = Children.Find<LabelWidget>("DownloadContentFromLinkDialog.Type");
            m_changeTypeButtonWidget = Children.Find<ButtonWidget>("DownloadContentFromLinkDialog.ChangeType");
            m_downloadButtonWidget = Children.Find<ButtonWidget>("DownloadContentFromLinkDialog.Download");
            m_cancelButtonWidget = Children.Find<ButtonWidget>("DownloadContentFromLinkDialog.Cancel");
            m_linkTextBoxWidget.TextChanged += delegate {
                m_updateContentName = true;
                m_updateContentType = true;
            };
        }

        public override void Update() {
            string text = m_linkTextBoxWidget.Text.Trim();
            string name = m_nameTextBoxWidget.Text.Trim();
            m_typeLabelWidget.Text = ExternalContentManager.GetEntryTypeDescription(m_type);
            m_typeIconWidget.Subtexture = ExternalContentManager.GetEntryTypeIcon(m_type);
            if (ExternalContentManager.DoesEntryTypeRequireName(m_type)) {
                m_nameTextBoxWidget.IsEnabled = true;
                m_downloadButtonWidget.IsEnabled = text.Length > 0 && name.Length > 0 && m_type != ExternalContentType.Unknown;
                if (m_updateContentName) {
                    m_nameTextBoxWidget.Text = GetNameFromLink(m_linkTextBoxWidget.Text);
                    m_updateContentName = false;
                }
            }
            else {
                m_nameTextBoxWidget.IsEnabled = false;
                m_nameTextBoxWidget.Text = string.Empty;
                m_downloadButtonWidget.IsEnabled = text.Length > 0 && m_type != ExternalContentType.Unknown;
            }
            if (m_updateContentType) {
                m_type = GetTypeFromLink(m_linkTextBoxWidget.Text);
                m_updateContentType = false;
            }
            if (m_changeTypeButtonWidget.IsClicked) {
                DialogsManager.ShowDialog(
                    ParentWidget,
                    new SelectExternalContentTypeDialog(
                        "Select Content Type",
                        delegate(ExternalContentType item) {
                            m_type = item;
                            m_updateContentName = true;
                        }
                    )
                );
            }
            else if (Input.Cancel
                || m_cancelButtonWidget.IsClicked) {
                DialogsManager.HideDialog(this);
            }
            else if (m_downloadButtonWidget.IsClicked) {
                CancellableBusyDialog busyDialog = new("Downloading", false);
                DialogsManager.ShowDialog(ParentWidget, busyDialog);
                WebManager.Get(
                    text,
                    null,
                    null,
                    busyDialog.Progress,
                    delegate(byte[] data) {
                        ExternalContentManager.ImportExternalContent(
                            new MemoryStream(data),
                            m_type,
                            name,
                            delegate {
                                DialogsManager.HideDialog(busyDialog);
                                DialogsManager.HideDialog(this);
                            },
                            delegate(Exception error) {
                                DialogsManager.HideDialog(busyDialog);
                                DialogsManager.ShowDialog(ParentWidget, new MessageDialog("Error", error.Message, "OK", null, null));
                            }
                        );
                    },
                    delegate(Exception error) {
                        DialogsManager.HideDialog(busyDialog);
                        DialogsManager.ShowDialog(ParentWidget, new MessageDialog("Error", error.Message, "OK", null, null));
                    }
                );
            }
        }

        public static string UnclutterLink(string address) {
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
                return Uri.UnescapeDataString(text);
            }
            catch (Exception) {
                return string.Empty;
            }
        }

        public static string GetNameFromLink(string address) {
            try {
                return Storage.GetFileNameWithoutExtension(UnclutterLink(address));
            }
            catch (Exception) {
                return string.Empty;
            }
        }

        public static ExternalContentType GetTypeFromLink(string address) {
            try {
                return ExternalContentManager.ExtensionToType(Storage.GetExtension(UnclutterLink(address)));
            }
            catch (Exception) {
                return ExternalContentType.Unknown;
            }
        }
    }
}