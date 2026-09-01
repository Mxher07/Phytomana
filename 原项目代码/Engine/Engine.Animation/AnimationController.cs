using Engine.Animation.RootMotion;
using Engine.Graphics;
using Engine.Media;

namespace Engine.Animation {
    /// <summary>
    /// 动画控制器，管理状态机、参数和层
    /// </summary>
    public class AnimationController {
        public readonly Model m_model;
        public readonly AnimationTemplate m_template;
        public readonly AnimationLayer[] m_layers;
        public readonly AnimationParameters m_parameters = new();
        public readonly AnimationBlender m_blender = new();
        public readonly StateRuleEvaluator m_ruleEvaluator = new();
        public readonly AnimationConfigLoader m_configLoader = new();

        // 共享的表达式求值器
        public readonly ExpressionEvaluator m_expressionEvaluator;

        // 根运动应用器
        public readonly TranslationApplier m_translationApplier = new();
        public readonly CollisionBoxApplier m_collisionBoxApplier = new();

        // 根运动缓存（按动画名称缓存）
        public readonly Dictionary<string, RootMotionCache> m_rootMotionCaches = new();
        public readonly Dictionary<string, RootScaleCache> m_rootScaleCaches = new();

        // 当前 Base 层的动画名称
        public string m_currentAnimationName;

        /// <summary>Base 层访问器（root motion 仅 Base 层参与）</summary>
        AnimationLayer BaseLayer => m_layers?.FirstOrDefault(layer => layer.Index == 0);
        /// <summary>当前 Base 层根运动配置（读点：ApplyRootMotion/HasRootMotion；写须走 SetRootMotionConfig）</summary>
        public RootMotionConfig m_currentRootMotionConfig => BaseLayer?.m_rootMotionConfig;

        /// <summary>
        /// 是否有活动的根运动配置
        /// </summary>
        public bool HasRootMotion => m_currentRootMotionConfig != null;
        public float m_prevRootMotionTime;

        // 缓存的父骨骼链旋转（将动画局部空间速度变换到模型空间）
        public Quaternion m_parentChainRotation = Quaternion.Identity;

        // 状态规则配置（从动画配置文件加载）
        public Dictionary<string, StateLayerConfig> m_stateConfigs;

        // 记录每个状态层当前匹配的规则索引（用于避免重复切换）
        public readonly Dictionary<string, int[]> m_lastMatchedRuleIndex = new();

        // 记录每个状态层当前规则解析后的 source 名（[param] 插值后）。path 相同时比较 source 名变 → 重切，
        // 支持 source: "[param]" 动态换动画（字面量 source 恒同 → 天然不重切）。
        readonly Dictionary<string, string> m_lastResolvedSource = new();

        // 动画引用配置（用于获取 OnComplete 动作）
        public Dictionary<string, AnimationReference> m_animationReferences = new();

        // 当前层播放的动画别名（用于查找 OnComplete 配置）
        public readonly Dictionary<string, string> m_layerAnimationAlias = new();

        // 记录层的动画播放状态（用于检测完成）
        public readonly Dictionary<string, bool> m_layerWasPlaying = new();

        // 记录层的循环设置（用于判断是否是非循环动画完成）
        public readonly Dictionary<string, bool> m_layerLooping = new();

        // 记录当前应用在每个层上的动画引用（用于获取 OnComplete）
        public readonly Dictionary<string, AnimationReference> m_layerAnimationRef = new();

        // 记录哪些层被手动控制（跳过状态规则评估）
        public readonly HashSet<string> m_manualOverrideLayers = new();

        // IK 求解器（延迟初始化）
        public IKSolver m_ikSolver;

        public Model Model => m_model;
        public AnimationTemplate Template => m_template;
        public AnimationParameters Parameters => m_parameters;
        public AnimationLayer[] Layers => m_layers;

        /// <summary>
        /// 共享的表达式求值器（供动画来源使用）
        /// </summary>
        public ExpressionEvaluator ExpressionEvaluator => m_expressionEvaluator;

        /// <summary>
        /// IK 求解器（延迟初始化）
        /// </summary>
        public IKSolver IKSolver {
            get {
                if (m_ikSolver == null) {
                    m_ikSolver = new IKSolver();
                }
                return m_ikSolver;
            }
        }

        /// <summary>
        /// 根骨骼旋转角度（弧度），用于修正模型朝向
        /// 某些 glTF 模型的前方方向可能与游戏不一致，需要旋转修正
        /// </summary>
        public float RootBoneRotation { get; set; } = 0f;

        // ===== 根骨骼变换（rotation Vector3 扩展 + translation + 每动画覆盖混合） =====
        // 顶层配置目标
        Quaternion? m_configRootRotation;
        Vector3 m_configRootTranslation;
        bool m_hasConfigTranslation;

        // 当前有效值（ComponentModel / root motion 读取）
        Quaternion m_effectiveRootRotation = Quaternion.Identity;
        Vector3 m_effectiveRootTranslation = Vector3.Zero;

        // 活动混合（from → to）
        Quaternion m_rootRotFrom = Quaternion.Identity;
        Quaternion m_rootRotTo = Quaternion.Identity;
        Vector3 m_rootTransFrom = Vector3.Zero;
        Vector3 m_rootTransTo = Vector3.Zero;
        float m_rootBlendElapsed;
        float m_rootBlendDuration;
        bool m_rootBlending;

        /// <summary>当前有效根骨骼旋转（已混合）。ComponentModel 与 root motion 读取。</summary>
        public Quaternion EffectiveRootRotation => m_effectiveRootRotation;
        /// <summary>当前有效根骨骼平移（已混合）。</summary>
        public Vector3 EffectiveRootTranslation => m_effectiveRootTranslation;

        /// <summary>
        /// 模型缩放比例
        /// </summary>
        public float ModelScale { get; set; } = 1f;

        /// <summary>
        /// 根骨骼名称（可通过配置指定，或自动检测）
        /// </summary>
        public string RootBoneName { get; set; } = "Root";

        /// <summary>
        /// 关联的速度向量（用于根运动应用）
        /// 设置后根运动会修改此向量的值
        /// </summary>
        public Vector3? Velocity { get; set; }

        /// <summary>
        /// 关联的旋转（用于根运动坐标转换）
        /// </summary>
        public Quaternion? EntityRotation { get; set; }

        /// <summary>
        /// 默认碰撞体尺寸
        /// </summary>
        public Vector3 DefaultCollisionSize {
            get => m_collisionBoxApplier.DefaultSize;
            set => m_collisionBoxApplier.DefaultSize = value;
        }

        /// <summary>
        /// 碰撞体尺寸设置回调（由外部提供）
        /// </summary>
        public Action<Vector3> SetCollisionBox { get; set; }

        /// <summary>
        /// 动画事件触发时调用
        /// </summary>
        public event AnimationEventHandler OnAnimationEvent;

        public AnimationController(Model model, string templateName) {
            m_model = model;
            m_template = AnimationTemplateManager.Get(templateName);
            if (m_template == null) {
                // 使用简单模板作为后备
                m_template = AnimationTemplateManager.Get("Simple");
            }

            if (model.RootBone != null) {
                RootBoneName = FindAnimatableRootBoneName(model);
            }

            // 初始化共享的表达式求值器
            m_expressionEvaluator = m_ruleEvaluator.Evaluator;

            // 初始化层（按 Index 排序，确保混合顺序正确）
            m_layers = new AnimationLayer[m_template.Layers.Count];
            int layerIndex = 0;
            foreach ((string name, LayerDefinition layerDef) in m_template.Layers.OrderBy(kvp => kvp.Value.Index)) {
                m_layers[layerIndex] = new AnimationLayer(name, layerDef.Index, layerDef.BlendMode, layerDef.BoneMask, layerDef.BoneMaskExclude);
                m_layers[layerIndex].Weight = layerDef.Weight; // 应用配置权重（默认 1）
                m_layers[layerIndex].m_configuredWeight = layerDef.Weight; // 同步配置权重（权重渐入/中断恢复目标）

                // 订阅层的动画事件（通过层的事件接口，统一处理主播放器和过渡播放器）
                m_layers[layerIndex].OnAnimationEvent += ForwardAnimationEvent;
                layerIndex++;
            }
        }

