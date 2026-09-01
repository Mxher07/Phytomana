using Engine;

namespace Game {
    public class DragHostWidget : ContainerWidget {
        public Widget m_dragWidget;

        public object m_dragData;

        public Action m_dragEndedHandler;

        public Vector2 m_dragPosition;

        public bool IsDragInProgress => m_dragWidget != null;

        public DragHostWidget() => IsHitTestVisible = false;

        public void BeginDrag(Widget dragWidget, object dragData, Action dragEndedHandler) {
            if (m_dragWidget == null) {
                m_dragWidget = dragWidget;
                m_dragData = dragData;
                m_dragEndedHandler = dragEndedHandler;
                Children.Add(m_dragWidget);
                UpdateDragPosition();
            }
        }

        public void EndDrag() {
            if (m_dragWidget != null) {
                Children.Remove(m_dragWidget);
                m_dragWidget = null;
                m_dragData = null;
                if (m_dragEndedHandler != null) {
                    m_dragEndedHandler();
                    m_dragEndedHandler = null;
                }
            }
        }

        public override void Update() {
            if (m_dragWidget != null) {
                UpdateDragPosition();
                IDragTargetWidget dragTargetWidget = HitTestGlobal(m_dragPosition, w => w is IDragTargetWidget) as IDragTargetWidget;
                if (Input.Drag.HasValue) {
                    dragTargetWidget?.DragOver(m_dragWidget, m_dragData);
                }
                else {
                    try {
                        dragTargetWidget?.DragDrop(m_dragWidget, m_dragData);
                    }
                    finally {
                        EndDrag();
                    }
                }
            }
        }

        public override void ArrangeOverride() {
            foreach (Widget child in Children) {
                Vector2 parentDesiredSize = child.ParentDesiredSize;
                parentDesiredSize.X = MathUtils.Min(parentDesiredSize.X, ActualSize.X);
                parentDesiredSize.Y = MathUtils.Min(parentDesiredSize.Y, ActualSize.Y);
                child.Arrange(ScreenToWidget(m_dragPosition) - 0.5f * parentDesiredSize, parentDesiredSize);
            }
        }

        public virtual void UpdateDragPosition() {
            if (Input.Drag.HasValue) {
                m_dragPosition = Input.Drag.Value;
                m_dragPosition.X = Math.Clamp(m_dragPosition.X, GlobalBounds.Min.X, GlobalBounds.Max.X - 1f);
                m_dragPosition.Y = Math.Clamp(m_dragPosition.Y, GlobalBounds.Min.Y, GlobalBounds.Max.Y - 1f);
            }
        }
    }
}