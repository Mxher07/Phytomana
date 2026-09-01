using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace Game {
    public class HelpScreen : Screen {
        public ListPanelWidget m_topicsList;

        public ButtonWidget m_reportButton;

        public ButtonWidget m_recipaediaButton;

        public ButtonWidget m_bestiaryButton;

        [Obsolete]
        public Screen m_previousScreen;

        public Dictionary<string, HelpTopic> m_topics = [];

        /// <summary>
        ///     点击帮助条目时执行
        /// </summary>
        public virtual void OnTopicsListItemClicked(object item) {
            if (item is HelpTopic helpTopic2) {
                ShowTopic(helpTopic2);
            }
        }

        public HelpScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/HelpScreen");
            LoadContents(this, node);
            m_topicsList = Children.Find<ListPanelWidget>("TopicsList");
            m_reportButton = Children.Find<ButtonWidget>("ReportButton");
            m_recipaediaButton = Children.Find<ButtonWidget>("RecipaediaButton");
            m_bestiaryButton = Children.Find<ButtonWidget>("BestiaryButton");
            m_topicsList.ItemWidgetFactory = delegate(object item) {
                HelpTopic helpTopic3 = (HelpTopic)item;
                XElement node2 = ContentManager.Get<XElement>("Widgets/HelpTopicItem");
                ContainerWidget obj = (ContainerWidget)LoadWidget(this, node2, null);
                obj.Children.Find<LabelWidget>("HelpTopicItem.Title").Text = helpTopic3.Title;
                return obj;
            };
            m_topicsList.ItemClicked += OnTopicsListItemClicked;
            JsonObject helpObject = LanguageControl.jsonNode["Help"]?.AsObject();
            if (helpObject != null) {
                foreach (KeyValuePair<string, JsonNode> item in helpObject) {
                    JsonNode item3 = item.Value;
                    JsonNode displa = item3["DisabledPlatforms"];
                    if (displa != null
                        && displa.GetValueKind() == JsonValueKind.String) {
                        if (displa.GetValue<string>()
                                .Split([","], StringSplitOptions.None)
                                .FirstOrDefault(s => s.Trim().Equals(VersionsManager.PlatformString, StringComparison.CurrentCultureIgnoreCase))
                            == null) {
                            continue;
                        }
                    }
                    JsonNode Title = item3["Title"];
                    JsonNode Name1 = item3["Name"];
                    JsonNode value = item3["value"];
                    string attributeValue = Name1 != null && Name1.GetValueKind() == JsonValueKind.String ? Name1.GetValue<string>() : string.Empty;
                    string attributeValue2 = Title != null && Title.GetValueKind() == JsonValueKind.String ? Title.GetValue<string>() : string.Empty;
                    string text = string.Empty;
                    if (value != null) {
                        string[] array = value.GetValue<string>().Split(["\n"], StringSplitOptions.None);
                        foreach (string text2 in array) {
                            text = $"{text}{text2.Trim()} ";
                        }
                        text = text.Replace("\r", "");
                        text = text.Replace("’", "'");
                        text = text.Replace("\\n", "\n");
                    }
                    bool floatParseSucceed = float.TryParse(item.Key, out float index);
                    HelpTopic helpTopic = new() {
                        Index = floatParseSucceed ? index : 0f, Name = attributeValue, Title = attributeValue2, Text = text
                    };
                    if (!string.IsNullOrEmpty(helpTopic.Name)) {
                        if (m_topics.TryAdd(helpTopic.Name, helpTopic)) {
                            m_topicsList.m_items.Add(helpTopic);
                        }
                    }
                }
            }
            m_topicsList.m_items.Sort((x, y) => x is not HelpTopic x_topic || y is not HelpTopic y_topic ? 0 : x_topic.Index.CompareTo(y_topic.Index)
            );
        }

        public override void Enter(object[] parameters) {
            /*if (ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("HelpTopic")
                && ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("Recipaedia")
                && ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("Bestiary")
                && ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("KeyboardMapping")
                && ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("GamepadMapping")) {
                m_previousScreen = ScreensManager.PreviousScreen;
            }*/
        }

        public override void Leave() {
            m_topicsList.SelectedItem = null;
        }

        public override void Update() {
            if (m_recipaediaButton.IsClicked) {
                ScreensManager.SwitchScreen("Recipaedia");
            }
            if (m_bestiaryButton.IsClicked) {
                ScreensManager.SwitchScreen("Bestiary");
            }
            if (m_reportButton.IsClicked) {
                WebBrowserManager.LaunchBrowser(ModsManager.ReportLink);
            }
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.GoBack();
            }
        }

        public HelpTopic GetTopic(string name) => m_topics[name];

        public void ShowTopic(HelpTopic helpTopic) {
            if (helpTopic.Name == "Keyboard") {
                ScreensManager.SwitchScreen("KeyboardMapping");
            }
            else if (helpTopic.Name == "Gamepad") {
                ScreensManager.SwitchScreen("GamepadMapping");
            }
            else {
                ScreensManager.SwitchScreen("HelpTopic", helpTopic);
            }
        }
    }
}