using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// 动画驱动器接口
    /// 用于程序化生成骨骼变换
    /// </summary>
    public interface IAnimationDriver {
        /// <summary>
        /// 驱动器名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 混合模式
        /// </summary>
        public AnimationBlendMode BlendMode { get; }

        /// <summary>
        /// 目标骨骼列表
        /// </summary>
        public string[] TargetBones { get; }

        /// <summary>
        /// 采样驱动器产生的骨骼变换
        /// </summary>
        /// <param name="boneTransforms">骨骼变换数组，驱动器需要填充对应骨骼的变换</param>
        /// <param name="model">模型对象，用于查找骨骼索引</param>
        public void SampleTransforms(Matrix?[] boneTransforms, Model model);

        /// <summary>
        /// 更新驱动器状态
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        /// <param name="parameters">动画参数</param>
        public void Update(float deltaTime, AnimationParameters parameters);
    }
}