using Engine;

namespace Game {
    public class GridPanelWidget : ContainerWidget {
        public class Column {
            public float Position;

            public float ActualWidth;
        }

        public class Row {
            public float Position;

            public float ActualHeight;
        }

        public List<Column> m_columns = [];

        public List<Row> m_rows = [];

        public Dictionary<Widget, Point2> m_cells = [];

        public int ColumnsCount {
            get => m_columns.Count;
            set {
                m_columns = new List<Column>(m_columns.GetRange(0, MathUtils.Min(m_columns.Count, value)));
                while (m_columns.Count < value) {
                    m_columns.Add(new Column());
                }
            }
        }

        public int RowsCount {
            get => m_rows.Count;
            set {
                m_rows = new List<Row>(m_rows.GetRange(0, MathUtils.Min(m_rows.Count, value)));
                while (m_rows.Count < value) {
                    m_rows.Add(new Row());
                }
            }
        }

        public GridPanelWidget() {
            ColumnsCount = 1;
            RowsCount = 1;
        }

        public Point2 GetWidgetCell(Widget widget) {
            m_cells.TryGetValue(widget, out Point2 value);
            return value;
        }

        public void SetWidgetCell(Widget widget, Point2 cell) {
            m_cells[widget] = cell;
        }

        public static void SetCell(Widget widget, Point2 cell) {
            (widget.ParentWidget as GridPanelWidget)?.SetWidgetCell(widget, cell);
        }

        public override void WidgetRemoved(Widget widget) {
            m_cells.Remove(widget);
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            foreach (Column column2 in m_columns) {
                column2.ActualWidth = 0f;
            }
            foreach (Row row2 in m_rows) {
                row2.ActualHeight = 0f;
            }
            foreach (Widget child in Children) {
                child.Measure(Vector2.Max(parentAvailableSize - child.MarginHorizontalSumAndVerticalSum, Vector2.Zero));
                Point2 widgetCell = GetWidgetCell(child);
                if (IsCellValid(widgetCell)) {
                    Column column = m_columns[widgetCell.X];
                    column.ActualWidth = MathUtils.Max(column.ActualWidth, child.ParentDesiredSize.X + child.MarginHorizontalSum);
                    Row row = m_rows[widgetCell.Y];
                    row.ActualHeight = MathUtils.Max(row.ActualHeight, child.ParentDesiredSize.Y + child.MarginVerticalSum);
                }
            }
            Vector2 zero = Vector2.Zero;
            foreach (Column column3 in m_columns) {
                column3.Position = zero.X;
                zero.X += column3.ActualWidth;
            }
            foreach (Row row3 in m_rows) {
                row3.Position = zero.Y;
                zero.Y += row3.ActualHeight;
            }
            DesiredSize = zero;
        }

        public override void ArrangeOverride() {
            foreach (Widget child in Children) {
                Point2 widgetCell = GetWidgetCell(child);
                if (IsCellValid(widgetCell)) {
                    Column column = m_columns[widgetCell.X];
                    Row row = m_rows[widgetCell.Y];
                    ArrangeChildWidgetInCell(
                        new Vector2(column.Position, row.Position),
                        new Vector2(column.Position + column.ActualWidth, row.Position + row.ActualHeight),
                        child
                    );
                }
                else {
                    ArrangeChildWidgetInCell(Vector2.Zero, ActualSize, child);
                }
            }
        }

        public bool IsCellValid(Point2 cell) {
            if (cell.X >= 0
                && cell.X < m_columns.Count
                && cell.Y >= 0) {
                return cell.Y < m_rows.Count;
            }
            return false;
        }
    }
}