        /// <summary>
        /// 查找真正有动画数据的根骨骼名
        /// glTF 中 mesh 节点也会变成 bone，但动画 channel 通常目标是实际骨骼
        /// 从 RootBone 开始，向下搜索第一个在动画 channels 中出现的骨骼
        /// </summary>
        public static string FindAnimatableRootBoneName(Model model) {
            ModelBone root = model.RootBone;
            if (root == null) {
                return "Root";
            }
            // 收集所有动画中出现的目标骨骼名
            HashSet<string> animatedBones = new();
            foreach (ModelAnimation anim in model.Animations) {
                if (anim.Channels == null) continue;
                foreach (ModelAnimation.AnimationChannel ch in anim.Channels) {
                    if (ch.TargetBoneName != null) {
                        animatedBones.Add(ch.TargetBoneName);
                    }
                }
            }
            // 如果 RootBone 本身在动画中出现，直接使用
            if (animatedBones.Contains(root.Name)) {
                return root.Name;
            }
            // BFS 从 RootBone 子节点中找第一个在动画中出现的
            Queue<ModelBone> queue = new();
            foreach (ModelBone child in root.ChildBones) {
                queue.Enqueue(child);
            }
            while (queue.Count > 0) {
                ModelBone bone = queue.Dequeue();
                if (animatedBones.Contains(bone.Name)) {
                    return bone.Name;
                }
                foreach (ModelBone child in bone.ChildBones) {
                    queue.Enqueue(child);
                }
            }
            // 全部没找到，回退到 RootBone 名称
            return root.Name;
        }

        /// <summary>
        /// 更新控制器
        /// </summary>
        public void Update(float deltaTime) {
            // 1. 同步引擎参数
            SyncEngineParameters();

            // 2. 评估状态规则（仅当参数有变化时）
            if (m_parameters.IsDirty) {
                EvaluateStateRules();
                m_parameters.ClearDirty();
            }

            // 3. 更新动态属性（速度、循环状态等）
            UpdateDynamicProperties();

            // 4. 更新所有层
            foreach (AnimationLayer layer in m_layers) {
                layer.Update(deltaTime, m_parameters);
            }

            // 5. 推进根骨骼变换混合（须在根运动前，根运动读 EffectiveRootRotation）
            AdvanceRootBlend(deltaTime);

            // 6. 应用根运动（仅 Base 层）
            ApplyRootMotion(deltaTime);

            // 7. 检查动画完成事件
            CheckAnimationCompletion();
        }

        /// <summary>
        /// 应用根运动到物理体
        /// </summary>
        public void ApplyRootMotion(float deltaTime) {
            // 检查是否有根运动配置
            if (m_currentRootMotionConfig == null) {
                return;
            }

            // 只处理 Base 层（index 0）
            AnimationLayer baseLayer = m_layers.FirstOrDefault(l => l.Index == 0);
            if (baseLayer == null) {
                return;
            }
            AnimationPlayer player = baseLayer.AnimationPlayer;
            if (player == null) {
                return;
            }
            // 非循环动画已完成且非 Blend/Override 模式：直接返回
            // Blend/Override 模式需要继续调用 TranslationApplier 以减速到零
            TranslationConfig currentTransConfig = m_currentRootMotionConfig?.Translation;
            bool isBlendOrOverride = currentTransConfig != null
                && currentTransConfig.Mode != TranslationMode.None
                && currentTransConfig.Mode != TranslationMode.AddImpulse;
            if (!player.IsPlaying) {
                if (!isBlendOrOverride
                    || player.Loop
                    || player.NormalizedTime < 1.0f) {
                    return;
                }
            }
            ModelAnimation animation = player.Animation;
            if (animation == null) {
                return;
            }

            // 检查是否需要更新缓存
            string animName = animation.Name;
            bool animChanged = animName != m_currentAnimationName;
            // 有效的测量骨骼名：SourceBone 优先，否则用自动检测的 RootBoneName
            string effectiveBoneName = !string.IsNullOrEmpty(m_currentRootMotionConfig.SourceBone)
                ? m_currentRootMotionConfig.SourceBone
                : RootBoneName;
            if (animChanged) {
                m_currentAnimationName = animName;
                m_prevRootMotionTime = player.Time;

                // 计算目标骨骼到模型根的父骨骼链旋转
                // 动画数据在目标骨骼的父局部空间中，需要变换到模型空间
                m_parentChainRotation = ComputeParentChainRotation(effectiveBoneName);

                // 构建缓存
                if (!m_rootMotionCaches.TryGetValue(animName, out RootMotionCache motionCache)) {
                    motionCache = new RootMotionCache();
                    motionCache.BuildFromAnimation(animation, effectiveBoneName);
                    m_rootMotionCaches[animName] = motionCache;
                }
                if (!m_rootScaleCaches.TryGetValue(animName, out RootScaleCache scaleCache)) {
                    scaleCache = new RootScaleCache();
                    scaleCache.BuildFromAnimation(animation, effectiveBoneName);
                    m_rootScaleCaches[animName] = scaleCache;
                }
            }

            // 获取缓存
            if (!m_rootMotionCaches.TryGetValue(animName, out RootMotionCache rootMotionCache)) {
                return;
            }
            if (!m_rootScaleCaches.TryGetValue(animName, out RootScaleCache rootScaleCache)) {
                rootScaleCache = null;
            }
            float currentTime = player.Time;
            float duration = animation.Duration;

            // 非循环动画已完成：AddImpulse 模式直接返回
            // Blend/Override 模式仍需调用 TranslationApplier 以减速到零
            if (!player.Loop
                && !player.IsPlaying
                && player.NormalizedTime >= 1.0f) {
                TranslationConfig completedConfig = m_currentRootMotionConfig?.Translation;
                if (completedConfig == null
                    || completedConfig.Mode == TranslationMode.AddImpulse
                    || completedConfig.Mode == TranslationMode.None) {
                    return;
                }
                // Blend/Override: 用零速度继续调用，让 SmoothDamp 减速
                Vector3 vel = Velocity ?? Vector3.Zero;
                Quaternion rotation = EntityRotation ?? Quaternion.Identity;
                m_translationApplier.ApplyTranslation(completedConfig, Vector3.Zero, null, rotation, ref vel, deltaTime);
                Velocity = vel;
                return;
            }
            RootMotionConfig rootMotionConfig = m_currentRootMotionConfig;
            Vector3 velocity = Vector3.Zero;
            Vector3? impulse = null;
            TranslationConfig translationConfig = rootMotionConfig.Translation;
            if (translationConfig.Mode != TranslationMode.None) {
                if (translationConfig.Mode == TranslationMode.AddImpulse) {
                    // ImpulseOverride / ImpulseSpeedOverride 不依赖动画位移数据
                    bool hasData = translationConfig.ImpulseOverride.HasValue
                        || translationConfig.ImpulseSpeedOverride.HasValue
                        || rootMotionCache.HasTranslationData;
                    if (hasData) {
                        // ImpulsePhase: 绝对动画相位，-1 表示自动（正播用 endPhase，反播用 startPhase）
                        float startPhase = player.StartPhase;
                        float endPhase = player.EndPhase;
                        bool forward = endPhase >= startPhase;
                        float impulsePhase = translationConfig.ImpulsePhase < 0
                            ? (forward ? endPhase : startPhase)
                            : translationConfig.ImpulsePhase;

                        float prevNorm = duration > 0 ? m_prevRootMotionTime / duration : 0f;
                        float currNorm = duration > 0 ? currentTime / duration : 0f;
                        bool crossed = false;

                        if (animChanged) {
                            if (forward && currNorm >= impulsePhase) {
                                crossed = true;
                            }
                            else if (!forward && currNorm <= impulsePhase) {
                                crossed = true;
                            }
                        }
                        else if (forward) {
                            if (prevNorm < impulsePhase && currNorm >= impulsePhase) {
                                crossed = true;
                            }
                            else if (player.Loop && currNorm < prevNorm) {
                                crossed = prevNorm < impulsePhase || currNorm >= impulsePhase;
                            }
                        }
                        else {
                            if (prevNorm > impulsePhase && currNorm <= impulsePhase) {
                                crossed = true;
                            }
                            else if (player.Loop && currNorm > prevNorm) {
                                crossed = prevNorm > impulsePhase || currNorm <= impulsePhase;
                            }
                        }

                        if (crossed) {
                            impulse = CalculateRootMotionImpulse(translationConfig, rootMotionCache, startPhase, endPhase, impulsePhase);
                        }
                    }
                }
                else if (rootMotionCache.HasTranslationData) {
                    velocity = rootMotionCache.GetVelocity(m_prevRootMotionTime, currentTime);
                    // 动画局部空间 → 模型空间 → 实体空间
                    velocity = Vector3.Transform(velocity, m_parentChainRotation);
                    velocity *= ModelScale;
                    velocity = Vector3.Transform(velocity, m_effectiveRootRotation);
                }
            }
            Vector3? scale = null;
            if (rootMotionConfig.Scale.Mode != ScaleMode.None
                && rootMotionConfig.Scale.Source == ScaleSource.Animation
                && rootScaleCache?.HasScaleData == true) {
                float normalizedTime = duration > 0 ? currentTime / duration : 0;
                scale = rootScaleCache.SampleScale(normalizedTime);
            }
            m_prevRootMotionTime = currentTime;

            // 应用位移
            // AddImpulse: 仅在有冲量时应用
            // Blend/Override: 始终应用（包括零速度，确保 SmoothDamp 能减速）
            bool shouldApply = translationConfig.Mode != TranslationMode.None
                && Velocity.HasValue;
            if (translationConfig.Mode == TranslationMode.AddImpulse) {
                shouldApply = shouldApply && impulse.HasValue;
            }
            else {
                shouldApply = shouldApply && rootMotionCache.HasTranslationData;
            }
            if (shouldApply) {
                Vector3 vel = Velocity.Value;
                Quaternion rotation = EntityRotation ?? Quaternion.Identity;
                m_translationApplier.ApplyTranslation(translationConfig, velocity, impulse, rotation, ref vel, deltaTime);
                Velocity = vel;
            }

            // 应用缩放
            if (rootMotionConfig.Scale.Mode != ScaleMode.None
                && SetCollisionBox != null) {
                m_collisionBoxApplier.ApplyScale(rootMotionConfig.Scale, scale, SetCollisionBox, deltaTime);
            }
        }

