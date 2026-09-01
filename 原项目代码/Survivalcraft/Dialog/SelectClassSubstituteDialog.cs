using System.Xml.Linq;

namespace Game {
    public class SelectClassSubstituteDialog : Dialog {
        public string m_guid;
        public XElement m_element;
        public Action m_handler;

        public ListPanelWidget m_listPanel;

        public const string fName = "SelectClassSubstituteDialog";

        public SelectClassSubstituteDialog(string guid, XElement element, Action handler) {
            m_guid = guid;
            m_element = element;
            m_handler = handler;
            LoadContents(this, ContentManager.Get<XElement>("Dialogs/SelectClassSubstituteDialog"));
            m_listPanel = Children.Find<ListPanelWidget>("SelectClassSubstituteDialog.ListPanel");
            m_listPanel.ItemWidgetFactory = o => {
                if (o is not ModsManager.ClassSubstitute substitute) {
                    return null;
                }
                if (substitute.PackageName == "survivalcraft") {
                    return new LabelWidget() {
                        Text = $"{LanguageControl.Get(fName, "1")} - {substitute.ClassName}", VerticalAlignment = WidgetAlignment.Center
                    };
                }
                if (ModsManager.PackageNameToModEntity.TryGetValue(substitute.PackageName, out ModEntity modEntity)) {
                    return new ScrollPanelWidget() {
                        Direction = LayoutDirection.Horizontal,
                        Children = {
                            new LabelWidget() {
                                Text = $"{modEntity.modInfo.Name} - {substitute.ClassName}", VerticalAlignment = WidgetAlignment.Center
                            }
                        }
                    };
                }
                return null;
            };
            m_listPanel.ItemClicked = o => {
                if (o is not ModsManager.ClassSubstitute substitute) {
                    return;
                }
                ModsManager.SelectedClassSubstitutes[m_guid] = substitute;
                m_element.SetAttributeValue("Value", substitute.ClassName);
                DialogsManager.HideDialog(this);
                m_handler?.Invoke();
            };
            if (ModsManager.ClassSubstitutes.TryGetValue(guid, out List<ModsManager.ClassSubstitute> substitutes)
                && substitutes.Count > 0) {
                string description = element.Parent?.Attribute("Description")?.Value ?? LanguageControl.Unknown;
                if (description.StartsWith('[')
                    && description.EndsWith(']')) {
                    string[] array = description.Substring(1, description.Length - 2).Split(':');
                    if (array.Length == 2) {
                        description = LanguageControl.GetDatabase(array[0], array[1]);
                    }
                }
                Children.Find<LabelWidget>("SelectClassSubstituteDialog.Description")?.Text = string.Format(
                    LanguageControl.Get(fName, "2"),
                    substitutes.Count - 1,
                    substitutes[0].ClassName,
                    description
                );
                m_listPanel.AddItems(substitutes);
                if (ModsManager.SelectedClassSubstitutes.TryGetValue(guid, out ModsManager.ClassSubstitute selected)) {
                    m_listPanel.SelectedItem = selected;
                }
            }
        }

        public override void Update() {
            if (Input.Back
                || Input.Cancel
                || (Input.Tap.HasValue && !HitTest(Input.Tap.Value))) {
                // 直接关闭将保持原版
                DialogsManager.HideDialog(this);
                m_handler?.Invoke();
            }
        }
    }
}