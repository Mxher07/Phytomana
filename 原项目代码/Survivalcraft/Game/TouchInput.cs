using Engine;
using TemplatesDatabase;

namespace Game {
    public struct TouchInput {
        public TouchInput() { }
        public TouchInputType InputType;

        public Vector2 Position;

        public Vector2 Move;

        public Vector2 TotalMove;

        public Vector2 TotalMoveLimited;

        public float Duration;

        public int DurationFrames;

        /// <summary>
        ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
        /// </summary>
        public ValuesDictionary ValuesDictionaryForMods = new();
    }
}