        /// <summary>
        /// 计算根运动冲量
        /// </summary>
        public Vector3 CalculateRootMotionImpulse(TranslationConfig config, RootMotionCache cache,
            float startPhase, float endPhase, float impulsePhase) {
            // 优先使用配置覆盖值（已在实体空间，不需要变换）
            if (config.ImpulseOverride.HasValue) {
                return config.ImpulseOverride.Value;
            }
            // 冲量速度向量：body-local，由 TranslationApplier 经 body.Rotation 转世界
            if (config.ImpulseSpeedOverride.HasValue) {
                return config.ImpulseSpeedOverride.Value;
            }
            // 从动画数据计算：使用 impulsePhase → endPhase 范围
            float measureStart = impulsePhase > startPhase ? impulsePhase : startPhase;
            Vector3 localImpulse = config.ImpulseMethod switch {
                ImpulseMethod.Peak => cache.GetPeakVelocity(measureStart, endPhase),
                ImpulseMethod.Weighted => (cache.GetAverageVelocity(measureStart, endPhase) + cache.GetPeakVelocity(measureStart, endPhase)) * 0.5f,
                _ => cache.GetAverageVelocity(measureStart, endPhase)
            };
            // 动画局部空间 → 模型空间 → 实体空间
            localImpulse = Vector3.Transform(localImpulse, m_parentChainRotation);
            localImpulse *= ModelScale;
            localImpulse = Vector3.Transform(localImpulse, m_effectiveRootRotation);
            return localImpulse;
        }

        /// <summary>
        /// 计算目标骨骼到模型根的父骨骼链旋转
        /// 动画 translation 数据在目标骨骼的父局部空间中，
        /// 需要沿父链累积旋转才能变换到模型根空间
        /// </summary>
        public Quaternion ComputeParentChainRotation(string boneName) {
            ModelBone bone = m_model.FindBone(boneName, false);
            if (bone?.ParentBone == null) {
                return Quaternion.Identity;
            }
            Quaternion accumulated = Quaternion.Identity;
            ModelBone current = bone.ParentBone;
            while (current != null) {
                current.Transform.Decompose(out _, out Quaternion rot, out _);
                accumulated = rot * accumulated;
                current = current.ParentBone;
            }
            return accumulated;
        }

        /// <summary>
        /// 设置当前根运动配置
        /// </summary>
        public void SetRootMotionConfig(RootMotionConfig config) {
            if (BaseLayer != null) {
                BaseLayer.SetRootMotionConfig(config, m_model, RootBoneName);
            }
            m_currentAnimationName = null; // 重置动画名称，触发缓存更新
            m_parentChainRotation = Quaternion.Identity;
            m_translationApplier.Reset();
        }

        /// <summary>
        /// 设置顶层根骨骼旋转配置（度）。同步更新 legacy float RootBoneRotation（Y 弧度，向后兼容）。
        /// </summary>
        public void SetRootRotationConfig(Vector3? eulerDegrees) {
            m_configRootRotation = eulerDegrees.HasValue
                ? EulerDegreesToQuaternion(eulerDegrees.Value)
                : (Quaternion?)null;
            RootBoneRotation = eulerDegrees.HasValue
                ? eulerDegrees.Value.Y * MathF.PI / 180f
                : 0f;
        }

        /// <summary>
        /// 设置顶层根骨骼平移配置。
        /// </summary>
        public void SetRootTranslationConfig(Vector3? translation) {
            m_hasConfigTranslation = translation.HasValue;
            m_configRootTranslation = translation ?? Vector3.Zero;
        }

        /// <summary>
        /// 将 effective snap 到顶层配置目标（CreateController 初始化用，避免启动混合）。
        /// </summary>
        public void SnapRootTransformToConfig() {
            m_effectiveRootRotation = m_configRootRotation ?? Quaternion.Identity;
            m_effectiveRootTranslation = m_hasConfigTranslation ? m_configRootTranslation : Vector3.Zero;
            m_rootBlending = false;
            m_rootBlendElapsed = 0f;
        }

        /// <summary>
        /// 解析动画引用的根骨骼变换目标并启动混合。
        /// 由 ApplyAnimationToLayer 在合并 caller+alias 后调用。
        /// </summary>
        public void SetRootTransformTargetFromAnimation(AnimationReference animRef, float blendDuration) {
            Quaternion rotTarget = ResolveRotationTarget(animRef);
            Vector3 transTarget = ResolveTranslationTarget(animRef);
            SetRootTransformTarget(rotTarget, transTarget, blendDuration);
        }

        /// <summary>
        /// 启动根骨骼变换混合（从当前有效值出发，中断安全）。duration≤0 立即 snap。
        /// </summary>
        public void SetRootTransformTarget(Quaternion rotation, Vector3 translation, float duration) {
            m_rootRotFrom = m_effectiveRootRotation;
            m_rootRotTo = rotation;
            m_rootTransFrom = m_effectiveRootTranslation;
            m_rootTransTo = translation;
            if (duration <= 0f) {
                m_effectiveRootRotation = rotation;
                m_effectiveRootTranslation = translation;
                m_rootBlending = false;
                m_rootBlendElapsed = 0f;
                m_rootBlendDuration = 0f;
                return;
            }
            m_rootBlendDuration = duration;
            m_rootBlendElapsed = 0f;
            m_rootBlending = true;
        }

        /// <summary>
        /// 推进根骨骼变换混合。Update 每帧调用。
        /// </summary>
        public void AdvanceRootBlend(float deltaTime) {
            if (!m_rootBlending) {
                return;
            }
            m_rootBlendElapsed += deltaTime;
            float raw = m_rootBlendDuration > 0f
                ? m_rootBlendElapsed / m_rootBlendDuration
                : 1f;
            float t = Math.Clamp(raw, 0f, 1f);
            float s = t * t * (3f - 2f * t); // smoothstep
            m_effectiveRootRotation = Quaternion.Slerp(m_rootRotFrom, m_rootRotTo, s);
            m_effectiveRootTranslation = Vector3.Lerp(m_rootTransFrom, m_rootTransTo, s);
            if (t >= 1f) {
                m_effectiveRootRotation = m_rootRotTo;
                m_effectiveRootTranslation = m_rootTransTo;
                m_rootBlending = false;
            }
        }

