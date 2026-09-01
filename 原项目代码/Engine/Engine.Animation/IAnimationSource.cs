using Engine.Animation.RootMotion;
using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// 动画来源接口 - 关键帧动画和驱动器都实现此接口
    /// </summary>
    public interface IAnimationSource {
        /// <summary>
        /// 来源名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 更新动画状态
        /// </summary>
        public void Update(float deltaTime, AnimationParameters parameters);

        /// <summary>
        /// 采样骨骼变换
        /// </summary>
        public void SampleTransforms(Matrix?[] boneTransforms, Model model);

        /// <summary>
        /// 获取根运动数据（可选实现）
        /// </summary>
        /// <returns>速度、冲量、缩放三元组</returns>
        public (Vector3 velocity, Vector3? impulse, Vector3? scale) GetRootMotionDelta() => (Vector3.Zero, null, null);
    }

    /// <summary>
    /// 动画配置
    /// </summary>
    public class AnimationSourceConfig {
        public string Source { get; set; }

        /// <summary>
        /// 播放速度（静态值或表达式）
        /// </summary>
        public object SpeedValue { get; set; } = 1.0f;

        /// <summary>
        /// 是否循环（静态值或表达式）
        /// </summary>
        public object LoopValue { get; set; } = true;

        /// <summary>
        /// 起始相位（静态值或表达式）
        /// </summary>
        public object StartPhaseValue { get; set; } = 0f;

        /// <summary>
        /// 结束相位（静态值或表达式）
        /// </summary>
        public object EndPhaseValue { get; set; } = 1f;

        /// <summary>
        /// 是否保留上一动画的姿势（用于平滑过渡）
        /// </summary>
        public bool PreservePose { get; set; } = false;

        /// <summary>
        /// 过渡时长（静态值或表达式）
        /// </summary>
        public object BlendDurationValue { get; set; } = 0.3f;

        public bool Mirror { get; set; } = false;
        public Dictionary<string, object> DriverArgs { get; set; }
        public List<AnimationEventConfig> Events { get; set; }

        /// <summary>
        /// 骨骼重映射配置 - 用于交换骨骼变换
        /// 例如 {"HandL": "HandR", "HandR": "HandL"} 会交换左右手的变换
        /// </summary>
        public Dictionary<string, string> BoneRemapping { get; set; }

        /// <summary>
        /// 根运动配置
        /// </summary>
        public RootMotionConfig RootMotion { get; set; }

        // Cached dynamic properties (avoid repeated allocations)
        public DynamicProperty<float> m_cachedSpeedProperty;
        public DynamicProperty<bool> m_cachedLoopProperty;
        public DynamicProperty<float> m_cachedStartPhaseProperty;
        public DynamicProperty<float> m_cachedEndPhaseProperty;
        public DynamicProperty<float> m_cachedBlendDurationProperty;

        /// <summary>
        /// 创建动态属性包装器（缓存实例）
        /// </summary>
        public DynamicProperty<float> GetSpeedProperty() => m_cachedSpeedProperty ??= new DynamicProperty<float>(SpeedValue);

        public DynamicProperty<bool> GetLoopProperty() => m_cachedLoopProperty ??= new DynamicProperty<bool>(LoopValue);
        public DynamicProperty<float> GetStartPhaseProperty() => m_cachedStartPhaseProperty ??= new DynamicProperty<float>(StartPhaseValue);
        public DynamicProperty<float> GetEndPhaseProperty() => m_cachedEndPhaseProperty ??= new DynamicProperty<float>(EndPhaseValue);
        public DynamicProperty<float> GetBlendDurationProperty() => m_cachedBlendDurationProperty ??= new DynamicProperty<float>(BlendDurationValue);
    }

    /// <summary>
    /// 动画事件配置
    /// </summary>
    public class AnimationEventConfig {
        /// <summary>
        /// 事件触发时间（归一化时间 0-1）
        /// </summary>
        public float Time { get; set; }

        /// <summary>
        /// 事件名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 事件参数（可选）
        /// </summary>
        public string Data { get; set; }
    }
}