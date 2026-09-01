using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// 过渡中断策略
    /// </summary>
    public enum TransitionInterruptMode {
        /// <summary>
        /// 可以被新过渡中断
        /// </summary>
        CanInterrupt,

        /// <summary>
        /// 不可以被新过渡中断，必须等待过渡完成
        /// </summary>
        CannotInterrupt,

        /// <summary>
        /// 只有更高优先级的过渡才能中断
        /// </summary>
        HigherPriorityOnly
    }

    /// <summary>
    /// 过渡曲线类型
    /// </summary>
    public enum BlendCurve {
        /// <summary>
        /// 线性插值
        /// </summary>
        Linear,
        /// <summary>
        /// 平滑过渡（smoothstep: t*t*(3-2t)）
        /// </summary>
        Smoothstep
    }

    /// <summary>
    /// 动画过渡，负责管理两个动画之间的平滑过渡
    /// </summary>
    public class AnimationTransition {
        public float m_elapsedTime;
        public float m_duration;
        public bool m_isActive;
        public int m_priority;

        // 过渡源状态
        public AnimationPlayer m_sourcePlayer;
        public Matrix?[] m_sourceTransforms;

        // 过渡目标状态
        public AnimationPlayer m_targetPlayer;
        public Model m_targetModel;
        public ModelAnimation m_targetAnimation;
        public bool m_targetLoop;
        public AnimationEventHandler m_targetPlayerEventHandler; // 保存委托引用用于取消订阅

        // 停用过渡模式：淡出到无
        public bool m_isDeactivateTransition;

        RootStripInfo m_sourceStripInfo;   // crossfade/deactivate 源快照剥离需求
        RootStripInfo m_targetStripInfo;   // crossfade 目标 player 剥离需求（deactivate 不用）

        /// <summary>
        /// 过渡时长（秒）
        /// </summary>
        public float Duration {
            get => m_duration;
            set => m_duration = Math.Max(0f, value);
        }

        /// <summary>
        /// 过渡曲线
        /// </summary>
        public BlendCurve Curve { get; set; } = BlendCurve.Linear;

        /// <summary>
        /// 已过渡时间（秒）
        /// </summary>
        public float ElapsedTime => m_elapsedTime;

        /// <summary>
        /// 过渡进度 (0-1)
        /// </summary>
        public float Progress => m_duration > 0 ? ApplyCurve(Math.Min(1f, m_elapsedTime / m_duration), Curve) : 1f;

        /// <summary>
        /// 应用过渡曲线
        /// </summary>
        public static float ApplyCurve(float t, BlendCurve curve) {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return curve switch {
                BlendCurve.Smoothstep => t * t * (3f - 2f * t),
                _ => t
            };
        }

        /// <summary>
        /// 过渡是否正在进行
        /// </summary>
        public bool IsActive => m_isActive;

        /// <summary>
        /// 过渡中断模式
        /// </summary>
        public TransitionInterruptMode InterruptMode { get; set; } = TransitionInterruptMode.CanInterrupt;

        /// <summary>
        /// 过渡优先级（数值越大优先级越高）
        /// </summary>
        public int Priority => m_priority;

        /// <summary>
        /// 目标动画播放器
        /// </summary>
        public AnimationPlayer TargetPlayer => m_targetPlayer;

        /// <summary>
        /// 目标动画播放器的事件处理器（用于复制订阅）
        /// </summary>
        public event AnimationEventHandler TargetPlayerEvent;

        /// <summary>
        /// 创建动画过渡实例
        /// </summary>
        public AnimationTransition() => m_duration = 0.25f; // 默认过渡时长 0.25 秒

        /// <summary>
        /// 开始过渡到新动画
        /// </summary>
        /// <param name="sourcePlayer">源动画播放器（可以为 null）</param>
        /// <param name="targetModel">目标模型</param>
        /// <param name="targetAnimation">目标动画</param>
        /// <param name="loop">是否循环</param>
        /// <param name="duration">过渡时长</param>
        /// <param name="interruptMode">中断模式</param>
        /// <param name="priority">优先级</param>
        /// <param name="sourceTransformsOverride">可选的源姿态快照；中断现有过渡时由调用方传入当前实际渲染姿态，避免源退化为主 player 首帧。为 null 时从 sourcePlayer 采样</param>
        /// <returns>是否成功开始过渡</returns>
        public bool StartTransition(AnimationPlayer sourcePlayer,
            Model targetModel,
            ModelAnimation targetAnimation,
            bool loop,
            float duration,
            TransitionInterruptMode interruptMode = TransitionInterruptMode.CanInterrupt,
            int priority = 0,
            Matrix?[] sourceTransformsOverride = null) {
            // 检查是否可以被中断
            if (m_isActive) {
                if (InterruptMode == TransitionInterruptMode.CannotInterrupt) {
                    return false;
                }
                if (InterruptMode == TransitionInterruptMode.HigherPriorityOnly
                    && priority <= m_priority) {
                    return false;
                }
            }

            // 保存源状态
            m_sourcePlayer = sourcePlayer;
            // 源姿态来源：
            // - sourceTransformsOverride：中断现有过渡时，由调用方采样的当前实际渲染姿态
            //   （旧过渡的混合输出），避免源退化为已被 SetAnimation 切换的主 player 首帧
            // - 否则：从源 player 采样当前姿态（即使源动画已停止，也能正确混合）
            int boneCount = targetModel != null ? targetModel.Bones.Count : 0;
            if (boneCount > 0) {
                EnsureSourceBufferSize(boneCount);
                if (sourceTransformsOverride != null) {
                    int copyLen = Math.Min(boneCount, sourceTransformsOverride.Length);
                    for (int i = 0; i < copyLen; i++) {
                        m_sourceTransforms[i] = sourceTransformsOverride[i];
                    }
                }
                else if (m_sourcePlayer != null
                    && m_sourcePlayer.Animation != null) {
                    m_sourcePlayer.SampleBoneTransforms(m_sourceTransforms);
                }
            }

            // 设置目标状态
            m_targetModel = targetModel;
            m_targetAnimation = targetAnimation;
            m_targetLoop = loop;

            // 取消旧播放器的事件订阅（如果有）
            if (m_targetPlayer != null
                && m_targetPlayerEventHandler != null) {
                m_targetPlayer.OnAnimationEvent -= m_targetPlayerEventHandler;
            }
            m_targetPlayer = new AnimationPlayer();
            m_targetPlayer.SetAnimation(targetModel, targetAnimation);
            m_targetPlayer.Play(loop);

            // 订阅目标播放器的事件到过渡的事件（保存委托引用以便取消订阅）
            m_targetPlayerEventHandler = evt => TargetPlayerEvent?.Invoke(evt);
            m_targetPlayer.OnAnimationEvent += m_targetPlayerEventHandler;

            // 设置过渡参数
            m_duration = Math.Max(0f, duration);
            m_elapsedTime = 0f;
            m_isActive = true;
            m_priority = priority;
            InterruptMode = interruptMode;
            m_isDeactivateTransition = false;
            return true;
        }

        /// <summary>
        /// 更新过渡状态
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        public void Update(float deltaTime) {
            if (!m_isActive) {
                return;
            }
            m_elapsedTime += deltaTime;

            // 更新目标动画
            m_targetPlayer?.Update(deltaTime);

            // 检查过渡是否完成
            if (m_elapsedTime >= m_duration) {
                CompleteTransition();
            }
        }

        /// <summary>
        /// 盖戳源/目标剥离需求（config 切换时由层调用）。info 寿命 == blend 状态寿命。
        /// </summary>
        public void SetStripInfo(RootStripInfo source, RootStripInfo target) {
            m_sourceStripInfo = source;
            m_targetStripInfo = target;
        }

        /// <summary>
        /// 采样当前过渡状态的骨骼变换
        /// </summary>
        /// <param name="boneTransforms">输出骨骼变换数组</param>
        /// <param name="model">模型对象</param>
        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            if (!m_isActive
                || boneTransforms == null
                || model == null) {
                // 如果没有活动过渡，直接采样目标动画
                m_targetPlayer?.SampleBoneTransforms(boneTransforms);
                return;
            }
            int boneCount = model.Bones.Count;

            // 入口统一剥源快照（幂等）：源冻结，首帧剥后 translation=rest，后续帧再剥无变化
            RootMotionStrip.StripRootTranslation(m_sourceTransforms, model, m_sourceStripInfo);

            // 停用过渡模式：淡出源动画到 Identity
            if (m_isDeactivateTransition) {
                if (m_sourceTransforms == null
                    || Progress >= 1f) {
                    // 过渡完成，不做任何事（层将被停用）
                    return;
                }

                // 淡出源变换到 Identity
                for (int i = 0; i < boneCount; i++) {
                    if (m_sourceTransforms[i].HasValue) {
                        boneTransforms[i] = BlendTransforms(m_sourceTransforms[i].Value, Matrix.Identity, Progress);
                    }
                }
                return;
            }

            // 如果进度为 0 或没有源变换，使用目标变换
            // 注意：即使源动画已停止播放，只要 _sourceTransforms 有值就应该进行混合
            if (Progress <= 0f
                || m_sourceTransforms == null) {
                m_targetPlayer?.SampleBoneTransforms(boneTransforms);
                RootMotionStrip.StripRootTranslation(boneTransforms, model, m_targetStripInfo);
                return;
            }

            // 如果进度为 1，使用目标变换
            if (Progress >= 1f) {
                m_targetPlayer?.SampleBoneTransforms(boneTransforms);
                RootMotionStrip.StripRootTranslation(boneTransforms, model, m_targetStripInfo);
                return;
            }

            // 混合源和目标变换
            Matrix?[] targetTransforms = new Matrix?[boneCount];
            m_targetPlayer?.SampleBoneTransforms(targetTransforms);
            RootMotionStrip.StripRootTranslation(targetTransforms, model, m_targetStripInfo);
            for (int i = 0; i < boneCount; i++) {
                if (targetTransforms[i].HasValue) {
                    if (m_sourceTransforms[i].HasValue) {
                        // 混合变换
                        boneTransforms[i] = BlendTransforms(m_sourceTransforms[i].Value, targetTransforms[i].Value, Progress);
                    }
                    else {
                        boneTransforms[i] = targetTransforms[i].Value;
                    }
                }
                else if (m_sourceTransforms[i].HasValue) {
                    // 从源变换淡出
                    boneTransforms[i] = BlendTransforms(m_sourceTransforms[i].Value, Matrix.Identity, Progress);
                }
            }
        }

        /// <summary>
        /// 完成过渡
        /// </summary>
        public void CompleteTransition() {
            m_isActive = false;
            m_sourcePlayer = null;
            m_sourceTransforms = null;
            m_isDeactivateTransition = false;
            m_sourceStripInfo = default;
            m_targetStripInfo = default;
        }

        /// <summary>
        /// 是否是停用过渡
        /// </summary>
        public bool IsDeactivateTransition => m_isDeactivateTransition;

        /// <summary>
        /// 取消过渡
        /// </summary>
        public void CancelTransition() {
            // 取消目标播放器的事件订阅
            if (m_targetPlayer != null
                && m_targetPlayerEventHandler != null) {
                m_targetPlayer.OnAnimationEvent -= m_targetPlayerEventHandler;
            }
            m_isActive = false;
            m_sourcePlayer = null;
            m_sourceTransforms = null;
            m_targetPlayer = null;
            m_targetPlayerEventHandler = null;
            m_isDeactivateTransition = false;
            m_sourceStripInfo = default;
            m_targetStripInfo = default;
        }

        /// <summary>
        /// 仅摘除目标播放器的事件 relay（不动 m_targetPlayer/m_isActive）。
        /// promote target→主播放器时调用：摘 relay 防双触发，由 AnimationLayer 接管事件订阅。
        /// </summary>
        public void DetachTargetRelay() {
            if (m_targetPlayer != null
                && m_targetPlayerEventHandler != null) {
                m_targetPlayer.OnAnimationEvent -= m_targetPlayerEventHandler;
            }
            m_targetPlayerEventHandler = null;
        }

        /// <summary>
        /// 开始停用过渡（淡出到无）
        /// </summary>
        /// <param name="sourcePlayer">源动画播放器</param>
        /// <param name="duration">过渡时长</param>
        /// <returns>是否成功开始过渡</returns>
        public bool StartDeactivateTransition(AnimationPlayer sourcePlayer, float duration) {
            if (sourcePlayer == null
                || !sourcePlayer.IsPlaying) {
                return false;
            }

            // 检查是否可以被中断
            if (m_isActive && InterruptMode == TransitionInterruptMode.CannotInterrupt) {
                return false;
            }

            // 保存源状态
            m_sourcePlayer = sourcePlayer;
            Model model = sourcePlayer.Model;
            if (model != null) {
                EnsureSourceBufferSize(model.Bones.Count);
                sourcePlayer.SampleBoneTransforms(m_sourceTransforms);
            }

            // 清除目标状态
            m_targetPlayer = null;
            m_targetModel = null;
            m_targetAnimation = null;

            // 设置过渡参数
            m_duration = Math.Max(0f, duration);
            m_elapsedTime = 0f;
            m_isActive = true;
            m_isDeactivateTransition = true;
            m_priority = 0;
            InterruptMode = TransitionInterruptMode.CanInterrupt;
            return true;
        }

        /// <summary>
        /// 检查是否可以开始新过渡
        /// </summary>
        /// <param name="priority">新过渡的优先级</param>
        /// <returns>是否可以开始新过渡</returns>
        public bool CanStartNewTransition(int priority = 0) {
            if (!m_isActive) {
                return true;
            }
            if (InterruptMode == TransitionInterruptMode.CannotInterrupt) {
                return false;
            }
            if (InterruptMode == TransitionInterruptMode.HigherPriorityOnly
                && priority <= m_priority) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 确保源变换缓冲区大小足够
        /// </summary>
        public void EnsureSourceBufferSize(int requiredSize) {
            if (m_sourceTransforms == null
                || m_sourceTransforms.Length < requiredSize) {
                m_sourceTransforms = new Matrix?[Math.Max(requiredSize, 64)];
            }
            Array.Clear(m_sourceTransforms, 0, m_sourceTransforms.Length);
        }

        /// <summary>
        /// 混合两个变换矩阵
        /// </summary>
        public Matrix BlendTransforms(Matrix a, Matrix b, float t) {
            // 分解为 T、R、S 分别插值
            DecomposeMatrix(a, out Vector3 tA, out Quaternion rA, out Vector3 sA);
            DecomposeMatrix(b, out Vector3 tB, out Quaternion rB, out Vector3 sB);
            return Matrix.CreateScale(Vector3.Lerp(sA, sB, t))
                * Matrix.CreateFromQuaternion(Quaternion.Slerp(rA, rB, t))
                * Matrix.CreateTranslation(Vector3.Lerp(tA, tB, t));
        }

        /// <summary>
        /// 分解矩阵为平移、旋转、缩放
        /// </summary>
        public void DecomposeMatrix(Matrix m, out Vector3 translation, out Quaternion rotation, out Vector3 scale) {
            // 提取平移
            translation = m.Translation;

            // 提取缩放
            Vector3 right = new(m.M11, m.M12, m.M13);
            Vector3 up = new(m.M21, m.M22, m.M23);
            Vector3 forward = new(m.M31, m.M32, m.M33);
            float scaleX = right.Length();
            float scaleY = up.Length();
            float scaleZ = forward.Length();
            scale = new Vector3(scaleX, scaleY, scaleZ);

            // 提取旋转
            if (scaleX != 0) {
                right /= scaleX;
            }
            if (scaleY != 0) {
                up /= scaleY;
            }
            if (scaleZ != 0) {
                forward /= scaleZ;
            }
            Matrix rotationMatrix = new(
                right.X,
                right.Y,
                right.Z,
                0,
                up.X,
                up.Y,
                up.Z,
                0,
                forward.X,
                forward.Y,
                forward.Z,
                0,
                0,
                0,
                0,
                1
            );
            rotation = Quaternion.CreateFromRotationMatrix(rotationMatrix);

            // 处理负缩放
            if (scaleX * scaleY * scaleZ < 0) {
                scale = -scale;
            }
        }
    }
}