        Quaternion ResolveRotationTarget(AnimationReference animRef) {
            if (animRef != null && animRef.HasRootBoneRotation) {
                return RootRotationValueToQuaternion(animRef.RootBoneRotationValue);
            }
            return m_configRootRotation ?? Quaternion.Identity;
        }

        Vector3 ResolveTranslationTarget(AnimationReference animRef) {
            if (animRef != null
                && animRef.HasRootBoneTranslation
                && animRef.RootBoneTranslation.HasValue) {
                return animRef.RootBoneTranslation.Value;
            }
            return m_hasConfigTranslation ? m_configRootTranslation : Vector3.Zero;
        }

        static Quaternion EulerDegreesToQuaternion(Vector3 eulerDegrees) {
            float yaw = eulerDegrees.Y * MathF.PI / 180f;
            float pitch = eulerDegrees.X * MathF.PI / 180f;
            float roll = eulerDegrees.Z * MathF.PI / 180f;
            return Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
        }

        static Quaternion RootRotationValueToQuaternion(object value) {
            switch (value) {
                case float f: return EulerDegreesToQuaternion(new Vector3(0f, f, 0f));
                case int i: return EulerDegreesToQuaternion(new Vector3(0f, i, 0f));
                case double d: return EulerDegreesToQuaternion(new Vector3(0f, (float)d, 0f));
                case Vector3 v: return EulerDegreesToQuaternion(v);
                default: return Quaternion.Identity;
            }
        }

        /// <summary>
        /// 更新动态属性（每帧评估表达式）
        /// </summary>
        public void UpdateDynamicProperties() {
            foreach (AnimationLayer layer in m_layers) {
                string layerName = layer.Name;

                // 检查是否有该层的动画引用
                if (!m_layerAnimationRef.TryGetValue(layerName, out AnimationReference animRef)
                    || animRef == null) {
                    continue;
                }
                AnimationPlayer player = layer.AnimationPlayer;
                if (player == null) {
                    continue;
                }

                // 动态速度
                DynamicProperty<float> speedProp = animRef.GetSpeedProperty();
                if (speedProp.IsExpression) {
                    float speed = speedProp.GetValue(m_parameters, m_expressionEvaluator);
                    player.Speed = speed;
                }

                // 动态循环状态
                DynamicProperty<bool> loopProp = animRef.GetLoopProperty();
                if (loopProp.IsExpression) {
                    bool loop = loopProp.GetValue(m_parameters, m_expressionEvaluator);
                    player.Loop = loop;
                    m_layerLooping[layerName] = loop;
                }
            }
        }

        /// <summary>
        /// 检查动画完成事件
        /// </summary>
        public void CheckAnimationCompletion() {
            foreach (AnimationLayer layer in m_layers) {
                string layerName = layer.Name;
                AnimationPlayer player = layer.AnimationPlayer;

                // 获取当前播放状态
                bool isPlaying = player?.IsPlaying ?? false;
                bool wasPlaying = m_layerWasPlaying.GetValueOrDefault(layerName, false);
                bool isLooping = m_layerLooping.GetValueOrDefault(layerName, true);

                // 过渡/权重渐入期不判完成：blend 期层未真正接管目标动画，
                // endPhase≤startPhase 的姿势动画（如 attacked 仅取首帧后倾）目标 player 首帧即 IsPlaying=false，
                // 若不跳过会在此 blend 期误触发 OnComplete，动画刚切入就被切回。
                // 正常非循环动画（jump/climbup/wakeup/pickup）过渡/渐入期 IsPlaying=true，跳过无副作用；
                // 仅姿势动画受益——blend 完成后才判完成。
                bool blending = layer.Transition != null || layer.m_activating;

                // 检测非循环动画完成：之前在播放，现在停止了，且不是循环动画
                if (!blending
                    && wasPlaying
                    && !isPlaying
                    && !isLooping) {
                    // 动画完成，执行 OnComplete 动作
                    if (m_layerAnimationRef.TryGetValue(layerName, out AnimationReference animRef)
                        && animRef?.OnComplete != null) {
                        ExecuteOnCompleteAction(animRef.OnComplete);
                    }
                }

                // 更新播放状态记录（blend 期保持 wasPlaying=true，让 blend 完成后下帧仍能检测到完成）
                if (!blending) {
                    m_layerWasPlaying[layerName] = isPlaying;
                }
            }
        }

        /// <summary>
        /// 检测层的当前动画是否被打断（非循环动画仍在播放即被切换/停用），是则触发其 OnInterrupt trigger。
        /// 在 ApplyAnimationToLayer 开头（切到新动画前）和 EvaluateStateRules 停用分支（DeactivateAndClear 前）调用。
        /// 判据与 CheckAnimationCompletion 的完成判据互补：
        /// - 完成（OnComplete）：wasPlaying 且 !isPlaying 且 !isLooping（自然播到末尾停止）
        /// - 打断（OnInterrupt）：wasPlaying 且 isPlaying 且 !isLooping（仍在播放即被切走，未到末尾）
        /// 两者互斥：自然播完 isPlaying=false 走完成；被切走 isPlaying=true 走打断。
        /// 复用 ExecuteOnCompleteAction 触发 trigger（type=trigger 语义通用）。
        /// </summary>
        void TriggerOnInterruptIfActive(AnimationLayer layer, string layerName) {
            if (layer == null) {
                return;
            }
            if (!m_layerAnimationRef.TryGetValue(layerName, out AnimationReference oldRef)
                || oldRef?.OnInterrupt == null) {
                return;
            }
            bool isLooping = m_layerLooping.GetValueOrDefault(layerName, true);
            bool wasPlaying = m_layerWasPlaying.GetValueOrDefault(layerName, false);
            bool isPlaying = layer.AnimationPlayer?.IsPlaying ?? false;
            if (wasPlaying && isPlaying && !isLooping) {
                ExecuteOnCompleteAction(oldRef.OnInterrupt);
            }
        }

        /// <summary>
        /// 执行动画完成动作
        /// </summary>
        public void ExecuteOnCompleteAction(OnCompleteAction action) {
            if (action == null) {
                return;
            }
            switch (action.Type?.ToLowerInvariant()) {
                case "trigger":
                    // 触发自定义事件
                    if (!string.IsNullOrEmpty(action.Name)) {
                        AnimationEvent evt = new(action.Name, 0f, action.Data);
                        OnAnimationEvent?.Invoke(evt);
                    }
                    break;
            }
        }

        /// <summary>
        /// 设置动画引用配置（用于 OnComplete 回调查找）
        /// </summary>
        public void SetAnimationReferences(Dictionary<string, AnimationReference> references) {
            m_animationReferences = references ?? new Dictionary<string, AnimationReference>();
        }

        /// <summary>
        /// 设置状态规则配置
        /// </summary>
        public void SetStateConfigs(Dictionary<string, StateLayerConfig> configs) {
            m_stateConfigs = configs;
        }

