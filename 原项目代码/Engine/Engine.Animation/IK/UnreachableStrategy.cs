namespace Engine.Animation {
    /// <summary>
    /// 目标不可达时的回退策略
    /// </summary>
    public enum UnreachableStrategy {
        /// <summary>
        /// 向目标方向伸展到最大长度（默认）
        /// </summary>
        ExtendTowardTarget,

        /// <summary>
        /// 保持当前姿势不变
        /// </summary>
        KeepCurrentPose,

        /// <summary>
        /// 使用上一次有效结果
        /// </summary>
        UseLastValidResult
    }
}