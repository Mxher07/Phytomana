namespace Engine.Animation.RootMotion {
    /// <summary>
    /// 根运动位移数据缓存
    /// </summary>
    public class RootMotionCache {
        public readonly List<(float time, Vector3 position)> m_positionSamples = new();
        public float m_animationDuration;
        public Vector3 m_totalTranslation;
        public Vector3 m_peakVelocity;
        public int m_lastSampleIndex;

        /// <summary>
        /// 是否有位移数据
        /// </summary>
        public bool HasTranslationData => m_positionSamples.Count > 1;

        /// <summary>
        /// 从动画数据构建缓存
        /// </summary>
        /// <param name="animation">动画数据</param>
        /// <param name="rootBoneName">根骨骼名称</param>
        public void BuildFromAnimation(ModelAnimation animation, string rootBoneName) {
            m_positionSamples.Clear();
            m_animationDuration = animation.Duration;
            m_totalTranslation = Vector3.Zero;
            m_peakVelocity = Vector3.Zero;
            m_lastSampleIndex = 0;

            // 查找根骨骼的位移通道
            ModelAnimation.AnimationChannel translationChannel = null;
            foreach (ModelAnimation.AnimationChannel channel in animation.Channels) {
                if (channel.TargetBoneName == rootBoneName
                    && channel.Property == ModelAnimation.AnimationProperty.Translation) {
                    translationChannel = channel;
                    break;
                }
            }
            if (translationChannel == null) {
                return;
            }
            ModelAnimation.AnimationSampler sampler = translationChannel.Sampler;
            if (sampler.KeyTimes == null
                || sampler.Translations == null) {
                return;
            }
            int count = Math.Min(sampler.KeyTimes.Length, sampler.Translations.Length);
            if (count == 0) {
                return;
            }
            for (int i = 0; i < count; i++) {
                m_positionSamples.Add((sampler.KeyTimes[i], sampler.Translations[i]));
            }
            if (m_positionSamples.Count > 0) {
                m_totalTranslation = m_positionSamples[^1].position - m_positionSamples[0].position;
                m_peakVelocity = CalculatePeakVelocity();
            }
        }

        /// <summary>
        /// 计算动画中的峰值速度
        /// </summary>
        public Vector3 CalculatePeakVelocity() {
            if (m_positionSamples.Count < 2) {
                return Vector3.Zero;
            }
            Vector3 peakVel = Vector3.Zero;
            for (int i = 0; i < m_positionSamples.Count - 1; i++) {
                (float t1, Vector3 p1) = m_positionSamples[i];
                (float t2, Vector3 p2) = m_positionSamples[i + 1];
                float dt = t2 - t1;
                if (dt <= 0) {
                    continue;
                }
                Vector3 vel = (p2 - p1) / dt;
                if (vel.LengthSquared() > peakVel.LengthSquared()) {
                    peakVel = vel;
                }
            }
            return peakVel;
        }

        /// <summary>
        /// 获取指定时间区间的位移速度（本地坐标系）
        /// </summary>
        /// <param name="prevTime">上一帧时间</param>
        /// <param name="currentTime">当前时间</param>
        /// <returns>速度向量</returns>
        public Vector3 GetVelocity(float prevTime, float currentTime) {
            if (m_positionSamples.Count < 2) {
                return Vector3.Zero;
            }
            Vector3 prevPos = SamplePosition(prevTime);
            Vector3 currentPos = SamplePosition(currentTime);
            float deltaTime = currentTime - prevTime;
            if (deltaTime <= 0) {
                // 处理循环回绕
                if (currentTime < prevTime
                    && m_animationDuration > 0) {
                    deltaTime = m_animationDuration - prevTime + currentTime;
                    if (deltaTime <= 0) {
                        return Vector3.Zero;
                    }
                    Vector3 endPos = m_positionSamples[^1].position;
                    Vector3 startPos = m_positionSamples[0].position;
                    Vector3 delta = endPos - prevPos + (currentPos - startPos);
                    return delta / deltaTime;
                }
                return Vector3.Zero;
            }
            return (currentPos - prevPos) / deltaTime;
        }

        /// <summary>
        /// 采样指定时间的位置
        /// </summary>
        public Vector3 SamplePosition(float time) {
            if (m_positionSamples.Count == 0) {
                return Vector3.Zero;
            }
            if (m_positionSamples.Count == 1) {
                return m_positionSamples[0].position;
            }

            // 处理循环
            if (m_animationDuration > 0) {
                time = time % m_animationDuration;
                if (time < 0) {
                    time += m_animationDuration;
                }
            }

            // 从上次位置开始搜索（通常只需要 0-2 次比较）
            int startIdx = m_lastSampleIndex;
            for (int i = startIdx; i < m_positionSamples.Count - 1; i++) {
                if (m_positionSamples[i].time <= time
                    && m_positionSamples[i + 1].time >= time) {
                    m_lastSampleIndex = i;
                    (float t1, Vector3 p1) = m_positionSamples[i];
                    (float t2, Vector3 p2) = m_positionSamples[i + 1];
                    float t = t2 - t1 > 0 ? (time - t1) / (t2 - t1) : 0;
                    return Vector3.Lerp(p1, p2, t);
                }
            }

            // 回退到二分查找
            int left = 0, right = m_positionSamples.Count - 1;
            while (left < right - 1) {
                int mid = (left + right) / 2;
                if (m_positionSamples[mid].time <= time) {
                    left = mid;
                }
                else {
                    right = mid;
                }
            }
            m_lastSampleIndex = left;
            if (left == right) {
                return m_positionSamples[left].position;
            }
            {
                (float t1, Vector3 p1) = m_positionSamples[left];
                (float t2, Vector3 p2) = m_positionSamples[right];
                float t = t2 - t1 > 0 ? (time - t1) / (t2 - t1) : 0;
                return Vector3.Lerp(p1, p2, t);
            }
        }

        /// <summary>
        /// 获取动画总位移（用于 AddImpulse 模式）
        /// </summary>
        public Vector3 GetTotalTranslation() => m_totalTranslation;

        /// <summary>
        /// 获取平均速度（用于 AddImpulse 模式 Average 方式）
        /// </summary>
        public Vector3 GetAverageVelocity() => m_animationDuration > 0 ? m_totalTranslation / m_animationDuration : Vector3.Zero;

        /// <summary>
        /// 获取峰值速度（用于 AddImpulse 模式 Peak 方式）
        /// </summary>
        public Vector3 GetPeakVelocity() => m_peakVelocity;

        /// <summary>
        /// 获取指定相位区间内的平均速度
        /// </summary>
        public Vector3 GetAverageVelocity(float startPhase, float endPhase) {
            if (m_positionSamples.Count < 2 || m_animationDuration <= 0) {
                return Vector3.Zero;
            }
            Vector3 startPos = SamplePosition(startPhase * m_animationDuration);
            Vector3 endPos = SamplePosition(endPhase * m_animationDuration);
            float deltaTime = MathF.Abs(endPhase - startPhase) * m_animationDuration;
            return deltaTime > 0 ? (endPos - startPos) / deltaTime : Vector3.Zero;
        }

        /// <summary>
        /// 获取指定相位区间内的峰值速度
        /// </summary>
        public Vector3 GetPeakVelocity(float startPhase, float endPhase) {
            if (m_positionSamples.Count < 2 || m_animationDuration <= 0) {
                return Vector3.Zero;
            }
            float startTime = Math.Min(startPhase, endPhase) * m_animationDuration;
            float endTime = Math.Max(startPhase, endPhase) * m_animationDuration;
            Vector3 peakVel = Vector3.Zero;
            for (int i = 0; i < m_positionSamples.Count - 1; i++) {
                (float t1, Vector3 p1) = m_positionSamples[i];
                (float t2, Vector3 p2) = m_positionSamples[i + 1];
                if (t2 < startTime || t1 > endTime) {
                    continue;
                }
                float dt = t2 - t1;
                if (dt <= 0) {
                    continue;
                }
                Vector3 vel = (p2 - p1) / dt;
                if (vel.LengthSquared() > peakVel.LengthSquared()) {
                    peakVel = vel;
                }
            }
            return peakVel;
        }
    }

    /// <summary>
    /// 根骨骼缩放数据缓存
    /// </summary>
    public class RootScaleCache {
        public readonly List<(float time, Vector3 scale)> _scaleSamples = new();
        public float _animationDuration;

        /// <summary>
        /// 是否有缩放数据
        /// </summary>
        public bool HasScaleData => _scaleSamples.Count > 0;

        /// <summary>
        /// 从动画数据构建缓存
        /// </summary>
        /// <param name="animation">动画数据</param>
        /// <param name="rootBoneName">根骨骼名称</param>
        public void BuildFromAnimation(ModelAnimation animation, string rootBoneName) {
            _scaleSamples.Clear();
            _animationDuration = animation.Duration;

            // 查找根骨骼的缩放通道
            ModelAnimation.AnimationChannel scaleChannel = null;
            foreach (ModelAnimation.AnimationChannel channel in animation.Channels) {
                if (channel.TargetBoneName == rootBoneName
                    && channel.Property == ModelAnimation.AnimationProperty.Scale) {
                    scaleChannel = channel;
                    break;
                }
            }
            if (scaleChannel == null) {
                return;
            }
            ModelAnimation.AnimationSampler sampler = scaleChannel.Sampler;
            if (sampler.KeyTimes == null
                || sampler.Scales == null) {
                return;
            }
            int count = Math.Min(sampler.KeyTimes.Length, sampler.Scales.Length);
            for (int i = 0; i < count; i++) {
                _scaleSamples.Add((sampler.KeyTimes[i], sampler.Scales[i]));
            }
        }

        /// <summary>
        /// 采样指定时间的缩放值
        /// </summary>
        /// <param name="normalizedTime">归一化时间 (0-1)</param>
        /// <returns>缩放向量</returns>
        public Vector3 SampleScale(float normalizedTime) {
            if (_scaleSamples.Count == 0) {
                return Vector3.One;
            }
            if (_scaleSamples.Count == 1) {
                return _scaleSamples[0].scale;
            }
            float time = normalizedTime * _animationDuration;

            // 处理循环
            if (_animationDuration > 0) {
                time = time % _animationDuration;
                if (time < 0) {
                    time += _animationDuration;
                }
            }

            // 二分查找
            int left = 0, right = _scaleSamples.Count - 1;
            while (left < right - 1) {
                int mid = (left + right) / 2;
                if (_scaleSamples[mid].time <= time) {
                    left = mid;
                }
                else {
                    right = mid;
                }
            }
            if (left == right) {
                return _scaleSamples[left].scale;
            }
            (float t1, Vector3 s1) = _scaleSamples[left];
            (float t2, Vector3 s2) = _scaleSamples[right];
            float t = t2 - t1 > 0 ? (time - t1) / (t2 - t1) : 0;
            return Vector3.Lerp(s1, s2, t);
        }
    }
}