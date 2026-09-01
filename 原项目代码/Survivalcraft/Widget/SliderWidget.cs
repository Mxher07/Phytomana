using System.Xml.Linq;
using Engine;
using Engine.Media;

namespace Game {
    public class SliderWidget : CanvasWidget {
        public CanvasWidget m_canvasWidget;

        public CanvasWidget m_labelCanvasWidget;

        public Widget m_tabWidget;

        public LabelWidget m_labelWidget;

        public float m_minValue;

        public float m_maxValue = 1f;

        public float m_granularity = 0.1f;

        public float m_value;

        public Vector2? m_dragStartPoint;

        public bool IsSliding { get; set; }

        public LayoutDirection LayoutDirection { get; set; }

        public float MinValue {
            get => m_minValue;
            set {
                if (value != m_minValue) {
                    m_minValue = value;
                    MaxValue = MathUtils.Max(MinValue, MaxValue);
                    Value = Math.Clamp(Value, MinValue, MaxValue);
                }
            }
        }

        public float MaxValue {
            get => m_maxValue;
            set {
                if (value != m_maxValue) {
                    m_maxValue = value;
                    MinValue = MathUtils.Min(MinValue, MaxValue);
                    Value = Math.Clamp(Value, MinValue, MaxValue);
                }
            }
        }

        public float Value {
            get => m_value;
            set => m_value = m_granularity > 0f
                ? MathF.Round(Math.Clamp(value, MinValue, MaxValue) / m_granularity) * m_granularity
                : Math.Clamp(value, MinValue, MaxValue);
        }

        public float Granularity {
            get => m_granularity;
            set => m_granularity = MathUtils.Max(value, 0f);
        }

        public string Text {
            get => m_labelWidget.Text;
            set => m_labelWidget.Text = value;
        }

        public BitmapFont Font {
            get => m_labelWidget.Font;
            set => m_labelWidget.Font = value;
        }

        public string SoundName { get; set; }

        public bool IsLabelVisible {
            get => m_labelCanvasWidget.IsVisible;
            set => m_labelCanvasWidget.IsVisible = value;
        }

        public float LabelWidth {
            get => m_labelCanvasWidget.Size.X;
            set => m_labelCanvasWidget.Size = new Vector2(value, m_labelCanvasWidget.Size.Y);
        }

        public Color TextColor {
            get => m_labelWidget.Color;
            set => m_labelWidget.Color = value;
        }

        public bool SlidingCompleted { get; private set; }

        public SliderWidget() {
            XElement node = ContentManager.Get<XElement>("Widgets/SliderContents");
            LoadChildren(this, node);
            m_canvasWidget = Children.Find<CanvasWidget>("Slider.Canvas");
            m_labelCanvasWidget = Children.Find<CanvasWidget>("Slider.LabelCanvas");
            m_tabWidget = Children.Find<Widget>("Slider.Tab");
            m_labelWidget = Children.Find<LabelWidget>("Slider.Label");
            LoadProperties(this, node);
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            base.MeasureOverride(parentAvailableSize);
            IsDrawRequired = true;
        }

        public override void ArrangeOverride() {
            base.ArrangeOverride();
            float num = LayoutDirection == LayoutDirection.Horizontal ? m_canvasWidget.ActualSize.X : m_canvasWidget.ActualSize.Y;
            float num2 = LayoutDirection == LayoutDirection.Horizontal ? m_tabWidget.ActualSize.X : m_tabWidget.ActualSize.Y;
            float num3 = MaxValue > MinValue ? (Value - MinValue) / (MaxValue - MinValue) : 0f;
            if (LayoutDirection == LayoutDirection.Horizontal) {
                Vector2 zero = Vector2.Zero;
                zero.X = num3 * (num - num2);
                zero.Y = MathUtils.Max((ActualSize.Y - m_tabWidget.ActualSize.Y) / 2f, 0f);
                m_canvasWidget.SetWidgetPosition(m_tabWidget, zero);
            }
            else {
                Vector2 zero2 = Vector2.Zero;
                zero2.X = MathUtils.Max(ActualSize.X - m_tabWidget.ActualSize.X, 0f) / 2f;
                zero2.Y = num3 * (num - num2);
                m_canvasWidget.SetWidgetPosition(m_tabWidget, zero2);
            }
            base.ArrangeOverride();
        }

        public override void Update() {
            float num = LayoutDirection == LayoutDirection.Horizontal ? m_canvasWidget.ActualSize.X : m_canvasWidget.ActualSize.Y;
            float num2 = LayoutDirection == LayoutDirection.Horizontal ? m_tabWidget.ActualSize.X : m_tabWidget.ActualSize.Y;
            if (Input.Tap.HasValue
                && HitTestGlobal(Input.Tap.Value) == m_tabWidget) {
                m_dragStartPoint = ScreenToWidget(Input.Press.Value);
            }
            if (Input.Press.HasValue) {
                if (m_dragStartPoint.HasValue) {
                    Vector2 vector = ScreenToWidget(Input.Press.Value);
                    float value = Value;
                    if (LayoutDirection == LayoutDirection.Horizontal) {
                        float f = (vector.X - num2 / 2f) / (num - num2);
                        Value = MathUtils.Lerp(MinValue, MaxValue, f);
                    }
                    else {
                        float f2 = (vector.Y - num2 / 2f) / (num - num2);
                        Value = MathUtils.Lerp(MinValue, MaxValue, f2);
                    }
                    if (Value != value
                        && m_granularity > 0f
                        && !string.IsNullOrEmpty(SoundName)) {
                        AudioManager.PlaySound(SoundName, 1f, 0f, 0f);
                    }
                }
            }
            else {
                m_dragStartPoint = null;
            }
            bool flag = m_dragStartPoint.HasValue && IsEnabledGlobal && IsVisibleGlobal;
            SlidingCompleted = IsSliding && !flag;
            IsSliding = flag;
            if (m_dragStartPoint.HasValue) {
                Input.Clear();
            }
        }
    }
}