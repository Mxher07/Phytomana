using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Media;

namespace Game {
    public class LinkWidget : FixedSizePanelWidget {
        public LabelWidget m_labelWidget;

        public ClickableWidget m_clickableWidget;

        public Vector2 Size {
            get => m_labelWidget.Size;
            set => m_labelWidget.Size = value;
        }

        public bool IsClicked => m_clickableWidget.IsClicked;

        public bool IsPressed => m_clickableWidget.IsPressed;

        public string Text {
            get => m_labelWidget.Text;
            set => m_labelWidget.Text = value;
        }

        public float FontScale {
            get => m_labelWidget.FontScale;
            set => m_labelWidget.FontScale = value;
        }

        public TextAnchor TextAnchor {
            get => m_labelWidget.TextAnchor;
            set => m_labelWidget.TextAnchor = value;
        }

        public TextOrientation TextOrientation {
            get => m_labelWidget.TextOrientation;
            set => m_labelWidget.TextOrientation = value;
        }

        public BitmapFont Font {
            get => m_labelWidget.Font;
            set => m_labelWidget.Font = value;
        }

        public Color Color {
            get => m_labelWidget.Color;
            set => m_labelWidget.Color = value;
        }

        public bool DropShadow {
            get => m_labelWidget.DropShadow;
            set => m_labelWidget.DropShadow = value;
        }

        public bool WordWrap {
            get => m_labelWidget.WordWrap;
            set => m_labelWidget.WordWrap = value;
        }

        public bool Ellipsis {
            get => m_labelWidget.Ellipsis;
            set => m_labelWidget.Ellipsis = value;
        }

        public string Url { get; set; }

        public LinkWidget() {
            XElement node = ContentManager.Get<XElement>("Widgets/LinkContents");
            LoadChildren(this, node);
            m_labelWidget = Children.Find<LabelWidget>("Label");
            m_clickableWidget = Children.Find<ClickableWidget>("Clickable");
            LoadProperties(this, node);
        }

        public override void Update() {
            if (!string.IsNullOrEmpty(Url) && IsClicked) {
                WebBrowserManager.LaunchBrowser(Url);
            }
        }
    }
}