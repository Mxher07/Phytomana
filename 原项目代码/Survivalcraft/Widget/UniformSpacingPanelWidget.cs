using Engine;

namespace Game {
    public class UniformSpacingPanelWidget : ContainerWidget {
        public LayoutDirection m_direction;

        public int m_count;

        public LayoutDirection Direction {
            get => m_direction;
            set => m_direction = value;
        }

        public override void ArrangeOverride() {
            Vector2 zero = Vector2.Zero;
            foreach (Widget child in Children) {
                if (child.IsVisible) {
                    if (m_direction == LayoutDirection.Horizontal) {
                        float num = m_count > 0 ? ActualSize.X / m_count : 0f;
                        ArrangeChildWidgetInCell(zero, new Vector2(zero.X + num, zero.Y + ActualSize.Y), child);
                        zero.X += num;
                    }
                    else {
                        float num2 = m_count > 0 ? ActualSize.Y / m_count : 0f;
                        ArrangeChildWidgetInCell(zero, new Vector2(zero.X + ActualSize.X, zero.Y + num2), child);
                        zero.Y += num2;
                    }
                }
            }
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            m_count = 0;
            foreach (Widget child in Children) {
                if (child.IsVisible) {
                    m_count++;
                }
            }
            parentAvailableSize = m_direction != 0
                ? Vector2.Min(parentAvailableSize, new Vector2(parentAvailableSize.X, parentAvailableSize.Y / m_count))
                : Vector2.Min(parentAvailableSize, new Vector2(parentAvailableSize.X / m_count, parentAvailableSize.Y));
            float num = 0f;
            foreach (Widget child2 in Children) {
                if (child2.IsVisible) {
                    child2.Measure(Vector2.Max(parentAvailableSize - child2.MarginHorizontalSumAndVerticalSum, Vector2.Zero));
                    num = m_direction != 0
                        ? MathUtils.Max(num, child2.ParentDesiredSize.X + child2.MarginHorizontalSum)
                        : MathUtils.Max(num, child2.ParentDesiredSize.Y + child2.MarginVerticalSum);
                }
            }
            DesiredSize = m_direction == LayoutDirection.Horizontal
                ? new Vector2(float.PositiveInfinity, num)
                : new Vector2(num, float.PositiveInfinity);
        }
    }
}