using System.Xml.Linq;
using Engine;

namespace Game {
    public class EditAdjustableDelayGateDialog : Dialog {
        public SliderWidget m_delaySlider;
        public ButtonWidget m_plusButton;
        public ButtonWidget m_minusButton;
        public LabelWidget m_delayLabel;
        public ButtonWidget m_okButton;
        public ButtonWidget m_cancelButton;

        public Action<int> m_handler;
        public int m_delay;

        public EditAdjustableDelayGateDialog(int delay, Action<int> handler) {
            XElement node = ContentManager.Get<XElement>("Dialogs/EditAdjustableDelayGateDialog");
            LoadContents(this, node);
            m_delaySlider = Children.Find<SliderWidget>("EditAdjustableDelayGateDialog.DelaySlider");
            m_plusButton = Children.Find<ButtonWidget>("EditAdjustableDelayGateDialog.PlusButton");
            m_minusButton = Children.Find<ButtonWidget>("EditAdjustableDelayGateDialog.MinusButton");
            m_delayLabel = Children.Find<LabelWidget>("EditAdjustableDelayGateDialog.Label");
            m_okButton = Children.Find<ButtonWidget>("EditAdjustableDelayGateDialog.OK");
            m_cancelButton = Children.Find<ButtonWidget>("EditAdjustableDelayGateDialog.Cancel");
            m_handler = handler;
            m_delay = delay;
            UpdateControls();
        }

        public override void Update() {
            if (m_delaySlider.IsSliding) {
                m_delay = (int)m_delaySlider.Value;
            }
            if (m_minusButton.IsClicked) {
                m_delay = MathUtils.Max(m_delay - 1, (int)m_delaySlider.MinValue);
            }
            if (m_plusButton.IsClicked) {
                m_delay = MathUtils.Min(m_delay + 1, (int)m_delaySlider.MaxValue);
            }
            if (m_okButton.IsClicked) {
                Dismiss(m_delay);
            }
            if (Input.Cancel
                || m_cancelButton.IsClicked) {
                Dismiss(null);
            }
            UpdateControls();
        }

        public virtual void UpdateControls() {
            m_delaySlider.Value = m_delay;
            m_minusButton.IsEnabled = m_delay > m_delaySlider.MinValue;
            m_plusButton.IsEnabled = m_delay < m_delaySlider.MaxValue;
            m_delayLabel.Text = string.Format(LanguageControl.Get("EditAdjustableDelayGateDialog", 1), Math.Round((m_delay + 1) * 0.01f, 2));
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