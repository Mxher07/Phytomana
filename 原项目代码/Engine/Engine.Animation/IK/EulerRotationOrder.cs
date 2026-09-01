namespace Engine.Animation {
    /// <summary>
    /// 欧拉角旋转顺序
    /// </summary>
    public enum EulerRotationOrder {
        /// <summary>
        /// 先 X 后 Y 再 Z
        /// </summary>
        XYZ,

        /// <summary>
        /// 先 Y 后 X 再 Z（大多数人形骨骼）
        /// </summary>
        YXZ,

        /// <summary>
        /// 先 Z 后 X 再 Y
        /// </summary>
        ZXY,

        /// <summary>
        /// 先 Z 后 Y 再 X（四足动物腿部常用）
        /// </summary>
        ZYX
    }
}