        /// <summary>
        /// 评估状态规则，根据条件自动切换动画
        /// </summary>
        public void EvaluateStateRules() {
            if (m_stateConfigs == null) {
                return;
            }

            // 如果所有层都被手动控制，跳过评估
            if (m_manualOverrideLayers.Count > 0
                && m_manualOverrideLayers.Count >= m_layers.Length) {
                return;
            }
            foreach ((string stateName, StateLayerConfig layerConfig) in m_stateConfigs) {
                if (string.IsNullOrEmpty(layerConfig.Layer)) {
                    continue;
                }
                if (!m_layers.Any(l => l.Name == layerConfig.Layer)) {
                    continue;
                }

                // 跳过被手动控制的层
                if (m_manualOverrideLayers.Contains(layerConfig.Layer)) {
                    continue;
                }
                if (layerConfig.Rules == null
                    || layerConfig.Rules.Count == 0) {
                    continue;
                }

                // 递归匹配规则（支持嵌套 rules 决策树），path 累积匹配索引路径
                List<int> path = new();
                if (!TryMatchRule(layerConfig.Rules, path, out StateRuleConfig matchedRule)) {
                    continue; // 无匹配，保持当前状态
                }

                // path 变化检测（路径 = 外层 idx → 内层 idx → ...）
                bool pathChanged = !m_lastMatchedRuleIndex.TryGetValue(stateName, out int[] lastPath)
                                || lastPath == null || !lastPath.SequenceEqual(path);
                // source 名变化检测（支持 source: "[param]" 动态换动画）：path 相同时比较解析后 source 名。
                // 字面量 source 恒同 → 不重切；[param] source 随参数值变 → 重切播新动画。
                string resolvedSource = ResolveSource(matchedRule.Animation) ?? string.Empty;
                if (!pathChanged) {
                    if (m_lastResolvedSource.TryGetValue(stateName, out string lastSrc)
                        && lastSrc == resolvedSource) {
                        continue;  // path + source 都同 → 跳过
                    }
                }
                else {
                    m_lastMatchedRuleIndex[stateName] = path.ToArray();
                }
                m_lastResolvedSource[stateName] = resolvedSource;

                // 切换动画。
                // Source 为空（含 {"source":null} 与整个 animation:null）= 该层无内容 → 停用，让下层输出可见。
                // 不能直接调 ApplyAnimationToLayer：它对空 Source early-return（944-947），既不播新也不停旧，
                // 旧动画会滞留（条件不再满足时该层动画继续播放，无法停止）。
                if (matchedRule.Animation != null
                    && !string.IsNullOrEmpty(matchedRule.Animation.Source)) {
                    ApplyAnimationToLayer(layerConfig.Layer, matchedRule.Animation);
                }
                else {
                    // animation:null 或 source:null → 停用层。
                    AnimationLayer layer = m_layers.FirstOrDefault(l => l.Name == layerConfig.Layer);
                    if (layer != null) {
                        // 停用前：若旧动画是非循环且仍在播放，视为被打断 → 触发 OnInterrupt。
                        TriggerOnInterruptIfActive(layer, layerConfig.Layer);
                        // 清该层动画跟踪：DeactivateAndClear 渐降期保留采样（IsPlaying 仍 true），渐降完 ClearAnimation
                        // 使 IsPlaying=false，此时 CheckAnimationCompletion 读残留 oldRef（wasPlaying=true、!isLooping）
                        // 会误触发 OnComplete——与上面 OnInterrupt 冲突破坏互斥；未配 OnInterrupt 的非循环动画
                        // 被停用同样误触发。重置为"无活跃非循环动画"态：OnComplete 三判据全失效。
                        m_layerAnimationRef[layerConfig.Layer] = null;
                        m_layerLooping[layerConfig.Layer] = true;
                        m_layerWasPlaying[layerConfig.Layer] = false;
                        // driver 层（如鱼 Head/FishAttack）用旧 Deactivate：保留 Weight、置 m_active=false。
                        // driver 再激活走 Activate()+SetDriver()，不走 PlayAnimationWithTransition 权重渐入，
                        // 不会恢复 Weight。若改用 DeactivateAndClear（清 Weight=0 + 保持 active），driver 层会卡在
                        // Weight=0：AnimationBlender 首骨骼在 Weight<1 分支写入 rest（非 null），非蒙皮模型
                        // ProcessBoneHierarchy 的旋转叠加分支会损坏该骨骼（鱼下颚前移错位），且咬合动画不可见。
                        // 片段层（无 driver）保留 DeactivateAndClear，修复其重启硬切（见该提交说明）。
                        if (layer.Driver != null) {
                            layer.Deactivate();
                        }
                        else {
                            layer.DeactivateAndClear(0.2f);
                        }
                    }

                    // Base 层停用时清除根运动配置，根骨骼变换回退顶层
                    if (layerConfig.Layer == "Base") {
                        SetRootMotionConfig(null);
                        SetRootTransformTarget(
                            m_configRootRotation ?? Quaternion.Identity,
                            m_configRootTranslation,
                            0.2f);
                    }
                }
            }
        }

