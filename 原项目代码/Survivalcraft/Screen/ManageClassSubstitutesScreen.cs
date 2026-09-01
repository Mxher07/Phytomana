using System.Xml.Linq;
using Engine;
using Engine.Graphics;

namespace Game {
    public class ManageClassSubstitutesScreen : Screen {
        class SubstitutesGroupWidget : StackPanelWidget {
            public readonly ManageClassSubstitutesScreen m_screen;
            public readonly string m_guid;
            public readonly List<ModsManager.ClassSubstitute> m_substitutes;
            public readonly StackPanelWidget m_substitutesContainer = new() { MarginLeft = 40f, Direction = LayoutDirection.Vertical };

            public SubstitutesGroupWidget(ManageClassSubstitutesScreen screen, List<ModsManager.ClassSubstitute> substitutes, string guid) {
                m_screen = screen;
                m_substitutes = substitutes;
                m_guid = guid;
                Direction = LayoutDirection.Vertical;
                MarginLeft = 10f;
                MarginRight = 10f;
                string description = ModsManager.FindElementByGuid(DatabaseManager.DatabaseNode, guid, out XElement element)
                    ? element.Parent?.Attribute("Description")?.Value ?? LanguageControl.Unknown
                    : LanguageControl.Unknown;
                if (description.StartsWith('[')
                    && description.EndsWith(']')) {
                    string[] array = description.Substring(1, description.Length - 2).Split(':');
                    if (array.Length == 2) {
                        description = LanguageControl.GetDatabase(array[0], array[1]);
                    }
                }
                AddChildren(
                    new CanvasWidget() {
                        Size = new Vector2(float.PositiveInfinity, 40f),
                        Children = {
                            new StackPanelWidget() {
                                VerticalAlignment = WidgetAlignment.Center,
                                Children = {
                                    new LabelWidget { Text = substitutes[0].ClassName, VerticalAlignment = WidgetAlignment.Center },
                                    new LabelWidget { Text = $"({description})", Color = new Color(192, 192, 192), VerticalAlignment = WidgetAlignment.Center, MarginLeft = 10f, Ellipsis = true }
                                }
                            }
                        }
                    }
                );
                if (!ModsManager.SelectedClassSubstitutes.TryGetValue(guid, out ModsManager.ClassSubstitute selected)) {
                    selected = substitutes[0];
                }
                for (int i = 0; i < substitutes.Count; i++) {
                    ModsManager.ClassSubstitute substitute = substitutes[i];
                    m_substitutesContainer.AddChildren(new SubstituteWidget(this, i, substitute, selected == substitute));
                }
                AddChildren(m_substitutesContainer);
            }

            public void SelectSubstitute(int index) {
                m_screen.m_changedGuids.Add(m_guid);
                int count = m_substitutes.Count;
                for (int i = 0; i < count; i++) {
                    if (i == index) {
                        ModsManager.SelectedClassSubstitutes[m_guid] = m_substitutes[i];
                        if (m_substitutesContainer.Children[i] is SubstituteWidget widget) {
                            widget.m_checkbox.IsChecked = true;
                        }
                    }
                    else if (m_substitutesContainer.Children[i] is SubstituteWidget widget) {
                        widget.m_checkbox.IsChecked = false;
                    }
                }
            }
        }

        class SubstituteWidget : StackPanelWidget {
            public SubstitutesGroupWidget m_group;
            public readonly int m_index;

            public CheckboxWidget m_checkbox;

            public SubstituteWidget(SubstitutesGroupWidget group, int index, ModsManager.ClassSubstitute substitute, bool isSelected) {
                m_group = group;
                m_index = index;
                m_checkbox = new CheckboxWidget() { IsAutoCheckingEnabled = false, IsChecked = isSelected };
                Texture2D icon;
                AddChildren(m_checkbox);
                LabelWidget label = new() { MarginLeft = 10f, VerticalAlignment = WidgetAlignment.Center };
                if (substitute.PackageName == "survivalcraft") {
                    icon = ModsManager.SurvivalCraftModEntity.Icon;
                    label.Text = $"{LanguageControl.Get("SelectClassSubstituteDialog", "1")} - {substitute.ClassName}";
                }
                else if (ModsManager.PackageNameToModEntity.TryGetValue(substitute.PackageName, out ModEntity modEntity)) {
                    icon = modEntity.Icon;
                    label.Text = $"{modEntity.modInfo.Name} - {substitute.ClassName}";
                }
                else {
                    icon = null;
                    label.Text = $"{substitute.PackageName} - {substitute.ClassName}";
                }
                if (icon != null) {
                    AddChildren(
                        new RectangleWidget() {
                            Subtexture = icon,
                            Size = new Vector2(48f),
                            VerticalAlignment = WidgetAlignment.Center,
                            FillColor = Color.White,
                            OutlineColor = Color.Transparent,
                            OutlineThickness = 0f
                        }
                    );
                }
                AddChildren(label);
            }

            public override void Update() {
                if (m_checkbox.IsClicked
                    && !m_checkbox.IsChecked) {
                    m_group.SelectSubstitute(m_index);
                    AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
                }
            }
        }

        public HashSet<string> m_changedGuids = [];

        public ButtonWidget m_backButton;

        public ManageClassSubstitutesScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/ManageClassSubstitutesScreen");
            LoadContents(this, node);
            m_backButton = Children.Find<ButtonWidget>("TopBar.Back");
            StackPanelWidget substitutesWidget = Children.Find<StackPanelWidget>("Substitutes");
            foreach ((string guid, List<ModsManager.ClassSubstitute> substitutes) in ModsManager.ClassSubstitutes) {
                substitutesWidget.AddChildren(new SubstitutesGroupWidget(this, substitutes, guid));
            }
        }

        public override void Update() {
            if (Input.Back
                || Input.Cancel
                || m_backButton.IsClicked) {
                if (m_changedGuids.Count > 0) {
                    foreach (string guid in m_changedGuids) {
                        if (ModsManager.SelectedClassSubstitutes.TryGetValue(guid, out ModsManager.ClassSubstitute substitute)
                            && ModsManager.FindElementByGuid(DatabaseManager.DatabaseNode, guid, out XElement element)) {
                            element.SetAttributeValue("Value", substitute.ClassName);
                        }
                    }
                    m_changedGuids.Clear();
                    DatabaseManager.LoadDataBaseFromXml(DatabaseManager.DatabaseNode);
                }
                ScreensManager.GoBack();
            }
        }
    }
}