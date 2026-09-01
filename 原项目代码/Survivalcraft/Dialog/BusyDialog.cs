using System.Xml.Linq;

namespace Game {
    public class BusyDialog : Dialog {
        public LabelWidget m_largeLabelWidget;

        public LabelWidget m_smallLabelWidget;

        public string LargeMessage {
            get => m_largeLabelWidget.Text;
            set {
                m_largeLabelWidget.Text = value ?? string.Empty;
                m_largeLabelWidget.IsVisible = !string.IsNullOrEmpty(value);
            }
        }

        public string SmallMessage {
            get => m_smallLabelWidget.Text;
            set {
                m_smallLabelWidget.Text = value ?? string.Empty;
                m_smallLabelWidget.IsVisible = !string.IsNullOrEmpty(value);
            }
        }

        public BusyDialog(string largeMessage, string smallMessage) {
            XElement node = ContentManager.Get<XElement>("Dialogs/BusyDialog");
            LoadContents(this, node);
            m_largeLabelWidget = Children.Find<LabelWidget>("BusyDialog.LargeLabel");
            m_smallLabelWidget = Children.Find<LabelWidget>("BusyDialog.SmallLabel");
            LargeMessage = largeMessage;
            SmallMessage = smallMessage;
        }

        public override void Update() {
            if (Input.Back) {
                Input.Clear();
            }
        }
    }
}