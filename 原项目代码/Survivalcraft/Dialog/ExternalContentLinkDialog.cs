using System.Xml.Linq;

namespace Game {
    public class ExternalContentLinkDialog : Dialog {
        public TextBoxWidget m_textBoxWidget;

        public ButtonWidget m_okButtonWidget;

        public ExternalContentLinkDialog(string link) {
            ClipboardManager.ClipboardString = link;
            XElement node = ContentManager.Get<XElement>("Dialogs/ExternalContentLinkDialog");
            LoadContents(this, node);
            m_textBoxWidget = Children.Find<TextBoxWidget>("ExternalContentLinkDialog.TextBox");
            m_okButtonWidget = Children.Find<ButtonWidget>("ExternalContentLinkDialog.OkButton");
            m_textBoxWidget.Text = link;
        }

        public override void Update() {
            if (Input.Cancel
                || m_okButtonWidget.IsClicked) {
                DialogsManager.HideDialog(this);
            }
        }
    }
}