using System.Globalization;
using System.Xml.Linq;

namespace Game {
    public class LevelFactorDialog : Dialog {
        public LabelWidget m_titleWidget;
        public LabelWidget m_descriptionWidget;
        public LabelWidget m_namesWidget;
        public LabelWidget m_valuesWidget;
        public LabelWidget m_totalNameWidget;
        public LabelWidget m_totalValueWidget;
        public ButtonWidget m_okWidget;
        public const string fName = "LevelFactorDialog";

        public LevelFactorDialog(string title, string description, IEnumerable<ComponentLevel.Factor> factors, float total) {
            XElement node = ContentManager.Get<XElement>("Dialogs/LevelFactorDialog");
            LoadContents(this, node);
            m_titleWidget = Children.Find<LabelWidget>("LevelFactorDialog.Title");
            m_descriptionWidget = Children.Find<LabelWidget>("LevelFactorDialog.Description");
            m_namesWidget = Children.Find<LabelWidget>("LevelFactorDialog.Names");
            m_valuesWidget = Children.Find<LabelWidget>("LevelFactorDialog.Values");
            m_totalNameWidget = Children.Find<LabelWidget>("LevelFactorDialog.TotalName");
            m_totalValueWidget = Children.Find<LabelWidget>("LevelFactorDialog.TotalValue");
            m_okWidget = Children.Find<ButtonWidget>("LevelFactorDialog.OK");
            m_titleWidget.Text = title;
            m_descriptionWidget.Text = description;
            m_namesWidget.Text = string.Empty;
            m_valuesWidget.Text = string.Empty;
            foreach (ComponentLevel.Factor factor in factors) {
                m_namesWidget.Text += $"{factor.Description,24}\n";
                string factorValueString = factor.Value switch {
                    float.NegativeInfinity => LanguageControl.Get(fName, "1"),
                    float.PositiveInfinity => LanguageControl.Get(fName, "2"),
                    _ => string.Format(CultureInfo.InvariantCulture, "{0:0.00}", factor.Value)
                };
                m_valuesWidget.Text += factor.FactorAdditionType switch {
                    FactorAdditionType.Multiply => $"x {factorValueString}\n",
                    _ => $"+ {factorValueString}\n"
                };
            }
            m_namesWidget.Text = m_namesWidget.Text.TrimEnd();
            m_valuesWidget.Text = m_valuesWidget.Text.TrimEnd();
            m_totalNameWidget.Text = $"{LanguageControl.Get(fName, "3"),24}";
            m_totalValueWidget.Text = string.Format(CultureInfo.InvariantCulture, "x {0:0.00}", total);
        }

        public override void Update() {
            if (Input.Cancel
                || m_okWidget.IsClicked) {
                DialogsManager.HideDialog(this);
            }
        }
    }
}