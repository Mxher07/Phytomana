using System.Xml.Linq;
using Engine;
using Engine.Input;
using TemplatesDatabase;

namespace Game {
    public class VrControllerMappingScreen : Screen {
        public const string fName = nameof(VrControllerMappingScreen);
        public const string keysSection = "VrControllerMappingScreenKeys";
        public const string actionsSection = "VrControllerMappingScreenActions";

        public ListPanelWidget m_keysList;
        public BevelledButtonWidget m_setKeyButton;
        public BevelledButtonWidget m_disableKeyButton;
        public BevelledButtonWidget m_resetButton;
        public BevelledButtonWidget m_gameHelpButton;
        public bool IsWaitingForInput;
        public Dictionary<string, bool> m_conflicts = [];

        static readonly string[] VrActions = [
            "VrJump", "VrInteract", "VrAim", "VrEditItem",
            "VrToggleMount", "VrToggleCrouch", "VrToggleInventory",
            "VrToggleClothing", "VrHit", "VrDig", "VrDrop",
            "VrScrollLeft", "VrScrollRight", "VrToggleFly", "VrSwitchCameraMode"
        ];

        static readonly string[][] VrCompatibleGroups = [
            ["VrInteract", "VrAim"],
            ["VrHit", "VrDig"]
        ];

        public VrControllerMappingScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/KeyboardMappingScreen");
            LoadContents(this, node);
            m_keysList = Children.Find<ListPanelWidget>("KeysList");
            m_keysList.ItemWidgetFactory = (Func<object, Widget>)Delegate.Combine(m_keysList.ItemWidgetFactory, MappingInfoWidget);
            m_keysList.ScrollPosition = 0f;
            m_keysList.ScrollSpeed = 0f;
            m_keysList.ItemClicked += item => {
                if (item is VrMappingItem mi && !mi.IsHeader) {
                    m_keysList.SelectedItem = m_keysList.SelectedItem == item ? null : item;
                }
            };
            m_setKeyButton = Children.Find<BevelledButtonWidget>("SetKey");
            m_disableKeyButton = Children.Find<BevelledButtonWidget>("DisableKey");
            m_resetButton = Children.Find<BevelledButtonWidget>("Reset");
            m_gameHelpButton = Children.Find<BevelledButtonWidget>("GameHelp");
        }

        public Widget MappingInfoWidget(object item) {
            XElement node = ContentManager.Get<XElement>("Widgets/KeyboardMappingItem");
            node.SetAttributeValue("Name", $"VrMapping_{item}");
            ContainerWidget containerWidget = (ContainerWidget)LoadWidget(this, node, null);
            LabelWidget nameLabel = containerWidget.Children.Find<LabelWidget>("Name");
            LabelWidget actionLabel = containerWidget.Children.Find<LabelWidget>("BoundKey");
            if (item is VrMappingItem mi) {
                if (mi.IsHeader) {
                    nameLabel.Text = TranslateHeader(mi.ActionName);
                    actionLabel.Text = "";
                }
                else {
                    m_widgetsByAction[mi.ActionName] = containerWidget;
                    nameLabel.Text = TranslateAction(mi.ActionName);
                    var (ctrl, btn) = SettingsManager.GetVrMapping(mi.ActionName);
                    if (btn == VrControllerButton.Null) {
                        actionLabel.Text = "";
                    }
                    else {
                        string ctrlName = ctrl == VrController.Left
                            ? LanguageControl.Get(keysSection, "LeftController")
                            : LanguageControl.Get(keysSection, "RightController");
                        actionLabel.Text = $"{ctrlName} · {TranslateButton(btn, ctrl)}";
                    }
                    actionLabel.Color = m_conflicts.TryGetValue(mi.ActionName, out bool conflicted) && conflicted
                        ? Color.Red
                        : Color.White;
                }
            }
            return containerWidget;
        }

        public override void Enter(object[] parameters) {
            m_gameHelpButton.IsVisible = ScreensManager.PreviousScreen is GameScreen;
            m_keysList.ClearItems();
            PopulateList();
            RefreshConflicts();
        }

