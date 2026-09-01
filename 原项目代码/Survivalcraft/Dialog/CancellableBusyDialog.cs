using System.Xml.Linq;
using Engine;

namespace Game {
    public class CancellableBusyDialog : Dialog {
        public LabelWidget m_largeLabelWidget;

        public LabelWidget m_smallLabelWidget;

        public ButtonWidget m_cancelButtonWidget;

        public ButtonWidget m_hideButtonWidget;

        public bool m_autoHideOnCancel;

        public CancellableProgress Progress { get; set; }

        public string LargeMessage {
            get => m_largeLabelWidget.Text;
            set {
                m_largeLabelWidget.Text = value ?? string.Empty;
                m_largeLabelWidget.IsVisible = !string.IsNullOrEmpty(value);
            }
        }

        public string SmallMessage {
            get => m_smallLabelWidget.Text;
            set => m_smallLabelWidget.Text = value ?? string.Empty;
        }

        public bool IsCancelButtonEnabled {
            get => m_cancelButtonWidget.IsEnabled;
            set => m_cancelButtonWidget.IsEnabled = value;
        }

        public bool ShowProgressMessage { get; set; }

        public CancellableBusyDialog(string largeMessage, bool autoHideOnCancel) {
            XElement node = ContentManager.Get<XElement>("Dialogs/CancellableBusyDialog");
            LoadContents(this, node);
            m_largeLabelWidget = Children.Find<LabelWidget>("CancellableBusyDialog.LargeLabel");
            m_smallLabelWidget = Children.Find<LabelWidget>("CancellableBusyDialog.SmallLabel");
            m_cancelButtonWidget = Children.Find<ButtonWidget>("CancellableBusyDialog.CancelButton");
            m_hideButtonWidget = Children.Find<ButtonWidget>("CancellableBusyDialog.HideButton");
            m_hideButtonWidget.IsVisible = false;
            Progress = new CancellableProgress();
            m_autoHideOnCancel = autoHideOnCancel;
            LargeMessage = largeMessage;
            ShowProgressMessage = true;
        }

        public CancellableBusyDialog(string largeMessage, string hideButtonName, bool autoHideOnCancel) {
            XElement node = ContentManager.Get<XElement>("Dialogs/CancellableBusyDialog");
            LoadContents(this, node);
            m_largeLabelWidget = Children.Find<LabelWidget>("CancellableBusyDialog.LargeLabel");
            m_smallLabelWidget = Children.Find<LabelWidget>("CancellableBusyDialog.SmallLabel");
            m_cancelButtonWidget = Children.Find<ButtonWidget>("CancellableBusyDialog.CancelButton");
            m_hideButtonWidget = Children.Find<ButtonWidget>("CancellableBusyDialog.HideButton");
            m_hideButtonWidget.IsVisible = true;
            m_hideButtonWidget.Text = hideButtonName;
            m_cancelButtonWidget.Size = new Vector2(160, 60);
            m_hideButtonWidget.Size = new Vector2(160, 60);
            Progress = new CancellableProgress();
            m_autoHideOnCancel = autoHideOnCancel;
            LargeMessage = largeMessage;
            ShowProgressMessage = true;
        }

        public override void Update() {
            if (ShowProgressMessage) {
                SmallMessage = Progress.Completed > 0f && Progress.Total > 0f ? $"{Progress.Completed / Progress.Total * 100f:0}%" : string.Empty;
            }
            if (m_cancelButtonWidget.IsClicked) {
                Progress.Cancel();
                if (m_autoHideOnCancel) {
                    DialogsManager.HideDialog(this);
                }
            }
            if (m_hideButtonWidget.IsClicked) {
                DialogsManager.HideDialog(this);
            }
            if (Input.Cancel) {
                Input.Clear();
            }
        }
    }
}