using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// 动画事件数据
    /// </summary>
    public class AnimationEvent {
        /// <summary>
        /// 事件名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 事件触发时间（归一化时间 0-1）
        /// </summary>
        public float Time { get; set; }

        /// <summary>
        /// 事件参数（可选）
        /// </summary>
        public object Parameter { get; set; }

        public AnimationEvent(string name, float time, object parameter = null) {
            Name = name;
            Time = time;
            Parameter = parameter;
        }
    }

    /// <summary>
    /// 动画事件处理器委托
    /// </summary>
    /// <param name="animationEvent">触发的事件</param>
    public delegate void AnimationEventHandler(AnimationEvent animationEvent);

    /// <summary>
    /// 动画播放器，负责采样和插值
    /// 支持相位范围播放（StartPhase, EndPhase）和循环边界插值
    /// </summary>
    public class AnimationPlayer {
        /// <summary>
        /// 当前渲染器是否支持 morph target 动画。由渲染器设置。
        /// </summary>
        public static bool MorphWeightAnimationEnabled;

        public Model m_model;
        public ModelAnimation m_animation;
        public float m_time;
        public float m_previousTime;
        public bool m_looping;
        public bool m_playing;
        public Dictionary<string, int> m_boneNameToIndex = new();
        public List<AnimationEvent> m_events = new();
        public int m_lastEventIndex = -1;

        // Phase range support
        public float m_startPhase;
        public float m_endPhase = 1f;
        public bool m_preservePose;
        public float m_wrapOvershoot; // For loop boundary interpolation
        public float m_keyInterval; // Cached keyframe interval for boundary interpolation
        public float[] m_weightBuffer; // Reusable buffer for morph weight interpolation

        /// <summary>
        /// 当前动画
        /// </summary>
        public ModelAnimation Animation => m_animation;

        /// <summary>
        /// 当前播放时间
        /// </summary>
        public float Time {
            get => m_time;
            set => m_time = value;
        }

        /// <summary>
        /// 起始相位 (0-1)，默认为 0
        /// </summary>
        public float StartPhase {
            get => m_startPhase;
            set => m_startPhase = Math.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// 结束相位 (0-1)，默认为 1
        /// </summary>
        public float EndPhase {
            get => m_endPhase;
            set => m_endPhase = Math.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// 是否在非播放状态下保持最后姿态
        /// </summary>
        public bool PreservePose {
            get => m_preservePose;
            set => m_preservePose = value;
        }

        /// <summary>
        /// 是否有有效的动画数据
        /// </summary>
        public bool HasValidAnimation => m_animation != null && m_animation.Duration > 0;

        /// <summary>
        /// 相位范围大小
        /// </summary>
        public float PhaseRange => Math.Abs(m_endPhase - m_startPhase);

        /// <summary>
        /// 相位方向：1 表示正向（EndPhase > StartPhase），-1 表示反向
        /// </summary>
        public int PhaseDirection => m_endPhase > m_startPhase ? 1 : -1;

        /// <summary>
        /// 实际播放方向：PhaseDirection × sign(Speed)
        /// </summary>
        public int ActualDirection => PhaseDirection * Math.Sign(Speed);

        /// <summary>
        /// 归一化时间 (0-1)，相对于相位范围
        /// </summary>
        public float NormalizedTime {
            get {
                if (!HasValidAnimation
                    || PhaseRange <= 0f) {
                    return 0f;
                }
                float effectiveTime = GetEffectiveTime(); // Time in seconds
                float normalizedPhase = effectiveTime / m_animation.Duration; // Convert to 0-1
                float normalizedProgress = (normalizedPhase - m_startPhase) / PhaseRange;
                return Math.Clamp(normalizedProgress, 0f, 1f);
            }
        }

        /// <summary>
        /// 是否正在播放
        /// </summary>
        public bool IsPlaying => m_playing;

        /// <summary>
        /// 关联的模型
        /// </summary>
        public Model Model => m_model;

        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool Loop {
            get => m_looping;
            set => m_looping = value;
        }

        /// <summary>
        /// 播放速度倍率
        /// </summary>
        public float Speed { get; set; } = 1.0f;

        /// <summary>
        /// 动画事件触发时调用
        /// </summary>
        public event AnimationEventHandler OnAnimationEvent;

        /// <summary>
        /// 获取动画事件列表
        /// </summary>
        public IReadOnlyList<AnimationEvent> Events => m_events;

        /// <summary>
        /// 获取有效时间（基于相位）
        /// </summary>
        public float GetEffectiveTime() {
            if (!HasValidAnimation) {
                return 0f;
            }
            float duration = m_animation.Duration;
            float startTime = m_startPhase * duration;
            float endTime = m_endPhase * duration;

            // 确保时间在相位范围内
            if (PhaseDirection > 0) {
                // 正向：StartPhase -> EndPhase
                return Math.Clamp(m_time, startTime, endTime);
            }
            // 反向：EndPhase -> StartPhase
            return Math.Clamp(m_time, endTime, startTime);
        }

        /// <summary>
        /// 设置动画
        /// </summary>
        public void SetAnimation(Model model, ModelAnimation animation) {
            m_model = model;
            m_animation = animation;
            m_time = 0f;
            m_previousTime = 0f;
            m_lastEventIndex = -1;
            m_wrapOvershoot = 0f;
            BuildBoneIndexMap();
        }

        /// <summary>
        /// 设置相位范围
        /// </summary>
        /// <param name="startPhase">起始相位 (0-1)</param>
        /// <param name="endPhase">结束相位 (0-1)</param>
        public void SetPhaseRange(float startPhase, float endPhase) {
            m_startPhase = Math.Clamp(startPhase, 0f, 1f);
            m_endPhase = Math.Clamp(endPhase, 0f, 1f);
            m_wrapOvershoot = 0f;
        }

        /// <summary>
        /// 添加动画事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="normalizedTime">触发时间（归一化时间 0-1）</param>
        /// <param name="parameter">可选参数</param>
        public void AddEvent(string eventName, float normalizedTime, object parameter = null) {
            m_events.Add(new AnimationEvent(eventName, normalizedTime, parameter));
            // 按时间排序
            m_events.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        /// <summary>
        /// 清除所有动画事件
        /// </summary>
        public void ClearEvents() {
            m_events.Clear();
            m_lastEventIndex = -1;
        }

        /// <summary>
        /// 开始播放
        /// </summary>
        public void Play(bool loop = true) {
            m_playing = true;
            m_looping = loop;
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop() {
            m_playing = false;
        }

        /// <summary>
        /// 设置归一化时间 (0-1)
        /// </summary>
        public void SetNormalizedTime(float normalizedTime) {
            if (m_animation != null
                && m_animation.Duration > 0) {
                m_time = normalizedTime * m_animation.Duration;
                m_previousTime = m_time;
                m_lastEventIndex = -1;
                m_wrapOvershoot = 0f;
            }
        }

        /// <summary>
        /// 更新动画时间
        /// </summary>
        public void Update(float deltaTime) {
            if (!m_playing) {
                return;
            }
            m_previousTime = m_time;

            // 如果有动画，处理相位范围逻辑
            if (!HasValidAnimation) {
                return;
            }
            float duration = m_animation.Duration;
            float startTime = m_startPhase * duration;
            float endTime = m_endPhase * duration;

            // 速度为 0 时停在 StartPhase
            if (Speed == 0f) {
                m_time = startTime;
                return;
            }

            // 更新时间
            m_time += deltaTime * Speed;

            // 计算边界
            float minTime = Math.Min(startTime, endTime);
            float maxTime = Math.Max(startTime, endTime);
            // 无相位范围（StartPhase==EndPhase，单帧姿势动画）：
            // 此时 minTime==maxTime，非循环反向分支 m_time+=dt 后 m_time(>0)<=minTime(0) 永不成立 → 永不停
            // → IsPlaying 永 true → onComplete 永不触发。取该相位帧并立即停止，使完成链正常。
            // 注：loop=true 单帧也走此分支即停（循环单帧语义退化，保持姿势无播放意义）；
            // 且 return 在 CheckEvents 前，单帧姿势的播放器级 AnimationEvent 不触发
            //（完成靠 controller 层 OnComplete，非 player event，当前无影响）。
            if (maxTime <= minTime) {
                m_time = startTime;
                m_playing = false;
                m_wrapOvershoot = 0f;
                return;
            }
            if (m_looping) {
                // 循环模式
                if (ActualDirection > 0) {
                    // 正向播放
                    if (m_time > maxTime) {
                        // Wrap 发生：记录超出的时间作为边界插值的起始点
                        m_wrapOvershoot = m_time - maxTime;
                        m_time = minTime + m_wrapOvershoot;
                        // 缓存关键帧间隔（从动画数据获取）
                        m_keyInterval = EstimateKeyInterval();
                        m_lastEventIndex = -1;
                    }
                    else if (m_wrapOvershoot > 0f) {
                        // 已经在 wrap 后，累加时间
                        m_wrapOvershoot += deltaTime * Math.Abs(Speed);
                        // 当累加超过关键帧间隔时，插值完成
                        if (m_keyInterval > 0f
                            && m_wrapOvershoot >= m_keyInterval) {
                            m_wrapOvershoot = 0f;
                            m_keyInterval = 0f;
                        }
                    }
                }
                else {
                    // 反向播放
                    if (m_time < minTime) {
                        m_wrapOvershoot = minTime - m_time;
                        m_time = maxTime - m_wrapOvershoot;
                        m_keyInterval = EstimateKeyInterval();
                        m_lastEventIndex = -1;
                    }
                    else if (m_wrapOvershoot > 0f) {
                        m_wrapOvershoot += deltaTime * Math.Abs(Speed);
                        if (m_keyInterval > 0f
                            && m_wrapOvershoot >= m_keyInterval) {
                            m_wrapOvershoot = 0f;
                            m_keyInterval = 0f;
                        }
                    }
                }
            }
            else {
                // 非循环模式：停在边界
                if (ActualDirection > 0) {
                    if (m_time >= maxTime) {
                        m_time = maxTime;
                        m_playing = false;
                    }
                }
                else {
                    if (m_time <= minTime) {
                        m_time = minTime;
                        m_playing = false;
                    }
                }
                m_wrapOvershoot = 0f;
            }

            // 检查事件
            CheckEvents(m_previousTime, m_time);
        }

        /// <summary>
        /// 检查并触发指定时间范围内的事件
        /// </summary>
        public void CheckEvents(float fromTime, float toTime) {
            if (m_events.Count == 0
                || OnAnimationEvent == null) {
                return;
            }
            float duration = m_animation?.Duration ?? 0f;
            if (duration <= 0f) {
                return;
            }

            // 将绝对时间转换为归一化时间
            float fromNormalized = fromTime / duration;
            float toNormalized = toTime / duration;
            for (int i = 0; i < m_events.Count; i++) {
                AnimationEvent evt = m_events[i];
                // 事件时间使用归一化时间 (0-1)
                // 检查事件时间是否在当前帧的时间范围内
                if (evt.Time > fromNormalized
                    && evt.Time <= toNormalized) {
                    // 确保每个事件只触发一次（通过索引跟踪）
                    if (i > m_lastEventIndex) {
                        OnAnimationEvent?.Invoke(evt);
                        m_lastEventIndex = i;
                    }
                }
            }
        }

        /// <summary>
        /// 采样当前时间的骨骼变换
        /// </summary>
        public void SampleBoneTransforms(Matrix?[] boneTransforms) {
            if (m_animation == null
                || m_model == null
                || boneTransforms == null) {
                return;
            }
            SampleAtTimeInternal(m_time, boneTransforms);
        }

        /// <summary>
        /// 采样 KHR_animation_pointer 目标（材质属性等）
        /// 直接修改 ModelMaterial 属性，递增 Version。
        /// </summary>
        public void SamplePointerTargets(Model model = null) {
            if (m_animation == null) {
                return;
            }
            Model m = model ?? m_model;
            float time = GetEffectiveTime();
            if (m_animation.PointerTargets.Count > 0) {
                for (int i = 0; i < m_animation.PointerTargets.Count; i++) {
                    m_animation.PointerTargets[i](time);
                }
            }
            if (m_animation.NodeVisibilityTargets.Count > 0
                && m != null) {
                for (int i = 0; i < m_animation.NodeVisibilityTargets.Count; i++) {
                    m_animation.NodeVisibilityTargets[i](time, m);
                }
            }
        }

        /// <summary>
        /// 在指定时间采样骨骼变换
        /// </summary>
        public void SampleAtTime(float time, Matrix?[] boneTransforms) {
            SampleAtTimeInternal(time, boneTransforms);
        }

        /// <summary>
        /// 采样 morph target 权重并写入 Model 的 MeshParts
        /// </summary>
        public void SampleMorphWeights(Model model) {
            if (m_animation == null
                || model == null
                || !MorphWeightAnimationEnabled) {
                return;
            }
            float time = m_time;
            foreach (ModelAnimation.AnimationChannel channel in m_animation.Channels) {
                if (channel.Property != ModelAnimation.AnimationProperty.Weights) {
                    continue;
                }
                ModelAnimation.AnimationSampler sampler = channel.Sampler;
                if (sampler?.Weights == null
                    || sampler.Weights.Length == 0) {
                    continue;
                }
                if (sampler.KeyTimes == null
                    || sampler.KeyTimes.Length == 0) {
                    continue;
                }
                float[] interpolated = SampleWeightArrays(sampler.Weights, sampler.KeyTimes, time, sampler.Interpolation);
                if (interpolated == null) {
                    continue;
                }
                foreach (ModelMesh mesh in model.Meshes) {
                    if (mesh.ParentBone?.Name != channel.TargetBoneName) {
                        continue;
                    }
                    foreach (ModelMeshPart part in mesh.MeshParts) {
                        if (part.MorphWeights == null) {
                            continue;
                        }
                        int copyLen = Math.Min(part.MorphWeights.Length, interpolated.Length);
                        if (copyLen > 0) {
                            Array.Copy(interpolated, part.MorphWeights, copyLen);
                        }
                    }
                }
            }
        }

        public float[] SampleWeightArrays(float[][] weights, float[] times, float time, ModelAnimation.InterpolationType interpolation) {
            if (weights == null
                || weights.Length == 0) {
                return null;
            }
            int len = weights[0]?.Length ?? 0;
            if (len == 0) {
                return null;
            }

            // Ensure buffer
            if (m_weightBuffer == null
                || m_weightBuffer.Length < len) {
                m_weightBuffer = new float[len];
            }
            int idx = FindKeyIndex(times, time);
            if (idx < 0) {
                idx = 0;
            }
            if (idx >= weights.Length - 1) {
                Array.Copy(weights[weights.Length - 1], m_weightBuffer, len);
                return m_weightBuffer;
            }
            if (interpolation == ModelAnimation.InterpolationType.Step) {
                Array.Copy(weights[idx], m_weightBuffer, len);
                return m_weightBuffer;
            }
            float t0 = times[idx];
            float t1 = times[idx + 1];
            float alpha = (time - t0) / (t1 - t0);
            for (int i = 0; i < len; i++) {
                m_weightBuffer[i] = weights[idx][i] + (weights[idx + 1][i] - weights[idx][i]) * alpha;
            }
            return m_weightBuffer;
        }

        /// <summary>
        /// 内部采样方法
        /// </summary>
        public void SampleAtTimeInternal(float time, Matrix?[] boneTransforms) {
            if (m_animation == null
                || m_model == null
                || boneTransforms == null) {
                return;
            }

            // 按骨骼分组通道，合并同一骨骼的所有属性
            Dictionary<int, (Vector3? translation, Quaternion? rotation, Vector3? scale)> boneTransformsData = new();
            foreach (ModelAnimation.AnimationChannel channel in m_animation.Channels) {
                if (!m_boneNameToIndex.TryGetValue(channel.TargetBoneName, out int boneIndex)) {
                    continue;
                }
                ModelAnimation.AnimationSampler sampler = channel.Sampler;
                if (sampler == null
                    || sampler.KeyTimes == null
                    || sampler.KeyTimes.Length == 0) {
                    continue;
                }

                // 获取或创建该骨骼的变换数据
                if (!boneTransformsData.TryGetValue(boneIndex, out (Vector3? translation, Quaternion? rotation, Vector3? scale) data)) {
                    data = (null, null, null);
                }
                switch (channel.Property) {
                    case ModelAnimation.AnimationProperty.Weights: continue; // Handled by SampleMorphWeights
                    case ModelAnimation.AnimationProperty.Translation:
                        if (sampler.Translations != null
                            && sampler.Translations.Length > 0) {
                            data.translation = SampleVector3(sampler.Translations, sampler.KeyTimes, time, sampler.Interpolation);
                        }
                        break;
                    case ModelAnimation.AnimationProperty.Rotation:
                        if (sampler.Rotations != null
                            && sampler.Rotations.Length > 0) {
                            data.rotation = SampleQuaternion(sampler.Rotations, sampler.KeyTimes, time, sampler.Interpolation);
                        }
                        break;
                    case ModelAnimation.AnimationProperty.Scale:
                        if (sampler.Scales != null
                            && sampler.Scales.Length > 0) {
                            data.scale = SampleVector3(sampler.Scales, sampler.KeyTimes, time, sampler.Interpolation);
                        }
                        break;
                }
                boneTransformsData[boneIndex] = data;
            }

            // 为每个骨骼构建完整的变换矩阵
            // 关键：当动画没有提供某个分量时，使用骨骼原始变换的对应分量
            foreach (KeyValuePair<int, (Vector3? translation, Quaternion? rotation, Vector3? scale)> kvp in boneTransformsData) {
                int boneIndex = kvp.Key;
                (Vector3? animTranslation, Quaternion? animRotation, Vector3? animScale) = kvp.Value;

                // 获取骨骼原始变换并分解
                ModelBone bone = m_model.m_bones[boneIndex];
                Vector3 origScale, origTranslation;
                Quaternion origRotation;
                bone.Transform.Decompose(out origScale, out origRotation, out origTranslation);

                // 使用动画值或原始值
                Vector3 finalScale = animScale ?? origScale;
                Quaternion finalRotation = animRotation ?? origRotation;
                Vector3 finalTranslation = animTranslation ?? origTranslation;

                // 构建变换矩阵: Scale * Rotation * Translation
                Matrix transform = Matrix.CreateScale(finalScale)
                    * Matrix.CreateFromQuaternion(finalRotation)
                    * Matrix.CreateTranslation(finalTranslation);
                boneTransforms[boneIndex] = transform;
            }
        }

        /// <summary>
        /// 在指定相位采样骨骼变换（用于 PreservePose）
        /// </summary>
        public void SampleBoneTransformsAtPhase(float phase, Matrix?[] boneTransforms) {
            if (m_animation == null
                || boneTransforms == null) {
                return;
            }
            float time = Math.Clamp(phase, 0f, 1f) * m_animation.Duration;
            SampleAtTimeInternal(time, boneTransforms);
        }

        /// <summary>
        /// 混合两个矩阵
        /// </summary>
        public Matrix BlendMatrix(Matrix a, Matrix b, float t) {
            // 分解矩阵
            Vector3 scaleA, translationA;
            Quaternion rotationA;
            a.Decompose(out scaleA, out rotationA, out translationA);
            Vector3 scaleB, translationB;
            Quaternion rotationB;
            b.Decompose(out scaleB, out rotationB, out translationB);

            // 插值
            Vector3 blendedScale = Vector3.Lerp(scaleA, scaleB, t);
            Quaternion blendedRotation = Quaternion.Slerp(rotationA, rotationB, t);
            Vector3 blendedTranslation = Vector3.Lerp(translationA, translationB, t);

            // 重建矩阵
            return Matrix.CreateScale(blendedScale) * Matrix.CreateFromQuaternion(blendedRotation) * Matrix.CreateTranslation(blendedTranslation);
        }

        public Vector3 SampleVector3(Vector3[] values, float[] times, float time, ModelAnimation.InterpolationType interpolation) {
            if (values == null
                || values.Length == 0) {
                return Vector3.Zero;
            }
            if (values.Length == 1) {
                return values[0];
            }
            int idx = FindKeyIndex(times, time);
            if (idx < 0) {
                return values[0];
            }

            // 循环边界插值：当 wrap 发生后，从边界帧插值到当前位置
            // _wrapOvershoot 会累积直到超过 _keyInterval
            if (m_looping
                && m_wrapOvershoot > 0f
                && m_keyInterval > 0f
                && times.Length >= 2) {
                // 计算插值权重：wrap 后经过的时间 / 关键帧间隔
                float blendAlpha = m_wrapOvershoot / m_keyInterval;
                blendAlpha = Math.Clamp(blendAlpha, 0f, 1f);

                // 获取当前位置的值（正常插值）
                Vector3 currentValue;
                if (idx >= values.Length - 1) {
                    currentValue = values[0];
                }
                else {
                    float kt0 = times[idx];
                    float kt1 = times[idx + 1];
                    float kalpha = (time - kt0) / (kt1 - kt0);
                    currentValue = interpolation switch {
                        ModelAnimation.InterpolationType.Step => values[idx],
                        _ => Vector3.Lerp(values[idx], values[idx + 1], kalpha)
                    };
                }

                // 边界值：正向播放从最后一帧开始，反向播放从第一帧开始
                Vector3 boundaryValue = ActualDirection > 0 ? values[^1] : values[0];
                return interpolation switch {
                    ModelAnimation.InterpolationType.Step => boundaryValue,
                    _ => Vector3.Lerp(boundaryValue, currentValue, blendAlpha)
                };
            }
            if (idx >= values.Length - 1) {
                return values[values.Length - 1];
            }
            float t0 = times[idx];
            float t1 = times[idx + 1];
            float alpha = (time - t0) / (t1 - t0);
            // CUBICSPLINE 在加载时由 SharpGLTF 以 30fps 预采样为 LINEAR 关键帧，
            // 运行时只需线性插值即可逼近原始样条曲线精度。
            return interpolation switch {
                ModelAnimation.InterpolationType.Step => values[idx],
                _ => Vector3.Lerp(values[idx], values[idx + 1], alpha)
            };
        }

        public Quaternion SampleQuaternion(Quaternion[] values, float[] times, float time, ModelAnimation.InterpolationType interpolation) {
            if (values == null
                || values.Length == 0) {
                return Quaternion.Identity;
            }
            if (values.Length == 1) {
                return values[0];
            }
            int idx = FindKeyIndex(times, time);
            if (idx < 0) {
                return values[0];
            }

            // 循环边界插值：当 wrap 发生后，从边界帧插值到当前位置
            if (m_looping
                && m_wrapOvershoot > 0f
                && m_keyInterval > 0f
                && times.Length >= 2) {
                float blendAlpha = m_wrapOvershoot / m_keyInterval;
                blendAlpha = Math.Clamp(blendAlpha, 0f, 1f);

                // 获取当前位置的值（正常插值）
                Quaternion currentValue;
                if (idx >= values.Length - 1) {
                    currentValue = values[0];
                }
                else {
                    float qt0 = times[idx];
                    float qt1 = times[idx + 1];
                    float qalpha = (time - qt0) / (qt1 - qt0);
                    currentValue = interpolation switch {
                        ModelAnimation.InterpolationType.Step => values[idx],
                        _ => Quaternion.Slerp(values[idx], values[idx + 1], qalpha)
                    };
                }

                // 边界值：正向播放从最后一帧开始，反向播放从第一帧开始
                Quaternion boundaryValue = ActualDirection > 0 ? values[^1] : values[0];
                return interpolation switch {
                    ModelAnimation.InterpolationType.Step => boundaryValue,
                    _ => Quaternion.Slerp(boundaryValue, currentValue, blendAlpha)
                };
            }
            if (idx >= values.Length - 1) {
                return values[values.Length - 1];
            }
            float t0 = times[idx];
            float t1 = times[idx + 1];
            float alpha = (time - t0) / (t1 - t0);
            return interpolation switch {
                ModelAnimation.InterpolationType.Step => values[idx],
                _ => Quaternion.Slerp(values[idx], values[idx + 1], alpha)
            };
        }

        public int FindKeyIndex(float[] times, float time) {
            if (times == null
                || times.Length == 0) {
                return -1;
            }
            for (int i = 0; i < times.Length - 1; i++) {
                if (time >= times[i]
                    && time < times[i + 1]) {
                    return i;
                }
            }
            return times.Length - 1;
        }

        /// <summary>
        /// 估算关键帧间隔（用于循环边界插值）
        /// </summary>
        public float EstimateKeyInterval() {
            if (m_animation == null
                || m_animation.Channels == null
                || m_animation.Channels.Count == 0) {
                return 0f;
            }

            // 从第一个有效的通道获取关键帧时间
            foreach (ModelAnimation.AnimationChannel channel in m_animation.Channels) {
                ModelAnimation.AnimationSampler sampler = channel.Sampler;
                if (sampler?.KeyTimes != null
                    && sampler.KeyTimes.Length >= 2) {
                    float[] times = sampler.KeyTimes;
                    // 使用最后两个关键帧的间隔
                    return times[^1] - times[^2];
                }
            }
            return 0f;
        }

        public void BuildBoneIndexMap() {
            m_boneNameToIndex.Clear();
            if (m_model == null) {
                return;
            }
            foreach (ModelBone bone in m_model.m_bones) {
                m_boneNameToIndex[bone.Name] = bone.Index;
            }
        }
    }
}