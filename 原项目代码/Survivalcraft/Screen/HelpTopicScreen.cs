using System.Xml.Linq;

namespace Game {
    public class HelpTopicScreen : Screen {
        public LabelWidget m_titleLabel;

        public LabelWidget m_textLabel;

        public ScrollPanelWidget m_scrollPanel;

        public HelpTopicScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/HelpTopicScreen");
            LoadContents(this, node);
            m_titleLabel = Children.Find<LabelWidget>("Title");
            m_textLabel = Children.Find<LabelWidget>("Text");
            m_scrollPanel = Children.Find<ScrollPanelWidget>("ScrollPanel");
        }

        public override void Enter(object[] parameters) {
            HelpTopic helpTopic = (HelpTopic)parameters[0];
            m_titleLabel.Text = helpTopic.Title;
            m_textLabel.Text = helpTopic.Text;
            m_scrollPanel.ScrollPosition = 0f;
        }

        public override void Update() {
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
            }
        }
    }
}