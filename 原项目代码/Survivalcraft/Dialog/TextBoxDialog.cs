using System.Xml.Linq;

namespace Game {
    public class TextBoxDialog : Dialog {
        public Action<string> m_handler;

        public Action<TextBoxWidget> m_handler2;

        public LabelWidget m_titleWidget;

        public TextBoxWidget m_textBoxWidget;

        public ButtonWidget m_okButtonWidget;

        public ButtonWidget m_cancelButtonWidget;

        public bool AutoHide { get; set; }

        public TextBoxDialog(string title, string text, int maximumLength, Action<string> handler) {
            m_handler = handler;
            XElement node = ContentManager.Get<XElement>("Dialogs/TextBoxDialog");
            LoadContents(this, node);
            m_titleWidget = Children.Find<LabelWidget>("TextBoxDialog.Title");
            m_textBoxWidget = Children.Find<TextBoxWidget>("TextBoxDialog.TextBox");
            m_okButtonWidget = Children.Find<ButtonWidget>("TextBoxDialog.OkButton");
            m_cancelButtonWidget = Children.Find<ButtonWidget>("TextBoxDialog.CancelButton");
            m_titleWidget.IsVisible = !string.IsNullOrEmpty(title);
            m_titleWidget.Text = title ?? string.Empty;
            m_textBoxWidget.MaximumLength = maximumLength;
            m_textBoxWidget.Text = text ?? string.Empty;
            m_textBoxWidget.HasFocus = true;
            m_textBoxWidget.Enter += delegate { Dismiss(m_textBoxWidget.Text); };
            AutoHide = true;
        }

        public TextBoxDialog(string title, string text, int maximumLength, Action<string> handler, Action<TextBoxWidget> handler2) {
            m_handler = handler;
            m_handler2 = handler2;
            XElement node = ContentManager.Get<XElement>("Dialogs/TextBoxDialog");
            LoadContents(this, node);
            m_titleWidget = Children.Find<LabelWidget>("TextBoxDialog.Title");
            m_textBoxWidget = Children.Find<TextBoxWidget>("TextBoxDialog.TextBox");
            m_okButtonWidget = Children.Find<ButtonWidget>("TextBoxDialog.OkButton");
            m_cancelButtonWidget = Children.Find<ButtonWidget>("TextBoxDialog.CancelButton");
            m_titleWidget.IsVisible = !string.IsNullOrEmpty(title);
            m_titleWidget.Text = title ?? string.Empty;
            m_textBoxWidget.MaximumLength = maximumLength;
            m_textBoxWidget.Text = text ?? string.Empty;
            m_textBoxWidget.HasFocus = true;
            m_textBoxWidget.Enter += delegate { Dismiss(m_textBoxWidget.Text); };
            if (m_handler2 != null) {
                m_textBoxWidget.TextChanged += delegate(TextBoxWidget textBox) { m_handler2.Invoke(textBox); };
            }
            AutoHide = true;
        }

        public override void Update() {
            if (Input.Cancel) {
                Dismiss(null);
            }
            else if (Input.Ok) {
                Dismiss(m_textBoxWidget.Text);
            }
            else if (m_okButtonWidget.IsClicked) {
                Dismiss(m_textBoxWidget.Text);
            }
            else if (m_cancelButtonWidget.IsClicked) {
                Dismiss(null);
            }
        }

        public void Dismiss(string result) {
            if (AutoHide) {
                DialogsManager.HideDialog(this);
            }
            m_handler?.Invoke(result);
        }
    }
}