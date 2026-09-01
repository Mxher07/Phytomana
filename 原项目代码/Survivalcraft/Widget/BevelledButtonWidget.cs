using System.Xml.Linq;
using Engine;
using Engine.Media;

namespace Game {
    public class BevelledButtonWidget : ButtonWidget {
        public BevelledRectangleWidget m_rectangleWidget;

        public RectangleWidget m_imageWidget;

        public LabelWidget m_labelWidget;

        public ClickableWidget m_clickableWidget;

        public float FontScale {
            get => m_labelWidget.FontScale;
            set => m_labelWidget.FontScale = value;
        }

        public override bool IsClicked => m_clickableWidget.IsClicked;

        public override bool IsChecked {
            get => m_clickableWidget.IsChecked;
            set => m_clickableWidget.IsChecked = value;
        }

        public override bool IsAutoCheckingEnabled {
            get => m_clickableWidget.IsAutoCheckingEnabled;
            set => m_clickableWidget.IsAutoCheckingEnabled = value;
        }

        public override string Text {
            get => m_labelWidget.Text;
            set => m_labelWidget.Text = value;
        }

        public override BitmapFont Font {
            get => m_labelWidget.Font;
            set => m_labelWidget.Font = value;
        }

        public Subtexture Subtexture {
            get => m_imageWidget.Subtexture;
            set => m_imageWidget.Subtexture = value;
        }

        public override Color Color { get; set; }

        public Color BevelColor {
            get => m_rectangleWidget.BevelColor;
            set => m_rectangleWidget.BevelColor = value;
        }

        public Color CenterColor {
            get => m_rectangleWidget.CenterColor;
            set => m_rectangleWidget.CenterColor = value;
        }

        public float AmbientLight {
            get => m_rectangleWidget.AmbientLight;
            set => m_rectangleWidget.AmbientLight = value;
        }

        public float DirectionalLight {
            get => m_rectangleWidget.DirectionalLight;
            set => m_rectangleWidget.DirectionalLight = value;
        }

        public float BevelSize { get; set; }

        public BevelledButtonWidget() {
            Color = Color.White;
            BevelSize = 2f;
            XElement node = ContentManager.Get<XElement>("Widgets/BevelledButtonContents");
            LoadChildren(this, node);
            m_rectangleWidget = Children.Find<BevelledRectangleWidget>("BevelledButton.Rectangle");
            m_imageWidget = Children.Find<RectangleWidget>("BevelledButton.Image");
            m_labelWidget = Children.Find<LabelWidget>("BevelledButton.Label");
            m_clickableWidget = Children.Find<ClickableWidget>("BevelledButton.Clickable");
            m_labelWidget.VerticalAlignment = WidgetAlignment.Center;
            LoadProperties(this, node);
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            bool isEnabledGlobal = IsEnabledGlobal;
            m_labelWidget.Color = isEnabledGlobal ? Color : new Color(112, 112, 112);
            m_imageWidget.FillColor = isEnabledGlobal ? Color : new Color(112, 112, 112);
            m_rectangleWidget.BevelSize = m_clickableWidget.IsPressed || IsChecked ? -0.5f * BevelSize : BevelSize;
            base.MeasureOverride(parentAvailableSize);
        }
    }
}