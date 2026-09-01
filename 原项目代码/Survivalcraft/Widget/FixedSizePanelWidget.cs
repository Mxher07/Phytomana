using Engine;

namespace Game {
    public class FixedSizePanelWidget : ContainerWidget {
        public override void MeasureOverride(Vector2 parentAvailableSize) {
            Vector2 zero = Vector2.Zero;
            foreach (Widget child in Children) {
                if (child.IsVisible) {
                    child.Measure(Vector2.Max(parentAvailableSize - child.MarginHorizontalSumAndVerticalSum, Vector2.Zero));
                    if (child.ParentDesiredSize.X != float.PositiveInfinity) {
                        zero.X = MathUtils.Max(zero.X, child.ParentDesiredSize.X + child.MarginHorizontalSum);
                    }
                    if (child.ParentDesiredSize.Y != float.PositiveInfinity) {
                        zero.Y = MathUtils.Max(zero.Y, child.ParentDesiredSize.Y + child.MarginVerticalSum);
                    }
                }
            }
            DesiredSize = zero;
        }

        public override void ArrangeOverride() {
            foreach (Widget child in Children) {
                ArrangeChildWidgetInCell(Vector2.Zero, ActualSize, child);
            }
        }
    }
}