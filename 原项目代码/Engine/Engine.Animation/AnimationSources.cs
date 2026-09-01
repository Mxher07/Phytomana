using Engine.Animation.RootMotion;
using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// 关键帧动画来源 - 包装 AnimationPlayer
    /// </summary>
    public class ClipAnimationSource : IAnimationSource {
        public readonly AnimationPlayer m_player;
        public readonly Model m_model;
        public readonly ModelAnimation m_animation;
        public readonly AnimationSourceConfig m_config;
        public List<AnimationEventConfig> m_events;
        public readonly Dictionary<string, string> m_boneRemapping;

        // Dynamic properties
        public readonly DynamicProperty<float> m_speedProperty;
        public readonly DynamicProperty<bool> m_loopProperty;
        public readonly DynamicProperty<float> m_startPhaseProperty;
        public readonly DynamicProperty<float> m_endPhaseProperty;

        // Cached evaluator reference
        public ExpressionEvaluator m_evaluator;

        // Cached last loop state to avoid unnecessary updates
        public bool m_lastLoopState = true;

        // Flag to track if phase range has been applied (for delayed expression evaluation)
        public bool m_phaseRangeApplied;

        public string Name { get; }

        public AnimationPlayer Player => m_player;
        public bool IsPlaying => m_player?.IsPlaying ?? false;
        public bool IsComplete => !m_lastLoopState && m_player != null && m_player.NormalizedTime >= 1.0f;

        /// <summary>
        /// 动画事件
        /// </summary>
        public event Action<string, string> OnAnimationEvent;

        /// <summary>
        /// 根运动相关
        /// </summary>
        public bool ExtractRootMotion { get; set; }

        public string RootBoneName { get; set; } = "Root";
        public Vector3 RootMotionDelta { get; set; }
        public Vector3 _lastRootPosition;
        public bool _rootMotionInitialized;
        public Matrix?[] _rootMotionTransforms;

        /// <summary>
        /// 根运动配置
        /// </summary>
        public RootMotionConfig RootMotionConfig => m_config?.RootMotion;

        /// <summary>
        /// 创建关键帧动画来源
        /// </summary>
        /// <param name="model">模型</param>
        /// <param name="animation">动画</param>
        /// <param name="config">动画配置</param>
        /// <param name="evaluator">表达式求值器（可选，用于动态属性）</param>
        public ClipAnimationSource(Model model, ModelAnimation animation, AnimationSourceConfig config = null, ExpressionEvaluator evaluator = null) {
            m_model = model;
            m_animation = animation;
            m_config = config ?? new AnimationSourceConfig();
            m_evaluator = evaluator;
            Name = animation?.Name ?? "Unknown";

            // Create dynamic properties from config
            m_speedProperty = m_config.GetSpeedProperty();
            m_loopProperty = m_config.GetLoopProperty();
            m_startPhaseProperty = m_config.GetStartPhaseProperty();
            m_endPhaseProperty = m_config.GetEndPhaseProperty();
            m_boneRemapping = m_config.BoneRemapping;
            m_player = new AnimationPlayer();
            m_player.SetAnimation(model, animation);

            // Get initial static values
            float speed = m_speedProperty.IsExpression ? 1.0f : m_speedProperty.StaticValue;
            bool loop = m_loopProperty.IsExpression ? true : m_loopProperty.StaticValue;
            m_player.Speed = speed;
            m_player.Play(loop);
            m_lastLoopState = loop;

            // Apply phase range: static values immediately, expressions will be evaluated in Update()
            bool startPhaseIsStatic = !m_startPhaseProperty.IsExpression;
            bool endPhaseIsStatic = !m_endPhaseProperty.IsExpression;
            if (startPhaseIsStatic && endPhaseIsStatic) {
                // Both are static - apply immediately
                float startPhase = m_startPhaseProperty.StaticValue;
                float endPhase = m_endPhaseProperty.StaticValue;
                m_player.SetPhaseRange(startPhase, endPhase);

                // Initialize time to start position
                if (m_animation != null
                    && m_animation.Duration > 0) {
                    m_player.Time = startPhase * m_animation.Duration;
                }
                m_phaseRangeApplied = true;
            }
            else {
                // At least one is an expression - delay evaluation until Update()
                // Apply default phase range for now
                m_player.SetPhaseRange(0f, 1f);
                m_phaseRangeApplied = false;
            }
            m_events = m_config.Events;

            // 预分配根运动变换数组，避免每帧分配
            if (model?.Bones != null) {
                _rootMotionTransforms = new Matrix?[model.Bones.Count];
            }

            // 初始化根运动缓存
            InitializeRootMotionCache();
        }

        /// <summary>
        /// 初始化根运动设置（根骨骼检测和标志设置）
        /// 注意：缓存由 AnimationController 统一管理，避免重复存储
        /// </summary>
        public void InitializeRootMotionCache() {
            RootMotionConfig rootMotionConfig = m_config?.RootMotion;
            if (rootMotionConfig == null
                || m_animation == null
                || m_model == null) {
                return;
            }

            if (m_model.RootBone != null) {
                RootBoneName = m_model.RootBone.Name;
            }

            // 启用根运动提取
            // 注意：缓存由 AnimationController 维护，此处仅设置标志
            ExtractRootMotion = true;
        }

        /// <summary>
        /// 设置表达式求值器（用于动态属性）
        /// </summary>
        /// <param name="evaluator">表达式求值器</param>
        public void SetEvaluator(ExpressionEvaluator evaluator) {
            m_evaluator = evaluator;
        }

        public void Update(float deltaTime, AnimationParameters parameters) {
            if (m_player == null) {
                return;
            }

            // Apply phase range expressions once at playback start
            if (!m_phaseRangeApplied
                && m_evaluator != null
                && parameters != null) {
                float startPhase = m_startPhaseProperty.IsExpression
                    ? m_startPhaseProperty.GetValue(parameters, m_evaluator)
                    : m_startPhaseProperty.StaticValue;
                float endPhase = m_endPhaseProperty.IsExpression
                    ? m_endPhaseProperty.GetValue(parameters, m_evaluator)
                    : m_endPhaseProperty.StaticValue;

                // Clamp to valid range
                startPhase = Math.Clamp(startPhase, 0f, 1f);
                endPhase = Math.Clamp(endPhase, 0f, 1f);
                m_player.SetPhaseRange(startPhase, endPhase);

                // Initialize time to start position
                if (m_animation != null
                    && m_animation.Duration > 0) {
                    m_player.Time = startPhase * m_animation.Duration;
                }
                m_phaseRangeApplied = true;
            }

            // Update dynamic properties
            UpdateDynamicProperties(parameters);
            float prevTime = m_player.NormalizedTime;
            m_player.Update(deltaTime);

            // 检查事件触发
            if (m_events != null
                && m_player.IsPlaying) {
                foreach (AnimationEventConfig evt in m_events) {
                    bool crossed = CrossedEventPoint(prevTime, m_player.NormalizedTime, evt.Time);
                    if (crossed) {
                        OnAnimationEvent?.Invoke(evt.Name, evt.Data);
                    }
                }
            }

            // 根运动提取
            if (ExtractRootMotion) {
                ExtractRootMotionDelta();
            }
        }

        /// <summary>
        /// 更新动态属性（速度、循环状态等）
        /// </summary>
        public void UpdateDynamicProperties(AnimationParameters parameters) {
            // 如果没有求值器或参数，使用静态值
            if (m_evaluator == null
                || parameters == null) {
                return;
            }

            // 动态速度
            if (m_speedProperty.IsExpression) {
                float speed = m_speedProperty.GetValue(parameters, m_evaluator);
                m_player.Speed = speed;
            }

            // 动态循环状态
            if (m_loopProperty.IsExpression) {
                bool loop = m_loopProperty.GetValue(parameters, m_evaluator);
                if (m_lastLoopState != loop) {
                    m_player.Loop = loop;
                    m_lastLoopState = loop;
                }
            }
        }

        public bool CrossedEventPoint(float prev, float current, float eventTime) {
            bool isLooping = m_lastLoopState;
            if (!isLooping) {
                return prev < eventTime && current >= eventTime;
            }
            if (current >= prev) {
                return prev < eventTime && current >= eventTime;
            }
            return prev < eventTime || current >= eventTime;
        }

        public void ExtractRootMotionDelta() {
            ModelBone rootBone = m_model.FindBone(RootBoneName);
            if (rootBone == null
                || _rootMotionTransforms == null) {
                return;
            }
            m_player.SampleBoneTransforms(_rootMotionTransforms);
            if (_rootMotionTransforms[rootBone.Index].HasValue) {
                Vector3 currentPos = _rootMotionTransforms[rootBone.Index].Value.Translation;
                if (!_rootMotionInitialized) {
                    _lastRootPosition = currentPos;
                    _rootMotionInitialized = true;
                    RootMotionDelta = Vector3.Zero;
                }
                else {
                    RootMotionDelta = currentPos - _lastRootPosition;
                    _lastRootPosition = currentPos;
                }
            }
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            if (m_player == null) {
                return;
            }
            m_player.SampleBoneTransforms(boneTransforms);

            // 镜像处理
            if (m_config.Mirror) {
                ApplyMirror(boneTransforms, model);
            }

            // 骨骼重映射处理
            if (m_boneRemapping != null
                && m_boneRemapping.Count > 0) {
                ApplyBoneRemapping(boneTransforms, model);
            }

            // 根运动模式下，根骨骼位移已被提取
            if (ExtractRootMotion) {
                ModelBone rootBone = model.FindBone(RootBoneName);
                if (rootBone != null
                    && boneTransforms[rootBone.Index].HasValue) {
                    Matrix transform = boneTransforms[rootBone.Index].Value;
                    // 保留旋转，清除位移
                    transform.Decompose(out _, out Quaternion rotation, out _);
                    boneTransforms[rootBone.Index] = Matrix.CreateFromQuaternion(rotation);
                }
            }
        }

        public void ApplyMirror(Matrix?[] boneTransforms, Model model) {
            for (int i = 0; i < boneTransforms.Length; i++) {
                if (!boneTransforms[i].HasValue) {
                    continue;
                }
                boneTransforms[i].Value.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation);

                // 翻转 X 轴
                translation.X = -translation.X;
                rotation.Y = -rotation.Y;
                rotation.Z = -rotation.Z;
                boneTransforms[i] = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translation);
            }
        }

        /// <summary>
        /// 应用骨骼重映射 - 交换骨骼变换
        /// </summary>
        public void ApplyBoneRemapping(Matrix?[] boneTransforms, Model model) {
            // 收集需要交换的骨骼变换
            Dictionary<int, Matrix?> swapped = new();
            foreach ((string boneA, string boneB) in m_boneRemapping) {
                ModelBone boneAInfo = model.FindBone(boneA);
                ModelBone boneBInfo = model.FindBone(boneB);
                if (boneAInfo != null
                    && boneBInfo != null) {
                    // 交换两个骨骼的变换
                    swapped[boneAInfo.Index] = boneTransforms[boneBInfo.Index];
                    swapped[boneBInfo.Index] = boneTransforms[boneAInfo.Index];
                }
            }

            // 应用交换后的变换
            foreach ((int index, Matrix? transform) in swapped) {
                boneTransforms[index] = transform;
            }
        }

        public void Reset() {
            m_player?.Stop();
            _rootMotionInitialized = false;
            RootMotionDelta = Vector3.Zero;
            m_phaseRangeApplied = false;

            // Reset time to start position if we have static phase values
            if (m_startPhaseProperty != null
                && !m_startPhaseProperty.IsExpression
                && m_animation != null
                && m_animation.Duration > 0) {
                m_player.Time = m_startPhaseProperty.StaticValue * m_animation.Duration;
            }
        }
    }

    /// <summary>
    /// 驱动器动画来源 - 包装 IAnimationDriver
    /// </summary>
    public class DriverAnimationSource : IAnimationSource {
        public readonly IAnimationDriver _driver;

        public string Name => _driver?.Name ?? "Driver";
        public IAnimationDriver Driver => _driver;

        public DriverAnimationSource(IAnimationDriver driver) => _driver = driver;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _driver?.Update(deltaTime, parameters);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            _driver?.SampleTransforms(boneTransforms, model);
        }
    }
}