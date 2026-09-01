namespace Engine.Animation {
    /// <summary>
    /// 动画层定义
    /// </summary>
    public class LayerDefinition {
        public int Index { get; }
        public AnimationBlendMode BlendMode { get; }
        public string[] BoneMask { get; }
        public string[] BoneMaskExclude { get; }
        public float Weight { get; }

        public LayerDefinition(int index, AnimationBlendMode blendMode,
            string[] boneMask = null, string[] boneMaskExclude = null, float weight = 1f) {
            Index = index;
            BlendMode = blendMode;
            BoneMask = boneMask;
            BoneMaskExclude = boneMaskExclude;
            Weight = weight;
        }
    }
}
