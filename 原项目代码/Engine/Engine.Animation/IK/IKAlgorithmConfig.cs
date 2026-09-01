namespace Engine.Animation {
    /// <summary>
    /// IK 算法配置参数
    /// </summary>
    public class IKAlgorithmConfig {
        /// <summary>
        /// CCD/FABRIK 最大迭代次数
        /// </summary>
        public int MaxIterations { get; set; } = 10;

        /// <summary>
        /// 收敛容差（世界单位）
        /// </summary>
        public float Tolerance { get; set; } = 0.001f;

        /// <summary>
        /// TwoBoneIK: 是否保持原始旋转（用于末端骨骼）
        /// </summary>
        public bool MaintainOriginalRotation { get; set; } = false;
    }
}