        public override void Update() {
            string selectedAction = GetSelectedAction();
            m_setKeyButton.IsEnabled = selectedAction != null;
            m_disableKeyButton.IsEnabled = selectedAction != null;

            if (Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
                return;
            }
            // Update conflict display for visible items
            foreach (object item in m_keysList.Items) {
                if (item is VrMappingItem mi && !mi.IsHeader) {
                    if (m_widgetsByAction.TryGetValue(mi.ActionName, out ContainerWidget widget)) {
                        LabelWidget actionLabel = widget.Children.Find<LabelWidget>("BoundKey");
                        var (ctrl, btn) = SettingsManager.GetVrMapping(mi.ActionName);
                        if (btn == VrControllerButton.Null) {
                            actionLabel.Text = "";
                        }
                        else {
                            string ctrlName = ctrl == VrController.Left
                                ? LanguageControl.Get(keysSection, "LeftController")
                                : LanguageControl.Get(keysSection, "RightController");
                            actionLabel.Text = $"{ctrlName} · {TranslateButton(btn, ctrl)}";
                        }
                        actionLabel.Color = m_conflicts.TryGetValue(mi.ActionName, out bool conflicted) && conflicted
                            ? Color.Red
                            : Color.White;
                    }
                }
            }
            if (m_disableKeyButton.IsClicked) {
                if (selectedAction != null) {
                    SettingsManager.SetVrMapping(selectedAction, VrController.Left, VrControllerButton.Null);
                    RefreshConflicts();
                }
                IsWaitingForInput = false;
            }
            if (m_resetButton.IsClicked) {
                MessageDialog dialog = new(
                    LanguageControl.Get("ContentWidgets", "KeyboardMappingScreen", "ResetTitle"),
                    LanguageControl.Get("ContentWidgets", "KeyboardMappingScreen", "ResetText"),
                    LanguageControl.Yes,
                    LanguageControl.No,
                    delegate(MessageDialogButton button) {
                        if (button == MessageDialogButton.Button1) {
                            ResetAll();
                        }
                    }
                );
                DialogsManager.ShowDialog(null, dialog);
                IsWaitingForInput = false;
            }
            if (IsWaitingForInput) {
                m_setKeyButton.IsChecked = true;
                // Cancel on Menu button press
                if (VrManager.IsButtonDownOnce(VrController.Left, VrControllerButton.Menu)
                    || VrManager.IsButtonDownOnce(VrController.Right, VrControllerButton.Menu)) {
                    IsWaitingForInput = false;
                    return;
                }
                // Poll all buttons on both controllers
                foreach (VrControllerButton button in System.Enum.GetValues<VrControllerButton>()) {
                    if (button == VrControllerButton.Null || button == VrControllerButton.Menu) continue;
                    // Check left controller
                    if (VrManager.IsButtonDownOnce(VrController.Left, button)) {
                        SettingsManager.SetVrMapping(selectedAction, VrController.Left, button);
                        IsWaitingForInput = false;
                        RefreshConflicts();
                        return;
                    }
                    // Check right controller
                    if (VrManager.IsButtonDownOnce(VrController.Right, button)) {
                        SettingsManager.SetVrMapping(selectedAction, VrController.Right, button);
                        IsWaitingForInput = false;
                        RefreshConflicts();
                        return;
                    }
                }
            }
            else {
                m_setKeyButton.IsChecked = false;
            }
            if (m_setKeyButton.IsClicked && selectedAction != null) {
                IsWaitingForInput = true;
            }
            if (m_gameHelpButton.IsClicked) {
                ScreensManager.SwitchScreen("Help");
            }
            if (!IsWaitingForInput && (Input.Back || Input.Cancel)) {
                ScreensManager.GoBack();
            }
        }

        void PopulateList() {
            m_widgetsByAction.Clear();
            foreach (string action in VrActions) {
                m_keysList.AddItem(new VrMappingItem(action, false));
            }
        }

        public void ResetAll() {
            SettingsManager.ResetVrMappingDefaults();
            RefreshConflicts();
        }

        void RefreshConflicts() {
            m_conflicts.Clear();
            var bindingToActions = new Dictionary<(VrController, VrControllerButton), List<string>>();
            foreach (string action in VrActions) {
                var (ctrl, btn) = SettingsManager.GetVrMapping(action);
                if (btn == VrControllerButton.Null) continue;
                var key = (ctrl, btn);
                if (!bindingToActions.TryGetValue(key, out var list)) {
                    list = [];
                    bindingToActions[key] = list;
                }
                list.Add(action);
            }
            foreach (var kvp in bindingToActions) {
                if (kvp.Value.Count > 1 && !IsVrCompatibleGroup(kvp.Value)) {
                    foreach (string action in kvp.Value) {
                        m_conflicts[action] = true;
                    }
                }
            }
        }

        static bool IsVrCompatibleGroup(List<string> actions) {
            foreach (string[] group in VrCompatibleGroups) {
                if (actions.Count == group.Length) {
                    bool match = true;
                    foreach (string a in group) {
                        if (!actions.Contains(a)) { match = false; break; }
                    }
                    if (match) return true;
                }
            }
            return false;
        }

        string GetSelectedAction() {
            if (m_keysList.SelectedItem is VrMappingItem mi && !mi.IsHeader) return mi.ActionName;
            return null;
        }

        // Translation helpers
        static string Tk(string key) => LanguageControl.Get(keysSection, key);
        static string TranslateHeader(string key) => Tk(key);
        static string TranslateAction(string name) {
            string key = name.StartsWith("Vr") ? name[2..] : name;
            string s = LanguageControl.Get(out bool found, "KeyboardMappingScreen", key);
            return found ? s : LanguageControl.Get(actionsSection, name);
        }
        static string TranslateButton(VrControllerButton btn, VrController controller) {
            if (btn == VrControllerButton.Primary)
                return controller == VrController.Left ? Tk("X") : Tk("A");
            if (btn == VrControllerButton.Secondary)
                return controller == VrController.Left ? Tk("Y") : Tk("B");
            return Tk(btn.ToString());
        }

        readonly Dictionary<string, ContainerWidget> m_widgetsByAction = [];

        public class VrMappingItem {
            public string ActionName;
            public bool IsHeader;
            public VrMappingItem(string actionName, bool isHeader) {
                ActionName = actionName;
                IsHeader = isHeader;
            }
            public override string ToString() => ActionName;
        }
    }
}
