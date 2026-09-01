using Engine;
using Engine.Graphics;

namespace Game {
    public class ClickTextWidget : CanvasWidget {
        public LabelWidget labelWidget;
        public Action click;
        public RectangleWidget rectangleWidget;
        public Color pressColor = Color.Red;
        public Color BorderColor = Color.Transparent;

        // ReSharper disable UnusedParameter.Local
        public ClickTextWidget(Vector2 vector2, string text, Action click, bool box = false)
            // ReSharper restore UnusedParameter.Local
        {
            Size = vector2;
            HorizontalAlignment = WidgetAlignment.Center;
            VerticalAlignment = WidgetAlignment.Center;
            labelWidget = new LabelWidget {
                Text = text, FontScale = 0.8f, HorizontalAlignment = WidgetAlignment.Center, VerticalAlignment = WidgetAlignment.Center
            };
            Children.Add(labelWidget);
            IsDrawEnabled = true;
            IsDrawRequired = true;
            IsUpdateEnabled = true;
            this.click = click;
        }

        public override void Draw(DrawContext dc) {
            Matrix m = GlobalTransform;
            Vector2 v = Vector2.Zero;
            Vector2 v2 = new(ActualSize.X, 0f);
            Vector2 v3 = ActualSize;
            Vector2 v4 = new(0f, ActualSize.Y);
            Vector2.Transform(ref v, ref m, out Vector2 result);
            Vector2.Transform(ref v2, ref m, out Vector2 result2);
            Vector2.Transform(ref v3, ref m, out Vector2 result3);
            Vector2.Transform(ref v4, ref m, out Vector2 result4);
            FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(1, DepthStencilState.DepthWrite);
            Vector2 vector = Vector2.Normalize(GlobalTransform.Right.XY);
            Vector2 v5 = -Vector2.Normalize(GlobalTransform.Up.XY);
            for (int i = 0; i < 1; i++) {
                flatBatch2D.QueueLine(result, result2, 1f, BorderColor);
                flatBatch2D.QueueLine(result2, result3, 1f, BorderColor);
                flatBatch2D.QueueLine(result3, result4, 1f, BorderColor);
                flatBatch2D.QueueLine(result4, result, 1f, BorderColor);
                result += vector - v5;
                result2 += -vector - v5;
                result3 += -vector + v5;
                result4 += vector + v5;
            }
        }

        public override void Update() {
            if (Input.Click.HasValue
                && HitTest(Input.Click.Value.Start)
                && HitTest(Input.Click.Value.End)) {
                click?.Invoke();
            }
        }
    }
}