        /// <summary>
        /// 递归匹配状态规则（支持嵌套 rules 决策树）。
        /// 分组节点（Rules 非空）匹配 Condition 后递归子 rules；叶子节点（Rules 空）直接返回。
        /// 外层 Condition 失败时短路整组（不评估子 rules）。
        /// path 累积匹配索引路径（外层→内层），用于变化检测触发 transition。
        /// </summary>
        bool TryMatchRule(List<StateRuleConfig> rules, List<int> path, out StateRuleConfig leaf) {
            leaf = null;
            for (int i = 0; i < rules.Count; i++) {
                StateRuleConfig rule = rules[i];
                bool result = !string.IsNullOrEmpty(rule.Condition)
                    && (rule.Condition == "true"
                        || m_ruleEvaluator.EvaluateCondition(rule.Condition, m_parameters));
                if (!result) {
                    continue;
                }
                path.Add(i);
                if (rule.HasRules) {
                    // 分组：递归子 rules。子全无匹配则回溯 path，继续同层下一 rule。
                    if (!TryMatchRule(rule.Rules, path, out leaf)) {
                        path.RemoveAt(path.Count - 1);
                        continue;
                    }
                }
                else {
                    leaf = rule;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 将 caller（状态规则/手动 API）的动画引用与别名配置合并。
        /// 语义：alias = 模板默认，caller = 覆盖层（只覆盖显式设置的字段）；
        /// Source 永远取 alias（alias 才持有真实动画名）。
        /// 动态字段（speed/loop/phase/blendDuration/preservePose）按 HasXxx 判断 caller 是否显式设置；
        /// 引用类型字段（driverArgs/events/onComplete/rootMotion）按 null 判断。
        /// </summary>
        private static AnimationReference MergeWithAlias(AnimationReference caller, AnimationReference alias) {
            return new AnimationReference {
                Source = alias.Source,
                SpeedValue = caller.HasSpeed ? caller.SpeedValue : alias.SpeedValue,
                LoopValue = caller.HasLoop ? caller.LoopValue : alias.LoopValue,
                StartPhaseValue = caller.HasStartPhase ? caller.StartPhaseValue : alias.StartPhaseValue,
                EndPhaseValue = caller.HasEndPhase ? caller.EndPhaseValue : alias.EndPhaseValue,
                BlendDurationValue = caller.HasBlendDuration ? caller.BlendDurationValue : alias.BlendDurationValue,
                PreservePose = caller.HasPreservePose ? caller.PreservePose : alias.PreservePose,
                DriverArgs = caller.DriverArgs ?? alias.DriverArgs,
                Events = caller.Events ?? alias.Events,
                OnComplete = caller.OnComplete ?? alias.OnComplete,
                OnInterrupt = caller.OnInterrupt ?? alias.OnInterrupt,
                RootBoneRotationValue = caller.HasRootBoneRotation ? caller.RootBoneRotationValue : alias.RootBoneRotationValue,
                RootBoneTranslation = caller.HasRootBoneTranslation ? caller.RootBoneTranslation : alias.RootBoneTranslation,
                HasRootBoneRotation = caller.HasRootBoneRotation || alias.HasRootBoneRotation,
                HasRootBoneTranslation = caller.HasRootBoneTranslation || alias.HasRootBoneTranslation,
                RootMotion = caller.RootMotion ?? alias.RootMotion
            };
        }

        /// <summary>
        /// 按名称精确查找模型动画（大小写不敏感）。
        /// 不做模糊包含——短名误命中太坑（如 "Idle_Loop" 命中 "Crouch_Idle_Loop"）。
        /// </summary>
        private ModelAnimation FindAnimation(string name) {
            if (string.IsNullOrEmpty(name)) {
                return null;
            }
            foreach (ModelAnimation a in m_model.Animations) {
                if (a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                    return a;
                }
            }
            return null;
        }

        /// <summary>
        /// 解析动画 source：source 形如 "[paramName]" 时从 Parameters 取 string 值（配置方须保证该 param 为 string），
        /// 支持规则 source 由参数动态驱动（如 "[RandomIdleEvent]"）；否则原样返回字面量。纯字符串取值，不走表达式计算。
        /// </summary>
        string ResolveSource(AnimationReference animRef) {
            string source = animRef?.Source;
            if (!string.IsNullOrEmpty(source) && source.Length > 2
                && source[0] == '[' && source[source.Length - 1] == ']') {
                return m_parameters.GetString(source.Substring(1, source.Length - 2));
            }
            return source;
        }

        /// <summary>
        /// 应用动画配置到指定层
        /// </summary>
        /// <returns>是否成功应用动画</returns>
        public bool ApplyAnimationToLayer(string layerName, AnimationReference animRef) {
            AnimationLayer layer = m_layers.FirstOrDefault(l => l.Name == layerName);
            if (layer == null) {
                return false;
            }
            // 切到新动画前：若旧动画是非循环且仍在播放（未自然播完），视为被打断 → 触发其 OnInterrupt。
            // 须在下方覆盖 m_layerAnimationRef[layerName] = 新 animRef 之前检测。
            TriggerOnInterruptIfActive(layer, layerName);
            string source = ResolveSource(animRef);
            // source 解析为空（含 {"source":null}、整个 animation:null、"[param]" 参数未注册或被设空）：
            // early-return false —— 不播新动画也不停旧，该层维持上一帧输出。设计：source 空=该规则"无内容"，
            // 参数空（如 [RandomIdleEvent] 事件未激活）= 维持现状而非停用。停用语义由 EvaluateStateRules 的空 Source
            // 分支（DeactivateAndClear）处理，不走这里。
            if (string.IsNullOrEmpty(source)) {
                return false;
            }

            // 检查 source 是否是动画别名（在 animations 部分定义）
            if (m_animationReferences.TryGetValue(source, out AnimationReference aliasRef)) {
                // alias 作为模板默认，caller（状态规则）显式设置的字段覆盖 alias。
                // 修复：之前整体用 alias 替换 caller，导致 caller 的 speed 表达式等动态属性丢失。
                animRef = MergeWithAlias(animRef, aliasRef);
                source = animRef.Source;
            }

            // 获取动态属性值（初始静态值）
            float speed = animRef.GetSpeedProperty().IsExpression ? 1.0f : animRef.GetSpeedProperty().StaticValue;
            bool loop = animRef.GetLoopProperty().IsExpression ? true : animRef.GetLoopProperty().StaticValue;
            float blendDuration = animRef.GetBlendDurationProperty().IsExpression ? 0.3f : animRef.GetBlendDurationProperty().StaticValue;

            // 解析根骨骼变换目标（每动画覆盖 → 顶层回退），启动混合（时长=blendDuration，与姿态过渡同步）
            SetRootTransformTargetFromAnimation(animRef, blendDuration);

            // 处理 driver: 语法
            if (source.StartsWith("driver:")) {
                string driverType = source.Substring(7);

                // 激活层
                layer.Activate();
                // 恢复 Weight 到配置权重 m_configuredWeight（与渐入/中断恢复目标一致，非硬编码 1）。
                // 被状态规则停用过的层（Death/Activity，活著时 condition:true→animation:null 停用清零）
                // 切回 driver 时若不恢复，Weight 残留 0 → 按 0 混合动画不显示。
                layer.Weight = layer.m_configuredWeight;

                // 检查层是否有预配置的驱动器，且类型匹配
                if (layer.Driver != null
                    && IsDriverTypeMatch(layer.Driver, driverType)) {
                    // 驱动器已存在且类型匹配，只更新运行时参数（通过 driverArgs）
                    if (animRef.DriverArgs != null) {
                        foreach (KeyValuePair<string, object> kvp in animRef.DriverArgs) {
                            m_parameters.SetParameter(kvp.Key, kvp.Value);
                        }
                    }
                }
                else {
                    // 没有预配置驱动器或类型不匹配，创建新驱动器
                    // 注意：这会覆盖预配置的驱动器
                    IAnimationDriver driver = CreateDriverFromConfig(driverType, animRef.DriverArgs);
                    if (driver != null) {
                        layer.SetDriver(driver);
                    }
                    else {
                        // 驱动器创建失败
                        return false;
                    }
                }

                // 驱动器相关参数通过 Parameters 传递
                if (speed != 1f) {
                    m_parameters.SetParameter("Speed", speed);
                }

                // 驱动器没有完成概念，清除跟踪
                m_layerAnimationRef.Remove(layerName);
                m_layerLooping.Remove(layerName);
                return true;
            }
            // 处理 file: 语法 - 加载外部动画文件
            if (source.StartsWith("file:")) {
                ModelAnimation animation = LoadExternalAnimation(source);
                if (animation != null) {
                    // 创建动画配置
                    AnimationSourceConfig sourceConfig = new() {
                        Source = source,
                        SpeedValue = animRef.SpeedValue,
                        LoopValue = animRef.LoopValue,
                        StartPhaseValue = animRef.StartPhaseValue,
                        EndPhaseValue = animRef.EndPhaseValue,
                        BlendDurationValue = animRef.BlendDurationValue
                    };

                    // 根据是否有过渡时长选择播放方式
                    if (blendDuration > 0f) {
                        layer.PlayAnimationWithTransition(m_model, animation, loop, blendDuration);
                    }
                    else {
                        layer.PlayAnimation(m_model, animation, loop);
                    }

                    // 设置播放速度
                    layer.AnimationPlayer.Speed = speed;

                    // 应用相位范围（静态值；表达式由 ClipAnimationSource 处理）
                    float startPhase = animRef.GetStartPhaseProperty().IsExpression ? 0f : animRef.GetStartPhaseProperty().StaticValue;
                    float endPhase = animRef.GetEndPhaseProperty().IsExpression ? 0f : animRef.GetEndPhaseProperty().StaticValue;
                    layer.AnimationPlayer.StartPhase = startPhase;
                    layer.AnimationPlayer.EndPhase = endPhase;

                    // 应用 PreservePose
                    layer.AnimationPlayer.PreservePose = animRef.PreservePose;

                    // 添加动画事件
                    ApplyAnimationEvents(layer.AnimationPlayer, animation, animRef.Events);

                    // 记录动画引用和循环设置（用于 OnComplete）
                    m_layerAnimationRef[layerName] = animRef;
                    m_layerLooping[layerName] = loop;
                    m_layerWasPlaying[layerName] = true;

                    // Base 层设置根运动配置
                    if (layerName == "Base") {
                        SetRootMotionConfig(animRef.RootMotion);
                    }
                    return true;
                }

                // 外部文件加载失败
                return false;
            }
            // 处理动画名（模型内置动画）
            else {
                ModelAnimation animation = FindAnimation(source);
                if (animation == null) {
                    return false;
                }

                // 根据是否有过渡时长选择播放方式
                if (blendDuration > 0f) {
                    // 使用过渡播放
                    layer.PlayAnimationWithTransition(m_model, animation, loop, blendDuration);
                }
                else {
                    // 立即播放
                    layer.PlayAnimation(m_model, animation, loop);
                }

                // 设置播放速度
                layer.AnimationPlayer.Speed = speed;

                // 应用相位范围（静态值；表达式由 ClipAnimationSource 处理）
                float startPhase = animRef.GetStartPhaseProperty().IsExpression ? 0f : animRef.GetStartPhaseProperty().StaticValue;
                float endPhase = animRef.GetEndPhaseProperty().IsExpression ? 0f : animRef.GetEndPhaseProperty().StaticValue;
                layer.AnimationPlayer.StartPhase = startPhase;
                layer.AnimationPlayer.EndPhase = endPhase;

                // 应用 PreservePose
                layer.AnimationPlayer.PreservePose = animRef.PreservePose;

                // 添加动画事件
                ApplyAnimationEvents(layer.AnimationPlayer, animation, animRef.Events);

                // 记录动画引用和循环设置（用于 OnComplete）
                m_layerAnimationRef[layerName] = animRef;
                m_layerLooping[layerName] = loop;
                m_layerWasPlaying[layerName] = true;

                // Base 层设置根运动配置
                if (layerName == "Base") {
                    SetRootMotionConfig(animRef.RootMotion);
                }
                return true;
            }
        }

        /// <summary>
        /// 应用动画事件到播放器
        /// </summary>
        /// <param name="player">动画播放器</param>
        /// <param name="animation">动画（未使用，保留参数兼容性）</param>
        /// <param name="events">事件配置列表（时间使用归一化时间 0-1）</param>
        public void ApplyAnimationEvents(AnimationPlayer player, ModelAnimation animation, List<AnimationEventConfig> events) {
            // 清除旧事件
            player.ClearEvents();

            // 添加新事件（时间直接使用归一化时间）
            if (events != null) {
                foreach (AnimationEventConfig evt in events) {
                    player.AddEvent(evt.Name, evt.Time, evt.Data);
                }
            }
        }

        /// <summary>
        /// 根据配置创建驱动器
        /// </summary>
        public IAnimationDriver CreateDriverFromConfig(string driverType, Dictionary<string, object> args) {
            IAnimationDriver driver = m_configLoader.CreateDriver(driverType);
            if (driver != null
                && args != null) {
                m_configLoader.ApplyDriverProperties(driver, args);
            }
            return driver;
        }

        /// <summary>
        /// 检查驱动器类型是否匹配
        /// 支持多种匹配方式：完整名称、简短名称、带/不带 Driver 后缀
        /// </summary>
        public static bool IsDriverTypeMatch(IAnimationDriver driver, string requestedType) {
            if (driver == null
                || string.IsNullOrEmpty(requestedType)) {
                return false;
            }
            string driverName = driver.Name;

            // 精确匹配
            if (string.Equals(driverName, requestedType, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            // 尝试添加/移除 Driver 后缀
            string requestedWithDriver = requestedType.EndsWith("Driver", StringComparison.OrdinalIgnoreCase)
                ? requestedType
                : requestedType + "Driver";
            string requestedWithoutDriver = requestedType.EndsWith("Driver", StringComparison.OrdinalIgnoreCase)
                ? requestedType.Substring(0, requestedType.Length - 6)
                : requestedType;
            if (string.Equals(driverName, requestedWithDriver, StringComparison.OrdinalIgnoreCase)
                || string.Equals(driverName, requestedWithoutDriver, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 加载外部动画文件
        /// 支持格式：file:path/to/animation.glb 或 file:path/animation.glb#AnimationName
        /// </summary>
        public ModelAnimation LoadExternalAnimation(string source) {
            // 移除 file: 前缀
            string pathAndName = source.Substring(5);

            // 解析路径和动画名称
            string filePath = pathAndName;
            string animationName = null;
            int hashIndex = pathAndName.IndexOf('#');
            if (hashIndex >= 0) {
                filePath = pathAndName.Substring(0, hashIndex);
                animationName = pathAndName.Substring(hashIndex + 1);
            }

            // 使用缓存加载
            LoadedAnimationData loadedData = AnimationCache.GetOrLoad(filePath, animationName, path => { return LoadAnimationFile(path); });
            if (loadedData == null) {
                return null;
            }

            // 查找指定的动画
            if (!string.IsNullOrEmpty(animationName)) {
                return loadedData.GetAnimation(animationName);
            }

            // 返回第一个动画
            return loadedData.Animations.FirstOrDefault();
        }

        /// <summary>
        /// 加载动画文件
        /// </summary>
        public LoadedAnimationData LoadAnimationFile(string path) {
            try {
                // 使用 GltfLoader 加载外部动画文件
                ModelData modelData = GltfLoader.LoadFromFile(path);
                if (modelData == null) {
                    return null;
                }

                // 创建 LoadedAnimationData，包含所有动画
                return new LoadedAnimationData(path, modelData, modelData.Animations);
            }
            catch {
                return null;
            }
        }

        /// <summary>
        /// 同步引擎参数（由子类扩展）
        /// </summary>
        protected virtual void SyncEngineParameters() { }

        /// <summary>
        /// 计算最终骨骼变换
        /// </summary>
        public void ComputeBoneTransforms(Matrix?[] boneTransforms) {
            // 1. 层混合（各层在采样点已按 RootStripInfo 剥根，无需全局后处理）
            m_blender.BlendLayers(m_layers, boneTransforms, m_model);

            // 2. KHR_animation_pointer 采样（材质/纹理属性动画）
            for (int i = 0; i < m_layers.Length; i++) {
                if (m_layers[i].IsActive) {
                    m_layers[i].AnimationPlayer?.SamplePointerTargets(m_model);
                }
            }

            // 3. Morph target 权重采样
            for (int i = 0; i < m_layers.Length; i++) {
                if (m_layers[i].IsActive) {
                    m_layers[i].AnimationPlayer?.SampleMorphWeights(m_model);
                }
            }

            // 4. IK 后处理（在层混合后应用）
            m_ikSolver?.Solve(boneTransforms, m_model);
        }

        /// <summary>
        /// 转发动画事件
        /// </summary>
        public void ForwardAnimationEvent(AnimationEvent animationEvent) {
            OnAnimationEvent?.Invoke(animationEvent);
        }

        /// <summary>
        /// 为指定层添加动画事件
        /// </summary>
        /// <param name="layerName">层名称</param>
        /// <param name="eventName">事件名称</param>
        /// <param name="time">触发时间</param>
        /// <param name="parameter">可选参数</param>
        public void AddAnimationEvent(string layerName, string eventName, float time, object parameter = null) {
            AnimationLayer layer = m_layers.FirstOrDefault(l => l.Name == layerName);
            if (layer != null) {
                layer.AnimationPlayer.AddEvent(eventName, time, parameter);
            }
        }

        /// <summary>
        /// 为所有层清除动画事件
        /// </summary>
        public void ClearAnimationEvents() {
            foreach (AnimationLayer layer in m_layers) {
                layer.AnimationPlayer.ClearEvents();
            }
        }

        /// <summary>
        /// 为指定层设置驱动器
        /// </summary>
        /// <param name="layerName">层名称</param>
        /// <param name="driver">驱动器实例</param>
        public void SetDriver(string layerName, IAnimationDriver driver) {
            AnimationLayer layer = m_layers.FirstOrDefault(l => l.Name == layerName);
            if (layer != null) {
                layer.SetDriver(driver);
            }
        }

        #region 手动动画控制 API

        /// <summary>
        /// 强制播放指定动画（支持配置文件中定义的别名）
        /// <para>
        /// 调用后，该层将跳过配置文件中的状态规则条件评估，
        /// 直到调用 <see cref="ReleaseManualControl" /> 释放控制权。
        /// </para>
        /// <para>
        /// <b>调用时机：</b>应在 <see cref="Update" /> 之前调用，
        /// 通常在 <see cref="SyncEngineParameters" /> 重写方法中或动画事件回调中调用。
        /// </para>
        /// </summary>
        /// <param name="layerName">层名称（如 "Base"、"Head"）</param>
        /// <param name="animationNameOrAlias">动画名或配置文件中定义的别名（如 "idle1"、"walk"）</param>
        /// <param name="loop">是否循环播放</param>
        /// <param name="blendDuration">过渡时长（秒），0 表示立即切换</param>
        /// <returns>是否成功开始播放</returns>
        public bool PlayAnimation(string layerName, string animationNameOrAlias, bool loop = true, float blendDuration = 0.3f) {
            AnimationLayer layer = m_layers.FirstOrDefault(l => l.Name == layerName);
            if (layer == null) {
                return false;
            }

            // 1. 检查是否是别名
            AnimationReference animRef;
            if (m_animationReferences.TryGetValue(animationNameOrAlias, out AnimationReference aliasRef)) {
                // caller 仅强制覆盖 loop/blendDuration（参数显式传入），其余字段继承 alias。
                // 修复：之前整体用 alias 替换，导致 alias 的 rootMotion/events/onComplete 等虽能继承，
                // 但语义不统一；现统一走 MergeWithAlias。
                AnimationReference caller = new() {
                    Source = animationNameOrAlias,
                    LoopValue = loop,
                    BlendDurationValue = blendDuration,
                    HasLoop = true,
                    HasBlendDuration = true
                };
                animRef = MergeWithAlias(caller, aliasRef);
            }
            else {
                // 2. 创建临时引用（直接使用动画名）
                animRef = new AnimationReference { Source = animationNameOrAlias, LoopValue = loop, BlendDurationValue = blendDuration };
            }

            // 3. 标记该层为手动控制
            m_manualOverrideLayers.Add(layerName);

            // 4. 设置保持姿态模式（非循环动画结束后保持当前姿态）
            layer.SetHoldPose(true);

            // 5. 应用到层并返回结果
            bool success = ApplyAnimationToLayer(layerName, animRef);
            if (!success) {
                // 应用失败时回滚状态
                layer.SetHoldPose(false);
                m_manualOverrideLayers.Remove(layerName);
            }
            return success;
        }

        /// <summary>
        /// 播放外部动画文件中的动画
        /// <para>
        /// 调用后，该层将跳过配置文件中的状态规则条件评估，
        /// 直到调用 <see cref="ReleaseManualControl" /> 释放控制权。
        /// </para>
        /// <para>
        /// <b>调用时机：</b>应在 <see cref="Update" /> 之前调用。
        /// </para>
        /// </summary>
        /// <param name="layerName">层名称</param>
        /// <param name="filePath">动画文件路径（相对于 Content 目录）</param>
        /// <param name="animationName">文件中的动画名称（可选，默认使用第一个动画）</param>
        /// <param name="loop">是否循环播放</param>
        /// <param name="blendDuration">过渡时长（秒）</param>
        /// <returns>是否成功开始播放</returns>
        public bool PlayExternalAnimation(string layerName,
            string filePath,
            string animationName = null,
            bool loop = true,
            float blendDuration = 0.3f) {
            string source = string.IsNullOrEmpty(animationName) ? $"file:{filePath}" : $"file:{filePath}#{animationName}";
            AnimationReference animRef = new() { Source = source, LoopValue = loop, BlendDurationValue = blendDuration };

            // 标记该层为手动控制
            m_manualOverrideLayers.Add(layerName);

            // 应用并返回结果
            return ApplyAnimationToLayer(layerName, animRef);
        }

        /// <summary>
        /// 停止指定层的动画播放
        /// <para>
        /// 此方法不会释放手动控制权，层仍会跳过状态规则评估。
        /// 如需恢复自动控制，请调用 <see cref="ReleaseManualControl" />。
        /// </para>
        /// </summary>
        /// <param name="layerName">层名称</param>
        public void StopAnimation(string layerName) {
            AnimationLayer layer = m_layers.FirstOrDefault(l => l.Name == layerName);
            layer?.StopAnimation();
        }

        /// <summary>
        /// 释放层的手动控制权，恢复配置文件中的状态规则自动评估
        /// <para>
        /// 释放后会强制重新评估该层的状态规则，确保动画状态正确恢复。
        /// </para>
        /// <para>
        /// <b>调用时机：</b>当手动动画播放完成，需要恢复自动状态切换时调用。
        /// 通常在动画完成回调或特定条件满足时调用。
        /// </para>
        /// </summary>
        /// <param name="layerName">层名称，为 null 时释放所有层</param>
        public void ReleaseManualControl(string layerName = null) {
            if (string.IsNullOrEmpty(layerName)) {
                // 清除所有层的保持姿态状态
                foreach (AnimationLayer layer in m_layers) {
                    layer?.SetHoldPose(false);
                }
                m_manualOverrideLayers.Clear();
                // 清除所有规则匹配缓存，强制重新评估
                m_lastMatchedRuleIndex.Clear();
                m_lastResolvedSource.Clear();
            }
            else {
                // 清除指定层的保持姿态状态
                AnimationLayer layer = m_layers.FirstOrDefault(l => l.Name == layerName);
                layer?.SetHoldPose(false);
                m_manualOverrideLayers.Remove(layerName);

                // 清除该层相关状态层的规则匹配缓存
                // 通过遍历状态配置找到该层对应的状态层
                if (m_stateConfigs != null) {
                    foreach ((string stateName, StateLayerConfig layerConfig) in m_stateConfigs) {
                        if (layerConfig.Layer == layerName) {
                            m_lastMatchedRuleIndex.Remove(stateName);
                            m_lastResolvedSource.Remove(stateName);
                        }
                    }
                }
            }

            // 设置参数脏标记，确保下一帧会重新评估状态规则
            // 这对于插播动画完成后立即切换到其他状态（如 Sit）很重要
            m_parameters.SetDirty();
        }

        /// <summary>
        /// 检查指定层是否处于手动控制模式
        /// </summary>
        /// <param name="layerName">层名称</param>
        /// <returns>是否被手动控制</returns>
        public bool IsManualControl(string layerName) => m_manualOverrideLayers.Contains(layerName);

        /// <summary>
        /// 获取配置文件中定义的动画别名列表
        /// </summary>
        /// <returns>别名列表</returns>
        public IEnumerable<string> GetAnimationAliases() => m_animationReferences.Keys;

        /// <summary>
        /// 检查动画别名是否存在
        /// </summary>
        /// <param name="alias">别名</param>
        /// <returns>是否存在</returns>
        public bool HasAnimationAlias(string alias) => m_animationReferences.ContainsKey(alias);

        #endregion

        #region IK API

        /// <summary>
        /// 注册 IK 链
        /// </summary>
        /// <param name="name">链名称</param>
        /// <param name="endBoneName">末端骨骼名称</param>
        /// <param name="algorithmName">算法名称（SingleBoneIK/TwoBoneIK/CCD/FABRIK），null 则自动选择</param>
        /// <param name="maxChainLength">最大链长度</param>
        public void RegisterIKChain(string name, string endBoneName, string algorithmName = null, int maxChainLength = 3) {
            IKSolver.RegisterChainByName(name, endBoneName, algorithmName, maxChainLength);
        }

        /// <summary>
        /// 设置 IK 位置目标
        /// </summary>
        /// <param name="chainName">链名称</param>
        /// <param name="targetPosition">目标位置（模型空间）</param>
        /// <param name="weight">权重（0-1）</param>
        public void SetIKTarget(string chainName, Vector3? targetPosition, float weight = 1.0f) {
            IKSolver.SetIKTarget(chainName, targetPosition, weight);
        }

        /// <summary>
        /// 设置 IK 方向目标
        /// </summary>
        /// <param name="chainName">链名称</param>
        /// <param name="aimDirection">目标方向（模型空间）</param>
        /// <param name="weight">权重（0-1）</param>
        public void SetIKAim(string chainName, Vector3? aimDirection, float weight = 1.0f) {
            IKSolver.SetIKAim(chainName, aimDirection, weight);
        }

        /// <summary>
        /// 设置完整 IK 目标
        /// </summary>
        /// <param name="chainName">链名称</param>
        /// <param name="target">IK 目标对象</param>
        public void SetIKTarget(string chainName, IKTarget target) {
            IKSolver.SetIKTarget(chainName, target);
        }

        /// <summary>
        /// 清除 IK 目标
        /// </summary>
        /// <param name="chainName">链名称</param>
        public void ClearIKTarget(string chainName) {
            IKSolver.ClearIKTarget(chainName);
        }

        /// <summary>
        /// 获取 IK 链
        /// </summary>
        public IKChain GetIKChain(string chainName) => IKSolver.GetChain(chainName);

        /// <summary>
        /// 注册 IK 链并立即构建（用于需要在注册后立即访问链对象的场景）
        /// </summary>
        /// <returns>构建的 IK 链，如果构建失败返回 null</returns>
        public IKChain RegisterAndBuildIKChain(string name, string endBoneName, string algorithmName = null, int maxChainLength = 3) {
            IKSolver.RegisterChainByName(name, endBoneName, algorithmName, maxChainLength);
            return IKSolver.BuildChainImmediate(name, m_model);
        }

        /// <summary>
        /// 获取 IK 目标
        /// </summary>
        public IKTarget GetIKTarget(string chainName) => IKSolver.GetTarget(chainName);

        #endregion

        /// <summary>
        /// 清理资源，取消事件订阅
        /// </summary>
        public void Dispose() {
            foreach (AnimationLayer layer in m_layers) {
                if (layer != null) {
                    layer.OnAnimationEvent -= ForwardAnimationEvent;
                }
            }
        }
    }
}