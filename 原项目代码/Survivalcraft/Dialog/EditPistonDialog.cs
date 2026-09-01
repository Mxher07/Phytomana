using System.Xml.Linq;

namespace Game {
    public class EditPistonDialog : Dialog {
        public LabelWidget m_title;
        public SliderWidget m_slider1;
        public SliderWidget m_slider2;
        public ContainerWidget m_panel2;
        public SliderWidget m_slider3;
        public ButtonWidget m_okButton;
        public ButtonWidget m_cancelButton;
        
        public Action<int> m_handler;
        public int m_data;
        public PistonMode m_mode;
        public int m_maxExtension;
        public int m_pullCount;
        public int m_speed;
        public const string fName = "EditPistonDialog";

        public EditPistonDialog(int data, Action<int> handler) {
            XElement node = ContentManager.Get<XElement>("Dialogs/EditPistonDialog");
            LoadContents(this, node);
            m_title = Children.Find<LabelWidget>("EditPistonDialog.Title");
            m_slider1 = Children.Find<SliderWidget>("EditPistonDialog.Slider1");
            m_panel2 = Children.Find<ContainerWidget>("EditPistonDialog.Panel2");
            m_slider2 = Children.Find<SliderWidget>("EditPistonDialog.Slider2");
            m_slider3 = Children.Find<SliderWidget>("EditPistonDialog.Slider3");
            m_okButton = Children.Find<ButtonWidget>("EditPistonDialog.OK");
            m_cancelButton = Children.Find<ButtonWidget>("EditPistonDialog.Cancel");
            m_handler = handler;
            m_data = data;
            m_mode = PistonBlock.GetMode(data);
            m_maxExtension = PistonBlock.GetMaxExtension(data);
            m_pullCount = PistonBlock.GetPullCount(data);
            m_speed = PistonBlock.GetSpeed(data);
            m_title.Text = BlocksManager.Blocks[237].GetDisplayName(null, Terrain.MakeBlockValue(237, 0, data));
            m_slider1.Granularity = 1f;
            m_slider1.MinValue = 1f;
            m_slider1.MaxValue = 8f;
            m_slider2.Granularity = 1f;
            m_slider2.MinValue = 1f;
            m_slider2.MaxValue = 8f;
            m_slider3.Granularity = 1f;
            m_slider3.MinValue = 0f;
            m_slider3.MaxValue = 3f;
            m_panel2.IsVisible = m_mode != PistonMode.Pushing;
            UpdateControls();
        }

        public override void Update() {
            if (m_slider1.IsSliding) {
                m_maxExtension = (int)m_slider1.Value - 1;
            }
            if (m_slider2.IsSliding) {
                m_pullCount = (int)m_slider2.Value - 1;
            }
            if (m_slider3.IsSliding) {
                m_speed = (int)m_slider3.Value;
            }
            if (m_okButton.IsClicked) {
                int value = PistonBlock.SetMaxExtension(PistonBlock.SetPullCount(PistonBlock.SetSpeed(m_data, m_speed), m_pullCount), m_maxExtension);
                Dismiss(value);
            }
            if (Input.Cancel
                || m_cancelButton.IsClicked) {
                Dismiss(null);
            }
            UpdateControls();
        }

        public virtual void UpdateControls() {
            m_slider1.Value = m_maxExtension + 1;
            m_slider1.Text = string.Format(LanguageControl.Get(fName, 1), m_maxExtension + 1);
            m_slider2.Value = m_pullCount + 1;
            m_slider2.Text = string.Format(LanguageControl.Get(fName, 1), m_pullCount + 1);
            m_slider3.Value = m_speed;
            m_slider3.Text = LanguageControl.Get(fName, "Speed", m_speed.ToString());
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