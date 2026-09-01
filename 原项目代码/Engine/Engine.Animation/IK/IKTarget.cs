namespace Engine.Animation {
    /// <summary>
    /// IK 目标 - 运行时目标（位置/方向约束）
    /// </summary>
    public class IKTarget {
        /// <summary>
        /// 位置约束（模型空间坐标）
        /// </summary>
        /// <remarks>
        /// 由调用者负责从世界空间转换到模型空间：
        /// <code>
        /// Vector3 modelPos = Vector3.Transform(worldPos, Matrix.Invert(modelMatrix));
        /// </code>
        /// </remarks>
        public Vector3? Position { get; set; }

        /// <summary>
        /// 位置约束权重（0-1）
        /// </summary>
        public float PositionWeight { get; set; } = 1.0f;

        /// <summary>
        /// 方向约束（模型空间方向）
        /// </summary>
        /// <remarks>
        /// 同样需要从世界空间转换到模型空间：
        /// <code>
        /// Vector3 modelDir = Vector3.TransformNormal(worldDir, Matrix.Invert(modelMatrix));
        /// </code>
        /// </remarks>
        public Vector3? AimDirection { get; set; }

        /// <summary>
        /// 方向约束权重（0-1）
        /// </summary>
        public float AimWeight { get; set; } = 1.0f;

        /// <summary>
        /// 弯曲方向提示（如肘部弯曲方向，模型空间）
        /// </summary>
        public Vector3? Hint { get; set; }

        /// <summary>
        /// 位置平滑时间（秒），0 表示禁用平滑
        /// </summary>
        public float PositionSmoothTime { get; set; } = 0.1f;

        /// <summary>
        /// 方向平滑时间（秒），0 表示禁用平滑
        /// </summary>
        public float AimSmoothTime { get; set; } = 0.15f;

        /// <summary>
        /// 是否活动（有任何约束）
        /// </summary>
        public bool IsActive => Position.HasValue || AimDirection.HasValue;

        // 内部平滑状态（由 IKSolver 维护）
        public Vector3 m_smoothedPosition;
        public Vector3 m_positionVelocity;
        public Vector3 m_smoothedAimDirection;
        public Vector3 m_aimVelocity;
        public bool m_smoothInitialized;

        /// <summary>
        /// 创建位置目标
        /// </summary>
        public static IKTarget PositionTarget(Vector3 position, float weight = 1.0f) => new() { Position = position, PositionWeight = weight };

        /// <summary>
        /// 创建方向目标
        /// </summary>
        public static IKTarget AimTarget(Vector3 direction, float weight = 1.0f) => new() { AimDirection = direction, AimWeight = weight };

        /// <summary>
        /// 创建混合目标（位置 + 方向）
        /// </summary>
        public static IKTarget CombinedTarget(Vector3 position, Vector3 direction, float positionWeight = 1.0f, float aimWeight = 1.0f) => new() {
            Position = position, PositionWeight = positionWeight, AimDirection = direction, AimWeight = aimWeight
        };
    }
}