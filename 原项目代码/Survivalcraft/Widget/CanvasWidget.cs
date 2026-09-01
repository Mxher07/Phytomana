using Engine;

namespace Game {
    public class CanvasWidget : ContainerWidget {
        public Dictionary<Widget, Vector2> m_positions = [];

        public Vector2 Size { get; set; } = new(-1f);

        public static void SetPosition(Widget widget, Vector2 position) {
            (widget.ParentWidget as CanvasWidget)?.SetWidgetPosition(widget, position);
        }

        public Vector2? GetWidgetPosition(Widget widget) {
            if (m_positions.TryGetValue(widget, out Vector2 value)) {
                return value;
            }
            return null;
        }

        public void SetWidgetPosition(Widget widget, Vector2? position) {
            if (position.HasValue) {
                m_positions[widget] = position.Value;
            }
            else {
                m_positions.Remove(widget);
            }
        }

        public override void WidgetRemoved(Widget widget) {
            m_positions.Remove(widget);
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            Vector2 desiredSize = Vector2.Zero;
            if (Size.X >= 0f) {
                parentAvailableSize.X = MathUtils.Min(parentAvailableSize.X, Size.X);
            }
            if (Size.Y >= 0f) {
                parentAvailableSize.Y = MathUtils.Min(parentAvailableSize.Y, Size.Y);
            }
            foreach (Widget child in Children) {
                try {
                    if (child.IsVisible) {
                        Vector2? widgetPosition = GetWidgetPosition(child);
                        Vector2 v = widgetPosition ?? Vector2.Zero;
                        child.Measure(Vector2.Max(parentAvailableSize - v - child.MarginHorizontalSumAndVerticalSum, Vector2.Zero));
                        Vector2 vector = default;
                        vector.X = MathUtils.Max(desiredSize.X, v.X + child.ParentDesiredSize.X + child.MarginHorizontalSum);
                        vector.Y = MathUtils.Max(desiredSize.Y, v.Y + child.ParentDesiredSize.Y + child.MarginVerticalSum);
                        desiredSize = vector;
                    }
                }
                catch (Exception e) {
                    throw new Exception($"Exception measuring widget of type {child.GetType().FullName}.", e);
                }
            }
            if (Size.X >= 0f) {
                desiredSize.X = Size.X;
            }
            if (Size.Y >= 0f) {
                desiredSize.Y = Size.Y;
            }
            DesiredSize = desiredSize;
        }

        public override void ArrangeOverride() {
            foreach (Widget child in Children) {
                try {
                    if (child.IsVisible) {
                        Vector2? widgetPosition = GetWidgetPosition(child);
                        if (widgetPosition.HasValue) {
                            Vector2 zero = Vector2.Zero;
                            zero.X = !float.IsPositiveInfinity(child.ParentDesiredSize.X)
                                ? child.ParentDesiredSize.X
                                : MathUtils.Max(ActualSize.X - widgetPosition.Value.X, 0f);
                            zero.Y = !float.IsPositiveInfinity(child.ParentDesiredSize.Y)
                                ? child.ParentDesiredSize.Y
                                : MathUtils.Max(ActualSize.Y - widgetPosition.Value.Y, 0f);
                            child.Arrange(widgetPosition.Value, zero);
                        }
                        else {
                            ArrangeChildWidgetInCell(Vector2.Zero, ActualSize, child);
                        }
                    }
                }
                catch (Exception e) {
                    throw new Exception($"Exception arranging widget of type {child.GetType().FullName}.", e);
                }
            }
        }
    }
}