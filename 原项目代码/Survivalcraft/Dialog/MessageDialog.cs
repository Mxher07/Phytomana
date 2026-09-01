using System.Xml.Linq;
using Engine;

namespace Game {
    public class MessageDialog : Dialog {
        public Action<MessageDialogButton> m_handler;

        public LabelWidget m_largeLabelWidget;

        public LabelWidget m_smallLabelWidget;

        public ButtonWidget m_button1Widget;

        public ButtonWidget m_button2Widget;

        public bool AutoHide { get; set; }

        public MessageDialog(string largeMessage,
            string smallMessage,
            string button1Text,
            string button2Text,
            Vector2 size,
            Action<MessageDialogButton> handler) {
            m_handler = handler;
            XElement node = ContentManager.Get<XElement>("Dialogs/MessageDialog");
            LoadContents(this, node);
            Size = new Vector2(size.X >= 0f ? size.X : Size.X, size.Y >= 0f ? size.Y : Size.Y);
            m_largeLabelWidget = Children.Find<LabelWidget>("MessageDialog.LargeLabel");
            m_smallLabelWidget = Children.Find<LabelWidget>("MessageDialog.SmallLabel");
            m_button1Widget = Children.Find<ButtonWidget>("MessageDialog.Button1");
            m_button2Widget = Children.Find<ButtonWidget>("MessageDialog.Button2");
            m_largeLabelWidget.IsVisible = !string.IsNullOrEmpty(largeMessage);
            m_largeLabelWidget.Text = largeMessage ?? string.Empty;
            m_smallLabelWidget.IsVisible = !string.IsNullOrEmpty(smallMessage);
            m_smallLabelWidget.Text = smallMessage ?? string.Empty;
            //if (largeMessage == LanguageControl.Get("ModifyWorldScreen",1))
            //m_smallLabelWidget.Color = Color.Red;
            m_button1Widget.IsVisible = !string.IsNullOrEmpty(button1Text);
            m_button1Widget.Text = button1Text ?? string.Empty;
            m_button2Widget.IsVisible = !string.IsNullOrEmpty(button2Text);
            m_button2Widget.Text = button2Text ?? string.Empty;
            if (!m_button1Widget.IsVisible
                && !m_button2Widget.IsVisible) {
                throw new InvalidOperationException("MessageDialog must have at least one button.");
            }
            AutoHide = true;
        }

        public MessageDialog(string largeMessage, string smallMessage, string button1Text, string button2Text, Action<MessageDialogButton> handler) :
            this(largeMessage, smallMessage, button1Text, button2Text, new Vector2(-1f), handler) { }

        public override void Update() {
            if (Input.Cancel) {
                if (m_button2Widget.IsVisible) {
                    Dismiss(MessageDialogButton.Button2);
                }
                else {
                    Dismiss(MessageDialogButton.Button1);
                }
            }
            else if (Input.Ok
                || m_button1Widget.IsClicked) {
                Dismiss(MessageDialogButton.Button1);
            }
            else if (m_button2Widget.IsClicked) {
                Dismiss(MessageDialogButton.Button2);
            }
        }

        public void Dismiss(MessageDialogButton button) {
            if (AutoHide) {
                DialogsManager.HideDialog(this);
            }
            m_handler?.Invoke(button);
        }
    }
}