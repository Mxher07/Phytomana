using System.Xml.Linq;
using Engine;

namespace Game {
    public class BulletinDialog : Dialog {
        public LabelWidget m_titleLabel;

        public LabelWidget m_contentLabel;

        public LabelWidget m_timeLabel;

        public LabelWidget m_buttonLabel;

        public ButtonWidget m_okButton;

        public ButtonWidget m_editButton;

        public ButtonWidget m_updateButton;

        public ScrollPanelWidget m_scrollPanel;

        public float m_areaLength = 0;

        public Action Action;

        public Action<LabelWidget, LabelWidget> Action2;

        public Action<LabelWidget, LabelWidget> Action3;

        public BulletinDialog(string title,
            string content,
            string time,
            Action action,
            Action<LabelWidget, LabelWidget> action2,
            Action<LabelWidget, LabelWidget> action3) {
            XElement node = ContentManager.Get<XElement>("Dialogs/BulletinDialog");
            LoadContents(this, node);
            m_okButton = Children.Find<ButtonWidget>("OkButton");
            m_editButton = Children.Find<ButtonWidget>("EditButton");
            m_updateButton = Children.Find<ButtonWidget>("UpdateButton");
            m_titleLabel = Children.Find<LabelWidget>("Title");
            m_contentLabel = Children.Find<LabelWidget>("Content");
            m_timeLabel = Children.Find<LabelWidget>("Time");
            m_buttonLabel = Children.Find<LabelWidget>("ButtonLabel");
            m_scrollPanel = Children.Find<ScrollPanelWidget>("ScrollPanel");
            m_buttonLabel.Text = LanguageControl.Ok;
            m_okButton.IsVisible = false;
            m_titleLabel.Text = title;
            m_contentLabel.Text = content;
            m_timeLabel.Text = time;
            Action = action;
            Action2 = action2;
            Action3 = action3;
            m_editButton.IsVisible = false;
            m_updateButton.IsVisible = false;
        }

        public override void Update() {
            float length = MathUtils.Max(m_scrollPanel.m_scrollAreaLength - m_scrollPanel.ActualSize.Y, 0f);
            if (m_scrollPanel.ScrollPosition >= length * 0.8f
                && m_scrollPanel.m_scrollAreaLength != 0) {
                m_okButton.IsVisible = true;
            }
            if (m_okButton.IsClicked) {
                Action?.Invoke();
                DialogsManager.HideDialog(this);
            }
            if (m_editButton.IsClicked) {
                Action2?.Invoke(m_titleLabel, m_contentLabel);
            }
            if (m_updateButton.IsClicked) {
                Action3?.Invoke(m_titleLabel, m_contentLabel);
                DialogsManager.HideDialog(this);
            }
        }
    }
}