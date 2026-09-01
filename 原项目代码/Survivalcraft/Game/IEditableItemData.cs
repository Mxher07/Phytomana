namespace Game {
    public interface IEditableItemData {
        IEditableItemData Copy();

        void LoadString(string data);

        string SaveString();
    }
}