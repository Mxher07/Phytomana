using Engine;

namespace Game {
    public abstract class ContainerWidget : Widget {
        public readonly WidgetsList Children;

        public IEnumerable<Widget> AllChildren {
            get {
                foreach (Widget childWidget in Children) {
                    yield return childWidget;
                    if (childWidget is ContainerWidget containerWidget) {
                        foreach (Widget allChild in containerWidget.AllChildren) {
                            yield return allChild;
                        }
                    }
                }
            }
        }

        public ContainerWidget() => Children = new WidgetsList(this);

        public override void UpdateCeases() {
            foreach (Widget child in Children) {
                child.UpdateCeases();
            }
        }

        public void AddChildren(Widget widget) {
            if (Children.IndexOf(widget) < 0) {
                Children.Add(widget);
            }
        }

        public void RemoveChildren(Widget widget) {
            Children.Remove(widget);
        }

        public void ClearChildren() {
            Children.Clear();
        }

        public virtual void WidgetAdded(Widget widget) { }

        public virtual void WidgetRemoved(Widget widget) { }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            foreach (Widget child in Children) {
                try {
                    child.Measure(Vector2.Max(parentAvailableSize - child.MarginHorizontalSumAndVerticalSum, Vector2.Zero));
                }
                catch (Exception e) {
                    throw new Exception($"Exception measuring widget of type {child.GetType().FullName}.", e);
                }
            }
        }

        public override void ArrangeOverride() {
            foreach (Widget child in Children) {
                try {
                    ArrangeChildWidgetInCell(Vector2.Zero, ActualSize, child);
                }
                catch(Exception e) {
                    throw new Exception($"Exception arranging widget of type {child.GetType().FullName}.", e);
                }
            }
        }

        public static void ArrangeChildWidgetInCell(Vector2 c1, Vector2 c2, Widget widget) {
            Vector2 zero = Vector2.Zero;
            Vector2 zero2 = Vector2.Zero;
            Vector2 vector = c2 - c1;
            Vector2 parentDesiredSize = widget.ParentDesiredSize;
            if (float.IsPositiveInfinity(parentDesiredSize.X)
                || parentDesiredSize.X > vector.X - widget.MarginHorizontalSum) {
                parentDesiredSize.X = MathUtils.Max(vector.X - widget.MarginHorizontalSum, 0f);
            }
            if (float.IsPositiveInfinity(parentDesiredSize.Y)
                || parentDesiredSize.Y > vector.Y - widget.MarginVerticalSum) {
                parentDesiredSize.Y = MathUtils.Max(vector.Y - widget.MarginVerticalSum, 0f);
            }
            if (widget.HorizontalAlignment == WidgetAlignment.Near) {
                zero.X = c1.X + widget.MarginLeft;
                zero2.X = parentDesiredSize.X;
            }
            else if (widget.HorizontalAlignment == WidgetAlignment.Center) {
                zero.X = c1.X + (vector.X - parentDesiredSize.X) / 2f;
                zero2.X = parentDesiredSize.X;
            }
            else if (widget.HorizontalAlignment == WidgetAlignment.Far) {
                zero.X = c2.X - parentDesiredSize.X - widget.MarginRight;
                zero2.X = parentDesiredSize.X;
            }
            else if (widget.HorizontalAlignment == WidgetAlignment.Stretch) {
                zero.X = c1.X + widget.MarginLeft;
                zero2.X = MathUtils.Max(vector.X - widget.MarginHorizontalSum, 0f);
            }
            if (widget.VerticalAlignment == WidgetAlignment.Near) {
                zero.Y = c1.Y + widget.MarginTop;
                zero2.Y = parentDesiredSize.Y;
            }
            else if (widget.VerticalAlignment == WidgetAlignment.Center) {
                zero.Y = c1.Y + (vector.Y - parentDesiredSize.Y) / 2f;
                zero2.Y = parentDesiredSize.Y;
            }
            else if (widget.VerticalAlignment == WidgetAlignment.Far) {
                zero.Y = c2.Y - parentDesiredSize.Y - widget.MarginBottom;
                zero2.Y = parentDesiredSize.Y;
            }
            else if (widget.VerticalAlignment == WidgetAlignment.Stretch) {
                zero.Y = c1.Y + widget.MarginTop;
                zero2.Y = MathUtils.Max(vector.Y - widget.MarginVerticalSum, 0f);
            }
            widget.Arrange(zero, zero2);
        }

        public override void Dispose() {
            foreach (Widget child in Children) {
                child.Dispose();
            }
        }
    }
}