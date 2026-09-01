using System.Xml.Linq;

namespace Game {
    public class EditVoltageLevelDialog : Dialog {
        public Action<int> m_handler;

        public ButtonWidget m_okButton;

        public ButtonWidget m_cancelButton;

        public SliderWidget m_voltageSlider;

        public int m_voltageLevel;

        public EditVoltageLevelDialog(int voltageLevel, Action<int> handler) {
            XElement node = ContentManager.Get<XElement>("Dialogs/EditVoltageLevelDialog");
            LoadContents(this, node);
            m_okButton = Children.Find<ButtonWidget>("EditVoltageLevelDialog.OK");
            m_cancelButton = Children.Find<ButtonWidget>("EditVoltageLevelDialog.Cancel");
            m_voltageSlider = Children.Find<SliderWidget>("EditVoltageLevelDialog.VoltageSlider");
            m_handler = handler;
            m_voltageLevel = voltageLevel;
            UpdateControls();
        }

        public override void Update() {
            if (m_voltageSlider.IsSliding) {
                m_voltageLevel = (int)m_voltageSlider.Value;
            }
            if (m_okButton.IsClicked) {
                Dismiss(m_voltageLevel);
            }
            if (Input.Cancel
                || m_cancelButton.IsClicked) {
                Dismiss(null);
            }
            UpdateControls();
        }

        public virtual void UpdateControls() {
            m_voltageSlider.Text =
                $"{1.5f * m_voltageLevel / 15f:0.0}V ({(m_voltageLevel < 8 ? LanguageControl.Get("EditBatteryDialog", 1) : LanguageControl.Get("EditBatteryDialog", 2))})";
            m_voltageSlider.Value = m_voltageLevel;
        }

        public void Dismiss(int? result) {
            DialogsManager.HideDialog(this);
            if (m_handler != null
                && result.HasValue) {
                m_handler(result.Value);
            }
        }
    }
}