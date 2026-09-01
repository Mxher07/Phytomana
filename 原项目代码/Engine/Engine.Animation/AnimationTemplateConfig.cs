namespace Engine.Animation {
    /// <summary>
    /// 动画模板配置，用于 JSON 反序列化
    /// </summary>
    public class AnimationTemplateConfig {
        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 动画层配置字典（key 为层名）
        /// </summary>
        public Dictionary<string, TemplateLayerConfig> Layers { get; set; }

        /// <summary>
        /// 必需骨骼名称列表
        /// </summary>
        public List<string> RequiredBones { get; set; }
    }

    /// <summary>
    /// 模板动画层配置
    /// </summary>
    public class TemplateLayerConfig {
        /// <summary>
        /// 层索引（决定优先级）
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 混合模式：Override 或 Additive
        /// </summary>
        public string BlendMode { get; set; } = "Override";

        /// <summary>
        /// 骨骼遮罩（按子树展开：含该骨 + 全部后代）
        /// </summary>
        public List<string> BoneMask { get; set; }

        /// <summary>
        /// 骨骼遮罩排除（同子树语义，从结果集中扣除）
        /// </summary>
        public List<string> BoneMaskExclude { get; set; }

        /// <summary>
        /// 层权重（0-1）。null 或 ≤0 表示未设，回退默认 1。
        /// Override 层控制对下层的覆盖强度；Additive 层控制叠加强度。
        /// </summary>
        public float? Weight { get; set; }
    }

}