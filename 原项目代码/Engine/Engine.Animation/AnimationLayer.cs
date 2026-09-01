using Engine.Animation.RootMotion;
using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// 动画层，负责管理单个动画或驱动器
    /// </summary>
    public class AnimationLayer {
        public AnimationPlayer m_animationPlayer;
        public IAnimationDriver m_driver;
        public AnimationTransition m_transition;
        public bool m_active = true; // 层是否激活（参与采样）
        public bool m_holdPose; // 手动控制期间保持当前姿态（直到释放）

        // 停用渐变过渡状态
        public bool m_deactivating;
        public float m_deactivateElapsed;
        public float m_deactivateDuration;
        public float m_originalWeight;
        public bool m_clearOnDeactivate; // 渐降完成时清空动画（状态规则停用），区别于普通停用（恢复 Weight + m_active=false）
        public float m_configuredWeight = 1f; // 配置权重（层初始化写入），权重渐入与中断恢复的目标值，独立于过渡中的 Weight（解耦避免 m_originalWeight 残留污染）

        // 激活渐变过渡状态（Override 层的权重渐入）
        public bool m_activating;
        public float m_activateElapsed;
        public float m_activateDuration;
        public float m_targetWeight;
        public float m_activateFromWeight; // 激活/恢复渐入起点（首播 0；中断恢复用当前 Weight）
        public Matrix?[] m_activateSourceTransforms; // 激活过渡时的源姿态

        public RootMotionConfig m_rootMotionConfig;          // 当前/目标 config（Base 专属；非 Base 恒 null）
        RootStripInfo m_activateSourceStripInfo;             // Override 渐入冻结源剥离需求
        RootStripInfo m_deactivateStripInfo;                 // Override 权重渐降 live 源剥离需求
        string m_rootBoneName;                               // 控制器 RootBoneName（SetRootMotionConfig 时缓存）

        // 骨骼遮罩展开集缓存（include 子树 − exclude 子树），按需重建
        HashSet<int> m_boneMaskSet;
        string[] m_maskRef;      // 上次展开时的 BoneMask 引用
        string[] m_excludeRef;   // 上次展开时的 BoneMaskExclude 引用
        Model m_maskModel;        // 上次展开时的 Model 引用
        int m_maskBoneCount = -1;

        /// <summary>
        /// 层名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 层索引
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// 动画播放器（用于 pointer 采样）
        /// </summary>
        public AnimationPlayer Player => m_animationPlayer;

        /// <summary>
        /// 混合模式
        /// </summary>
        public AnimationBlendMode BlendMode { get; }

        /// <summary>
        /// 骨骼遮罩（null 表示影响所有骨骼）。
        /// 列出的骨名按子树展开：包含该骨 + 其全部后代。
        /// </summary>
        public string[] BoneMask { get; set; }

        /// <summary>
        /// 骨骼遮罩排除（同子树语义，从结果集中扣除）。
        /// 当 <see cref="BoneMask"/> 为空时，种子为全部骨骼，再扣除此处子树（即"除 X 外全部"）。
        /// </summary>
        public string[] BoneMaskExclude { get; set; }

        /// <summary>
        /// 混合权重 (0-1)
        /// </summary>
        public float Weight { get; set; } = 1f;

        /// <summary>
        /// 过渡曲线（用于激活/停用渐变）
        /// </summary>
        public BlendCurve Curve { get; set; } = BlendCurve.Linear;

        /// <summary>
        /// 是否有活动内容（动画或驱动器）且已激活
        /// </summary>
        public bool IsActive => (m_active || m_deactivating || m_activating || m_holdPose)
            && (m_animationPlayer?.IsPlaying == true
                || (m_animationPlayer?.PreservePose == true && m_animationPlayer.HasValidAnimation)
                || (m_holdPose && m_animationPlayer?.HasValidAnimation == true)
                || m_driver != null
                || m_transition?.IsActive == true);

        /// <summary>
        /// 动画播放器（过渡期间返回目标播放器）
        /// </summary>
        public AnimationPlayer AnimationPlayer => m_transition?.IsActive == true ? m_transition.TargetPlayer : m_animationPlayer;

        /// <summary>
        /// 驱动器
        /// </summary>
        public IAnimationDriver Driver => m_driver;

        /// <summary>
        /// 当前过渡（如果没有活动过渡则为 null）
        /// </summary>
        public AnimationTransition Transition => m_transition?.IsActive == true ? m_transition : null;

        /// <summary>
        /// 动画事件触发时调用（统一转发主播放器和过渡播放器的事件）
        /// </summary>
        public event AnimationEventHandler OnAnimationEvent;

        // 主播放器事件转发委托（构造器订阅原始 player；promote target→主 player 时迁移订阅）。
        AnimationEventHandler m_playerEventHandler;

        /// <summary>
        /// 创建动画层
        /// </summary>
        public AnimationLayer(string name, int index, AnimationBlendMode blendMode,
            string[] boneMask = null, string[] boneMaskExclude = null) {
            Name = name;
            Index = index;
            BlendMode = blendMode;
            BoneMask = boneMask;
            BoneMaskExclude = boneMaskExclude;
            m_animationPlayer = new AnimationPlayer();
            m_transition = new AnimationTransition();

            // 订阅主播放器的事件，转发到层的事件（持有委托引用，promote 时迁移到新主 player）
            m_playerEventHandler = evt => OnAnimationEvent?.Invoke(evt);
            m_animationPlayer.OnAnimationEvent += m_playerEventHandler;

            // 订阅过渡的 TargetPlayer 事件，转发到层的事件
            m_transition.TargetPlayerEvent += evt => OnAnimationEvent?.Invoke(evt);
        }

        /// <summary>
        /// 检查指定骨骼是否在该层的遮罩中（include 子树 − exclude 子树）。
        /// 首次调用或遮罩/骨数变化时按需展开并缓存。
        /// </summary>
        public bool IsBoneInMask(int boneIndex, Model model) {
            bool noInclude = BoneMask == null || BoneMask.Length == 0;
            bool noExclude = BoneMaskExclude == null || BoneMaskExclude.Length == 0;
            if (noInclude && noExclude) {
                return true; // 无遮罩 = 全部骨骼
            }
            EnsureBoneMaskSet(model, noInclude);
            return m_boneMaskSet.Contains(boneIndex);
        }

        /// <summary>
        /// 按需重建骨骼遮罩展开集。
        /// 失效判据：mask/exclude 数组引用变化、Model 引用变化或模型骨数变化。
        /// 假设：BoneMask/BoneMaskExclude 仅整体替换（重新赋值属性），不在原地修改数组元素
        /// （原地改 BoneMask[i] 不改变数组引用，缓存不会失效）。当前所有配置加载路径
        /// 仅整体赋值数组一次，满足该假设；若未来引入运行时原地修改，需改用内容指纹或显式失效。
        /// </summary>
        void EnsureBoneMaskSet(Model model, bool noInclude) {
            int boneCount = model.Bones.Count;
            if (m_boneMaskSet != null
                && ReferenceEquals(m_maskRef, BoneMask)
                && ReferenceEquals(m_excludeRef, BoneMaskExclude)
                && ReferenceEquals(m_maskModel, model)
                && m_maskBoneCount == boneCount) {
                return; // mask/exclude 数组引用 + 模型 + 骨数均未变 → 复用
            }
            HashSet<int> set = new();
            if (noInclude) {
                for (int i = 0; i < boneCount; i++) {
                    set.Add(i); // 种子 = 全部骨骼
                }
            }
            else {
                foreach (string name in BoneMask) {
                    ModelBone b = model.FindBone(name, false);
                    if (b != null) {
                        AddSubtree(b, set);
                    }
                }
            }
            if (BoneMaskExclude != null) {
                foreach (string name in BoneMaskExclude) {
                    ModelBone b = model.FindBone(name, false);
                    if (b != null) {
                        RemoveSubtree(b, set);
                    }
                }
            }
            m_boneMaskSet = set;
            m_maskRef = BoneMask;
            m_excludeRef = BoneMaskExclude;
            m_maskModel = model;
            m_maskBoneCount = boneCount;
        }

        static void AddSubtree(ModelBone bone, HashSet<int> set) {
            set.Add(bone.Index);
            foreach (ModelBone c in bone.ChildBones) {
                AddSubtree(c, set);
            }
        }

        static void RemoveSubtree(ModelBone bone, HashSet<int> set) {
            set.Remove(bone.Index);
            foreach (ModelBone c in bone.ChildBones) {
                RemoveSubtree(c, set);
            }
        }

        /// <summary>
        /// 设置驱动器
        /// </summary>
        public void SetDriver(IAnimationDriver driver) {
            m_driver = driver;
            m_animationPlayer?.Stop();
            m_transition?.CancelTransition();
            m_active = true; // 设置驱动器时激活层
        }

        /// <summary>
        /// 停用层（不参与采样，但保留驱动器）
        /// </summary>
        public void Deactivate() {
            m_active = false;
            m_holdPose = false;
            m_animationPlayer?.Stop();
            m_transition?.CancelTransition();
            CancelTransitioning();
        }

        /// <summary>
        /// 设置保持姿态模式（手动控制时使用）
        /// 当非循环动画结束后，保持当前姿态直到释放
        /// </summary>
        /// <param name="hold">true 表示启用保持姿态模式；false 表示禁用</param>
        public void SetHoldPose(bool hold) {
            m_holdPose = hold;
        }

        /// <summary>
        /// 设置层的根运动配置；config 切换时把 old/new 剥离需求盖戳到运行中的 blend 状态。
        /// info 寿命 == blend 状态寿命，状态完成/取消时清除 → 剥离覆盖期 == blend 期，无需计时器。
        /// </summary>
        public void SetRootMotionConfig(RootMotionConfig newConfig, Model model, string rootBoneName) {
            m_rootBoneName = rootBoneName;
            RootStripInfo oldInfo = RootStripInfo.From(m_rootMotionConfig, rootBoneName);
            RootStripInfo newInfo = RootStripInfo.From(newConfig, rootBoneName);

            if (m_transition?.IsActive == true) {
                // crossfade：源=old，目标=new；deactivate 变体目标不剥
                m_transition.SetStripInfo(oldInfo, m_transition.IsDeactivateTransition ? default : newInfo);
            }
            else if (m_activating) {
                m_activateSourceStripInfo = oldInfo;   // 目标(主player)用 new(=更新后 m_rootMotionConfig)
            }
            else if (m_deactivating) {
                m_deactivateStripInfo = oldInfo;       // 渐降 live 源用旧 config
            }
            // else 稳态：活动 player 采样按 new(=更新后 m_rootMotionConfig)剥

            m_rootMotionConfig = newConfig;
        }

        /// <summary>
        /// 取消所有渐变过渡状态，恢复权重
        /// </summary>
        public void CancelTransitioning() {
            if (m_activating) {
                m_activating = false;
                Weight = m_targetWeight;
                // 清除源姿态
                if (m_activateSourceTransforms != null) {
                    Array.Clear(m_activateSourceTransforms, 0, m_activateSourceTransforms.Length);
                }
                m_activateSourceStripInfo = default;
            }
            if (m_deactivating) {
                bool wasClearOnDeactivate = m_clearOnDeactivate;
                m_deactivating = false;
                m_clearOnDeactivate = false; // 中断渐降时同步清 flag，避免残留误触发后续完成分支清空
                m_deactivateStripInfo = default;
                if (!wasClearOnDeactivate) {
                    // 普通 DeactivateWithBlend 取消：恢复原权重
                    Weight = m_originalWeight;
                }
                else {
                    // 状态规则停用被中断（条件快速反复）：不瞬间恢复 Weight（避免 pop），
                    // 改启动权重恢复子过渡，从当前渐降 Weight 渐回配置权重（I-2）。
                    // 姿态由调用方后续的交叉淡入过渡采样，此处仅渐权重（sourceTransforms=null）。
                    if (m_configuredWeight > 0f && Weight < m_configuredWeight) {
                        m_activateFromWeight = Weight;
                        m_targetWeight = m_configuredWeight;
                        m_activateSourceTransforms = null;
                        m_activating = true;
                        m_activateElapsed = 0f;
                        m_activateDuration = m_deactivateDuration > 0f ? m_deactivateDuration : 0.2f;
                    }
                }
            }
        }

        /// <summary>
        /// 带过渡效果的停用层
        /// </summary>
        /// <param name="blendDuration">过渡时长（秒）</param>
        /// <returns>是否成功开始过渡</returns>
        public bool DeactivateWithBlend(float blendDuration = 0.25f) {
            // 如果层已经不活动或没有播放动画，直接停用
            if (!m_active
                || m_animationPlayer == null
                || !m_animationPlayer.IsPlaying) {
                Deactivate();
                return false;
            }

            // 对于 Override 模式的层，使用权重渐变过渡
            // 逐渐降低 Weight，让下层内容平滑显现
            if (BlendMode == AnimationBlendMode.Override) {
                if (blendDuration <= 0f) {
                    Deactivate();
                    return true;
                }
                m_deactivating = true;
                m_deactivateElapsed = 0f;
                m_deactivateDuration = blendDuration;
                m_originalWeight = Weight;
                m_clearOnDeactivate = false; // DeactivateWithBlend 是普通停用，确保不继承先前 DeactivateAndClear 的 flag（I-1）
                return true;
            }

            // 对于 Additive 模式的层，使用过渡淡出到 Identity
            bool started = m_transition.StartDeactivateTransition(m_animationPlayer, blendDuration);
            if (!started) {
                Deactivate();
            }
            return started;
        }

        /// <summary>
        /// 激活层
        /// </summary>
        public void Activate() {
            m_active = true;
            CancelTransitioning();
        }

        /// <summary>
        /// 播放动画（立即切换，无过渡）
        /// </summary>
        public void PlayAnimation(Model model, ModelAnimation animation, bool loop = true) {
            m_driver = null; // 清除驱动器
            m_transition?.CancelTransition();
            CancelTransitioning();
            m_animationPlayer.SetAnimation(model, animation);
            m_animationPlayer.Play(loop);
            m_active = true; // 激活层
        }

        /// <summary>
        /// 播放动画（带过渡效果）
        /// </summary>
        /// <param name="model">模型</param>
        /// <param name="animation">目标动画</param>
        /// <param name="loop">是否循环</param>
        /// <param name="transitionDuration">过渡时长（秒）</param>
        /// <param name="interruptMode">中断模式</param>
        /// <param name="priority">过渡优先级</param>
        /// <returns>是否成功开始播放</returns>
        public bool PlayAnimationWithTransition(Model model,
            ModelAnimation animation,
            bool loop = true,
            float transitionDuration = 0.25f,
            TransitionInterruptMode interruptMode = TransitionInterruptMode.CanInterrupt,
            int priority = 0) {
            m_driver = null; // 清除驱动器
            m_active = true; // 激活层
            CancelTransitioning();

            // 对于 Override 层，如果之前没有任何动画（Animation == null）
            // 使用权重渐入代替动画过渡
            // 注意：如果 Animation 存在但 IsPlaying=false（非循环动画结束），
            // 应该使用正常过渡，因为我们有源姿态可以过渡
            if (BlendMode == AnimationBlendMode.Override
                && m_animationPlayer.Animation == null
                && transitionDuration > 0f) {
                // 目标权重：配置权重（层初始化写入，独立于过渡中的 Weight）。
                // 用 m_configuredWeight 而非 m_originalWeight：后者停用完成后不重置会残留，污染下次渐入目标（I-3）。
                m_targetWeight = m_configuredWeight > 0 ? m_configuredWeight : 1f;

                // 采样当前姿态作为源姿态（用于平滑过渡）
                if (m_animationPlayer.Animation != null
                    && model != null) {
                    int boneCount = model.Bones.Count;
                    if (boneCount > 0) {
                        if (m_activateSourceTransforms == null
                            || m_activateSourceTransforms.Length < boneCount) {
                            m_activateSourceTransforms = new Matrix?[boneCount];
                        }
                        Array.Clear(m_activateSourceTransforms, 0, boneCount);
                        m_animationPlayer.SampleBoneTransforms(m_activateSourceTransforms);
                    }
                }

                // 直接播放目标动画
                m_animationPlayer.SetAnimation(model, animation);
                m_animationPlayer.Play(loop);

                // 启动权重渐入
                Weight = 0f;
                m_activateFromWeight = 0f;
                m_activating = true;
                m_activateElapsed = 0f;
                m_activateDuration = transitionDuration;
                return true;
            }

            // 检查是否可以开始新过渡
            if (!m_transition.CanStartNewTransition(priority)) {
                return false;
            }

            // 中断现有过渡时，采样当前实际渲染姿态作新过渡源。
            // 否则源会退化为 m_animationPlayer（已被上次 SetAnimation 切到旧 target，time≈0）的首帧，
            // 导致新过渡 blend 起点突变（如 run→walk→idle 切换时"立即变 idle"）。
            Matrix?[] sourceSnapshot = null;
            if (m_transition.IsActive && model != null) {
                sourceSnapshot = new Matrix?[model.Bones.Count];
                SampleTransforms(sourceSnapshot, model);
            }

            // 开始过渡
            bool started = m_transition.StartTransition(
                m_animationPlayer,
                model,
                animation,
                loop,
                transitionDuration,
                interruptMode,
                priority,
                sourceSnapshot
            );
            if (started) {
                // 同时更新主播放器，这样外部查询和速度设置都能正常工作
                m_animationPlayer.SetAnimation(model, animation);
                m_animationPlayer.Play(loop);
            }
            return started;
        }

        /// <summary>
        /// 交叉淡入淡出到新动画
        /// </summary>
        /// <param name="model">模型</param>
        /// <param name="animation">目标动画</param>
        /// <param name="duration">过渡时长</param>
        /// <param name="loop">是否循环</param>
        /// <returns>是否成功开始交叉淡入淡出</returns>
        public bool CrossFade(Model model, ModelAnimation animation, float duration, bool loop = true) =>
            PlayAnimationWithTransition(model, animation, loop, duration);

        /// <summary>
        /// 停止动画
        /// </summary>
        public void StopAnimation() {
            m_transition?.CancelTransition();
            m_animationPlayer?.Stop();
        }

        /// <summary>
        /// 清空层的动画内容（停止播放 + 清除动画引用 + 取消过渡 + 取消渐变）。
        /// 用于状态规则停用：使层进入"空 active"态（Animation=null, IsPlaying=false）。
        /// 注意 AnimationPlayer.Stop 只置 m_playing=false，不清 m_animation —— 此方法显式清空，
        /// 这样下次 PlayAnimationWithTransition 会走权重渐入分支（Animation==null 判据），
        /// 而非交叉淡入分支（Animation 非null + Weight 已恢复 → 硬切）。配合 Weight=0 使用。
        /// </summary>
        public void ClearAnimation() {
            m_animationPlayer?.Stop();
            if (m_animationPlayer != null) {
                m_animationPlayer.m_animation = null;
            }
            m_transition?.CancelTransition();
            CancelTransitioning();
        }

        /// <summary>
        /// 状态规则停用：权重渐降平滑淡出，完成时清空动画并进入"空 active"态
        ///（Animation=null, Weight=0, m_active=true），使下次播放走权重渐入分支（Animation==null 判据）实现重启平滑。
        /// 区别于 DeactivateWithBlend（完成 m_active=false + 恢复 Weight，且不清 Animation → 下次播放硬切）。
        /// 渐降期间保留动画采样，淡出可见；完成在 Update 的 m_deactivating 分支处理（m_clearOnDeactivate flag）。
        /// Additive 层例外：委托 DeactivateWithBlend（交叉淡出到 Identity，不清空 —— 其重启走交叉淡入，不依赖 Animation==null）。
        /// </summary>
        public void DeactivateAndClear(float blendDuration) {
            if (BlendMode == AnimationBlendMode.Additive) {
                // Additive 层不清空 Animation：重启走交叉淡入（PlayAnimationWithTransition 的 m_transition 分支），
                // 不依赖 Animation==null 的权重渐入判据（Override 专属）；清空反而会使交叉淡入 source 为空。
                DeactivateWithBlend(blendDuration);
                return;
            }
            // preservePose 保持的非循环动画末态（IsPlaying=false 但 PreservePose+HasValidAnimation）
            // 也算"有内容"可渐降淡出——如 attacked 仅取首帧后倾（endPhase=0），停用时应平滑淡出而非瞬间消失 pop。
            bool hasBlendableContent = m_animationPlayer?.IsPlaying == true
                || (m_animationPlayer?.PreservePose == true && m_animationPlayer?.HasValidAnimation == true);
            if (blendDuration <= 0f
                || !IsActive
                || !hasBlendableContent) {
                // 无活动动画或无过渡时长：直接清空进入"空 active"态
                m_deactivating = false;
                ClearAnimation();
                Weight = 0f;
                m_active = true;
                return;
            }
            // 取消进行中的激活渐入（激活渐入未完成即触发停用的边界场景），避免两套 Weight 渐变并行
            if (m_activating) {
                m_activating = false;
                m_activateSourceStripInfo = default; // 对称清除（与 CancelTransitioning/Update 完成分支一致），防陈旧 info 跨动画泄漏
                if (m_activateSourceTransforms != null) {
                    Array.Clear(m_activateSourceTransforms, 0, m_activateSourceTransforms.Length);
                }
            }
            m_originalWeight = Weight;
            m_deactivating = true;
            m_deactivateElapsed = 0f;
            m_deactivateDuration = blendDuration;
            m_clearOnDeactivate = true;
        }

        /// <summary>
        /// 更新层状态
        /// </summary>
        public void Update(float deltaTime, AnimationParameters parameters) {
            // 更新激活渐变过渡（Override 模式的权重渐入）
            if (m_activating) {
                m_activateElapsed += deltaTime;
                float progress = m_activateDuration > 0 ? AnimationTransition.ApplyCurve(m_activateElapsed / m_activateDuration, Curve) : 1f;
                if (progress >= 1f) {
                    // 过渡完成，设置目标权重
                    Weight = m_targetWeight;
                    m_activating = false;
                    // 清除源姿态
                    if (m_activateSourceTransforms != null) {
                        Array.Clear(m_activateSourceTransforms, 0, m_activateSourceTransforms.Length);
                    }
                    m_activateSourceStripInfo = default;
                }
                else {
                    // 渐变权重：从起点（首播 0 / 中断恢复为当前 Weight）渐变到目标权重
                    Weight = m_activateFromWeight + (m_targetWeight - m_activateFromWeight) * progress;
                }
            }

            // 更新停用渐变过渡（Override 模式的权重渐变）
            if (m_deactivating) {
                m_deactivateElapsed += deltaTime;
                float progress = m_deactivateDuration > 0 ? AnimationTransition.ApplyCurve(m_deactivateElapsed / m_deactivateDuration, Curve) : 1f;
                if (progress >= 1f) {
                    m_deactivating = false;
                    m_deactivateStripInfo = default;
                    if (m_clearOnDeactivate) {
                        // 状态规则停用：渐降完成 → 清空动画 + 保持 active + Weight=0，
                        // 使下次播放走权重渐入（Animation==null）实现平滑重启。
                        // m_deactivating 先置 false，避免 ClearAnimation→CancelTransitioning 把 Weight 恢复成 m_originalWeight。
                        m_clearOnDeactivate = false;
                        ClearAnimation();
                        Weight = 0f;
                        m_active = true;
                    }
                    else {
                        // 普通停用：恢复原始权重 + m_active=false
                        Weight = m_originalWeight;
                        m_active = false;
                    }
                }
                else {
                    // 渐变权重：从原始权重渐变到 0
                    Weight = m_originalWeight * (1f - progress);
                }
            }

            // 更新过渡
            if (m_transition?.IsActive == true) {
                m_transition.Update(deltaTime);

                // 检查过渡是否刚完成
                if (!m_transition.IsActive) {
                    if (m_transition.IsDeactivateTransition) {
                        // 停用过渡完成，停用层
                        m_transition.CompleteTransition();
                        m_active = false;
                    }
                    else if (m_transition.TargetPlayer != null) {
                        // 过渡完成，切换到目标动画。
                        // promote target→主播放器：迁移层事件订阅。构造器只订阅了原始 player，
                        // target 仅靠 transition relay 转发；relay 在后续 CancelTransition(ClearAnimation) 被摘，
                        // 会使复用该 player 的 FADEIN 事件全哑（CheckEvents 守卫 OnAnimationEvent==null 早退）。
                        AnimationPlayer promoted = m_transition.TargetPlayer;
                        m_transition.DetachTargetRelay();                            // 摘 relay，防换后 relay+handler 双触发
                        m_animationPlayer.OnAnimationEvent -= m_playerEventHandler;  // 摘旧主订阅
                        m_animationPlayer = promoted;
                        m_animationPlayer.OnAnimationEvent += m_playerEventHandler;  // 焊新主订阅
                        m_transition.CompleteTransition();
                    }
                }
            }
            // 更新普通动画
            else if (m_animationPlayer?.IsPlaying == true) {
                m_animationPlayer.Update(deltaTime);
            }

            // 更新驱动器
            if (m_driver != null) {
                m_driver.Update(deltaTime, parameters);
            }
        }

        /// <summary>
        /// 采样当前层的骨骼变换
        /// </summary>
        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            // 激活渐变过渡：从保存的源姿态混合到目标动画
            if (m_activating
                && m_activateSourceTransforms != null
                && model != null) {
                float progress = m_activateDuration > 0 ? AnimationTransition.ApplyCurve(Math.Clamp(m_activateElapsed / m_activateDuration, 0f, 1f), Curve) : 1f;

                // 采样目标动画
                m_animationPlayer?.SampleBoneTransforms(boneTransforms);

                // 源/目标各按自身 info 剥根（幂等，可原地改写）
                RootMotionStrip.StripRootTranslation(boneTransforms, model, RootStripInfo.From(m_rootMotionConfig, m_rootBoneName));
                RootMotionStrip.StripRootTranslation(m_activateSourceTransforms, model, m_activateSourceStripInfo);

                // 混合源姿态和目标姿态
                int boneCount = Math.Min(boneTransforms.Length, m_activateSourceTransforms.Length);
                for (int i = 0; i < boneCount; i++) {
                    if (boneTransforms[i].HasValue
                        && m_activateSourceTransforms[i].HasValue) {
                        // 两者都有值：正常混合
                        boneTransforms[i] = BlendTransforms(m_activateSourceTransforms[i].Value, boneTransforms[i].Value, progress);
                    }
                    else if (m_activateSourceTransforms[i].HasValue) {
                        // 只有源姿态有值：从源姿态淡出
                        boneTransforms[i] = BlendTransforms(m_activateSourceTransforms[i].Value, Matrix.Identity, progress);
                    }
                    // 如果只有目标有值，保持目标值（不需要处理）
                }
                return;
            }

            // 如果有活动过渡，使用过渡采样
            if (m_transition?.IsActive == true) {
                m_transition.SampleTransforms(boneTransforms, model);
            }
            // 否则使用普通动画采样
            else if (m_animationPlayer != null
                && m_animationPlayer.IsPlaying) {
                m_animationPlayer.SampleBoneTransforms(boneTransforms);
                RootStripInfo info = m_deactivating ? m_deactivateStripInfo
                                                    : RootStripInfo.From(m_rootMotionConfig, m_rootBoneName);
                RootMotionStrip.StripRootTranslation(boneTransforms, model, info);
            }
            // preservePose: 非循环动画结束后持续保持最终帧
            else if (m_animationPlayer != null
                && m_animationPlayer.PreservePose
                && m_animationPlayer.HasValidAnimation) {
                m_animationPlayer.SampleBoneTransformsAtPhase(m_animationPlayer.EndPhase, boneTransforms);
                // 与 IsPlaying 分支同：deactivating 时按旧 config(=m_deactivateStripInfo)剥，防尾段根位移泄漏
                RootStripInfo info = m_deactivating ? m_deactivateStripInfo
                                                    : RootStripInfo.From(m_rootMotionConfig, m_rootBoneName);
                RootMotionStrip.StripRootTranslation(boneTransforms, model, info);
            }
            // holdPose: 手动控制期间非循环动画结束后保持当前姿态
            else if (m_holdPose
                && m_animationPlayer != null
                && m_animationPlayer.HasValidAnimation) {
                m_animationPlayer.SampleBoneTransforms(boneTransforms);
                RootStripInfo info = m_deactivating ? m_deactivateStripInfo
                                                    : RootStripInfo.From(m_rootMotionConfig, m_rootBoneName);
                RootMotionStrip.StripRootTranslation(boneTransforms, model, info);
            }
            // 最后尝试驱动器
            else if (m_driver != null) {
                m_driver.SampleTransforms(boneTransforms, model);
            }
        }

        /// <summary>
        /// 混合两个变换矩阵
        /// </summary>
        public static Matrix BlendTransforms(Matrix a, Matrix b, float t) {
            a.Decompose(out Vector3 tA, out Quaternion rA, out Vector3 sA);
            b.Decompose(out Vector3 tB, out Quaternion rB, out Vector3 sB);
            return Matrix.CreateScale(Vector3.Lerp(sA, sB, t))
                * Matrix.CreateFromQuaternion(Quaternion.Slerp(rA, rB, t))
                * Matrix.CreateTranslation(Vector3.Lerp(tA, tB, t));
        }
    }
}