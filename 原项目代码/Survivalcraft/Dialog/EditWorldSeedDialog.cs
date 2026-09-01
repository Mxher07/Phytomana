using System.Xml.Linq;

namespace Game {
    public class EditWorldSeedDialog : Dialog {
        public Action<string, int> m_handler;

        public TextBoxWidget m_worldSeedTextBox;
        public LabelWidget m_worldSeedLabel;
        public TextBoxWidget m_trueWorldSeedTextBox;
        public ButtonWidget m_resetButton;
        public ButtonWidget m_okButton;
        public ButtonWidget m_cancelButton;

        public bool m_trueWorldSeedEdited = false;

        public const string fName = "EditWorldSeedDialog";

        public EditWorldSeedDialog(string seed, int trueSeed, Action<string, int> handler) {
            m_handler = handler;
            XElement node = ContentManager.Get<XElement>("Dialogs/EditWorldSeedDialog");
            LoadContents(this, node);
            m_worldSeedTextBox = Children.Find<TextBoxWidget>("EditWorldSeedDialog.WorldSeedTextBox");
            m_worldSeedLabel = Children.Find<LabelWidget>("EditWorldSeedDialog.WorldSeedLabel");
            m_trueWorldSeedTextBox = Children.Find<TextBoxWidget>("EditWorldSeedDialog.TrueWorldSeedTextBox");
            m_resetButton = Children.Find<ButtonWidget>("EditWorldSeedDialog.ResetButton");
            m_okButton = Children.Find<ButtonWidget>("EditWorldSeedDialog.OkButton");
            m_cancelButton = Children.Find<ButtonWidget>("EditWorldSeedDialog.CancelButton");
            if (seed == null) {
                m_trueWorldSeedEdited = true;
                m_worldSeedTextBox.Text = string.Empty;
                m_worldSeedLabel.Text = LanguageControl.Get(fName, "2");
                m_worldSeedLabel.IsVisible = true;
                m_trueWorldSeedTextBox.Text = trueSeed.ToString();
                m_trueWorldSeedTextBox.HasFocus = true;
            }
            else {
                m_worldSeedTextBox.Text = seed;
                m_worldSeedTextBox.HasFocus = true;
                m_trueWorldSeedTextBox.Text = SeedToTrueSeed(seed).ToString();
            }
            m_worldSeedTextBox.TextChanged += widget => {
                if (m_trueWorldSeedEdited) {
                    return;
                }
                m_trueWorldSeedTextBox.ChangeTextNoEvent(SeedToTrueSeed(widget.Text).ToString());
                if (string.IsNullOrEmpty(widget.Text)) {
                    m_worldSeedLabel.Text = LanguageControl.Get(fName, "1");
                }
            };
            m_trueWorldSeedTextBox.TextChanged += widget => {
                if (m_trueWorldSeedEdited) {
                    return;
                }
                m_trueWorldSeedEdited = true;
                m_worldSeedTextBox.Text = string.Empty;
                m_worldSeedLabel.Text = LanguageControl.Get(fName, "2");
                m_worldSeedLabel.IsVisible = true;
            };
        }

        public override void Update() {
            if (m_resetButton.IsClicked) {
                m_trueWorldSeedEdited = false;
                m_worldSeedTextBox.Text = string.Empty;
                m_worldSeedLabel.Text = LanguageControl.Get(fName, "1");
                m_trueWorldSeedTextBox.ChangeTextNoEvent(string.Empty);
            }
            else if (Input.Cancel
                || m_cancelButton.IsClicked) {
                DialogsManager.HideDialog(this);
            }
            else if (Input.Ok
                || m_okButton.IsClicked) {
                if (m_trueWorldSeedEdited) {
                    if (int.TryParse(m_trueWorldSeedTextBox.Text, out int trueSeed)) {
                        DialogsManager.HideDialog(this);
                        m_handler?.Invoke(null, trueSeed);
                    }
                    else {
                        DialogsManager.ShowDialog(
                            null,
                            new MessageDialog(LanguageControl.Error, LanguageControl.Get(fName, "3"), LanguageControl.Ok, null, null)
                        );
                    }
                }
                else {
                    DialogsManager.HideDialog(this);
                    m_handler?.Invoke(m_worldSeedTextBox.Text, 0);
                }
            }
            m_worldSeedLabel.IsVisible = m_trueWorldSeedEdited || (string.IsNullOrEmpty(m_worldSeedTextBox.Text) && !m_worldSeedTextBox.HasFocus);
        }

        public static int SeedToTrueSeed(string seed) {
            int trueSeed = 0;
            int num = 1;
            foreach (char c in seed) {
                trueSeed += c * num;
                num += 29;
            }
            return trueSeed;
        }
    }
}