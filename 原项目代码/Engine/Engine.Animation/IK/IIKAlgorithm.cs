using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// IK 算法接口
    /// </summary>
    public interface IIKAlgorithm {
        /// <summary>
        /// 算法名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 是否支持方向约束（瞄准）
        /// </summary>
        bool SupportsAim { get; }

        /// <summary>
        /// 求解 IK 链
        /// </summary>
        /// <param name="chain">IK 链定义</param>
        /// <param name="target">运行时目标</param>
        /// <param name="boneTransforms">骨骼变换数组（输出）</param>
        /// <param name="worldPositions">骨骼世界位置数组</param>
        /// <param name="model">模型对象</param>
        /// <param name="config">算法配置参数</param>
        public void Solve(IKChain chain,
            IKTarget target,
            Matrix?[] boneTransforms,
            Vector3[] worldPositions,
            Model model,
            IKAlgorithmConfig config = null);
    }
}