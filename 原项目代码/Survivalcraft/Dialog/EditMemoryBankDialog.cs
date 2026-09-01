using System.Xml.Linq;

namespace Game {
    public class EditMemoryBankDialog : Dialog {
        public Action m_handler;

        public Widget m_linearPanel;

        public Widget m_gridPanel;

        public ButtonWidget m_okButton;

        public ButtonWidget m_cancelButton;

        public ButtonWidget m_switchViewButton;

        public TextBoxWidget[] m_lineTextBoxes = new TextBoxWidget[16];

        public TextBoxWidget m_linearTextBox;

        public MemoryBankData m_memoryBankData;

        public MemoryBankData m_tmpMemoryBankData;

        public bool m_ignoreTextChanges;

        public EditMemoryBankDialog(MemoryBankData memoryBankData, Action handler) {
            XElement node = ContentManager.Get<XElement>("Dialogs/EditMemoryBankDialog");
            LoadContents(this, node);
            m_linearPanel = Children.Find<Widget>("EditMemoryBankDialog.LinearPanel");
            m_gridPanel = Children.Find<Widget>("EditMemoryBankDialog.GridPanel");
            m_okButton = Children.Find<ButtonWidget>("EditMemoryBankDialog.OK");
            m_cancelButton = Children.Find<ButtonWidget>("EditMemoryBankDialog.Cancel");
            m_switchViewButton = Children.Find<ButtonWidget>("EditMemoryBankDialog.SwitchViewButton");
            m_linearTextBox = Children.Find<TextBoxWidget>("EditMemoryBankDialog.LinearText");
            for (int i = 0; i < 16; i++) {
                m_lineTextBoxes[i] = Children.Find<TextBoxWidget>($"EditMemoryBankDialog.Line{i}");
            }
            m_handler = handler;
            m_memoryBankData = memoryBankData;
            m_tmpMemoryBankData = (MemoryBankData)m_memoryBankData.Copy();
            m_linearPanel.IsVisible = false;
            for (int j = 0; j < 16; j++) {
                m_lineTextBoxes[j].TextChanged += TextBox_TextChanged;
            }
            m_linearTextBox.TextChanged += TextBox_TextChanged;
        }

        public void TextBox_TextChanged(TextBoxWidget textBox) {
            if (m_ignoreTextChanges) {
                return;
            }
            if (textBox == m_linearTextBox) {
                m_tmpMemoryBankData = new MemoryBankData();
                m_tmpMemoryBankData.LoadString(m_linearTextBox.Text);
                return;
            }
            string text = string.Empty;
            for (int i = 0; i < 16; i++) {
                text += m_lineTextBoxes[i].Text;
            }
            m_tmpMemoryBankData = new MemoryBankData();
            m_tmpMemoryBankData.LoadString(text);
        }

        public override void Update() {
            m_ignoreTextChanges = true;
            try {
                string text = m_tmpMemoryBankData.SaveString(false);
                if (text.Length < 256) {
                    text += new string('0', 256 - text.Length);
                }
                for (int i = 0; i < 16; i++) {
                    m_lineTextBoxes[i].Text = text.Substring(i * 16, 16);
                }
                m_linearTextBox.Text = m_tmpMemoryBankData.SaveString(false);
            }
            finally {
                m_ignoreTextChanges = false;
            }
            if (m_linearPanel.IsVisible) {
                m_switchViewButton.Text = "Grid";
                if (m_switchViewButton.IsClicked) {
                    m_linearPanel.IsVisible = false;
                    m_gridPanel.IsVisible = true;
                }
            }
            else {
                m_switchViewButton.Text = "Linear";
                if (m_switchViewButton.IsClicked) {
                    m_linearPanel.IsVisible = true;
                    m_gridPanel.IsVisible = false;
                }
            }
            if (m_okButton.IsClicked) {
                m_memoryBankData.Data = m_tmpMemoryBankData.Data;
                Dismiss(true);
            }
            if (Input.Cancel
                || m_cancelButton.IsClicked) {
                Dismiss(false);
            }
        }

        public void Dismiss(bool result) {
            DialogsManager.HideDialog(this);
            if (m_handler != null && result) {
                m_handler();
            }
        }
    }
}