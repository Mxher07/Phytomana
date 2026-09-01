namespace Game {
    public interface IDragTargetWidget {
        void DragOver(Widget dragWidget, object data);

        void DragDrop(Widget dragWidget, object data);
    }
}