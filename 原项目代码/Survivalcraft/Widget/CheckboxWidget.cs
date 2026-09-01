using System.Xml.Linq;
using Engine;
using Engine.Media;

namespace Game {
    public class CheckboxWidget : CanvasWidget {
        public CanvasWidget m_canvasWidget;

        public RectangleWidget m_rectangleWidget;

        public RectangleWidget m_tickWidget;

        public LabelWidget m_labelWidget;

        public ClickableWidget m_clickableWidget;

        public bool IsPressed => m_clickableWidget.IsPressed;

        public bool IsClicked => m_clickableWidget.IsClicked;

        public bool IsTapped => m_clickableWidget.IsTapped;

        public bool IsChecked { get; set; }

        public bool IsAutoCheckingEnabled { get; set; }

        public string Text {
            get => m_labelWidget.Text;
            set => m_labelWidget.Text = value;
        }

        public BitmapFont Font {
            get => m_labelWidget.Font;
            set => m_labelWidget.Font = value;
        }

        public Subtexture TickSubtexture {
            get => m_tickWidget.Subtexture;
            set => m_tickWidget.Subtexture = value;
        }

        public Color Color { get; set; }

        public Vector2 CheckboxSize {
            get => m_canvasWidget.Size;
            set => m_canvasWidget.Size = value;
        }

        public CheckboxWidget() {
            XElement node = ContentManager.Get<XElement>("Widgets/CheckboxContents");
            LoadChildren(this, node);
            m_canvasWidget = Children.Find<CanvasWidget>("Checkbox.Canvas");
            m_rectangleWidget = Children.Find<RectangleWidget>("Checkbox.Rectangle");
            m_tickWidget = Children.Find<RectangleWidget>("Checkbox.Tick");
            m_labelWidget = Children.Find<LabelWidget>("Checkbox.Label");
            m_clickableWidget = Children.Find<ClickableWidget>("Checkbox.Clickable");
            LoadProperties(this, node);
        }

        public override void Update() {
            if (IsClicked && IsAutoCheckingEnabled) {
                IsChecked = !IsChecked;
            }
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            bool isEnabledGlobal = IsEnabledGlobal;
            m_labelWidget.Color = isEnabledGlobal ? Color : new Color(112, 112, 112);
            m_rectangleWidget.FillColor = new Color(0, 0, 0, 128);
            m_rectangleWidget.OutlineColor = isEnabledGlobal ? new Color(128, 128, 128) : new Color(112, 112, 112);
            m_tickWidget.IsVisible = IsChecked;
            m_tickWidget.FillColor = isEnabledGlobal ? Color : new Color(112, 112, 112);
            m_tickWidget.OutlineColor = Color.Transparent;
            m_tickWidget.Subtexture = TickSubtexture;
            base.MeasureOverride(parentAvailableSize);
        }
    }
}