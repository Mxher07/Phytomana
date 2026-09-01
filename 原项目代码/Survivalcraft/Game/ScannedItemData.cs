using TemplatesDatabase;

namespace Game {
    public struct ScannedItemData {
        public ScannedItemData() { }
        public object Container;

        public int IndexInContainer;

        public int Value;

        public int Count;

        /// <summary>
        ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
        /// </summary>
        public ValuesDictionary ValuesDictionaryForMods = new();
    }
}