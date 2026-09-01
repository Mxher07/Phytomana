using TemplatesDatabase;

namespace Game {
    public struct BlockPlacementData {
        public BlockPlacementData() { }
        public int Value;

        public CellFace CellFace;

        /// <summary>
        ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
        /// </summary>
        public ValuesDictionary ValuesDictionaryForMods = new();
    }
}