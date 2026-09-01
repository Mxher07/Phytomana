using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// 混合空间采样点 - 一维
    /// </summary>
    public class AnimationBlendSample {
        /// <summary>
        /// 参数值
        /// </summary>
        public float Value { get; set; }

        /// <summary>
        /// 对应的动画名称
        /// </summary>
        public string AnimationName { get; set; }

        /// <summary>
        /// 动画配置（可选）
        /// </summary>
        public AnimationSourceConfig AnimationConfig { get; set; }
    }

    /// <summary>
    /// 混合空间采样点 - 二维
    /// </summary>
    public class AnimationBlendSample2D {
        /// <summary>
        /// X 轴参数值
        /// </summary>
        public float ValueX { get; set; }

        /// <summary>
        /// Y 轴参数值
        /// </summary>
        public float ValueY { get; set; }

        /// <summary>
        /// 对应的动画名称
        /// </summary>
        public string AnimationName { get; set; }

        /// <summary>
        /// 动画配置（可选）
        /// </summary>
        public AnimationSourceConfig AnimationConfig { get; set; }
    }

    /// <summary>
    /// 一维混合空间定义
    /// </summary>
    public class AnimationBlendSpaceDefinition {
        /// <summary>
        /// 混合空间名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 参数名称（从 AnimationParameters 读取）
        /// </summary>
        public string ParameterName { get; set; }

        /// <summary>
        /// 采样点列表（按 Value 升序排列）
        /// </summary>
        public AnimationBlendSample[] Samples { get; set; }

        /// <summary>
        /// 是否同步动画时间（保持所有动画在相同的归一化时间）
        /// </summary>
        public bool SyncTime { get; set; } = true;
    }

    /// <summary>
    /// 二维混合空间定义
    /// </summary>
    public class AnimationBlendSpaceDefinition2D {
        /// <summary>
        /// 混合空间名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// X 轴参数名称
        /// </summary>
        public string ParameterNameX { get; set; }

        /// <summary>
        /// Y 轴参数名称
        /// </summary>
        public string ParameterNameY { get; set; }

        /// <summary>
        /// 采样点列表
        /// </summary>
        public AnimationBlendSample2D[] Samples { get; set; }

        /// <summary>
        /// 是否同步动画时间
        /// </summary>
        public bool SyncTime { get; set; } = true;
    }

    /// <summary>
    /// 混合空间动画来源 - 根据参数在多个动画间插值
    /// 支持一维和二维混合空间，类似于 Unity 的 BlendTree 或 Unreal 的 Blend Space
    /// </summary>
    public class BlendSpaceSource : IAnimationSource {
        public string Name { get; }

        public readonly AnimationBlendSpaceDefinition m_definition1D;
        public readonly AnimationBlendSpaceDefinition2D m_definition2D;
        public readonly ClipAnimationSource[] m_sources;
        public readonly Model m_model;
        public readonly bool m_is2D;

        // 预分配缓冲区，避免每帧 GC
        public Matrix?[] m_tempTransformsBuffer1;
        public Matrix?[] m_tempTransformsBuffer2;
        public int m_bufferSize;

        // 时间同步相关
        public float m_syncedNormalizedTime;
        public readonly bool m_syncTime;

        // 缓存的参数值（用于 SampleTransforms）
        public float m_cachedParamValue;
        public float m_cachedParamX;
        public float m_cachedParamY;

        // 预分配的二维混合缓冲区（避免每帧 GC）
        public float[] m_blend2DDistances;
        public int[] m_blend2DSortedIndices;
        public float[] m_blend2DWeights;
        public int m_blend2DMaxSamples;

        // 有效源索引（过滤掉 null）
        public int[] m_validSourceIndices;

        /// <summary>
        /// 创建一维混合空间
        /// </summary>
        public BlendSpaceSource(AnimationBlendSpaceDefinition definition, Model model) {
            m_definition1D = definition ?? throw new ArgumentNullException(nameof(definition));
            m_model = model ?? throw new ArgumentNullException(nameof(model));
            m_is2D = false;
            m_syncTime = definition.SyncTime;
            Name = definition.Name ?? "BlendSpace1D";
            if (definition.Samples == null
                || definition.Samples.Length == 0) {
                m_sources = Array.Empty<ClipAnimationSource>();
                m_validSourceIndices = Array.Empty<int>();
                return;
            }

            // 按值排序采样点，并更新定义
            definition.Samples = definition.Samples.OrderBy(s => s.Value).ToArray();
            m_sources = new ClipAnimationSource[definition.Samples.Length];
            List<int> validIndices = new();
            for (int i = 0; i < definition.Samples.Length; i++) {
                AnimationBlendSample sample = definition.Samples[i];
                ModelAnimation anim = model.Animations?.FirstOrDefault(a => a.Name == sample.AnimationName);
                if (anim != null) {
                    AnimationSourceConfig config = sample.AnimationConfig ?? new AnimationSourceConfig { LoopValue = true };
                    config.LoopValue = true; // 混合空间中的动画必须循环
                    m_sources[i] = new ClipAnimationSource(model, anim, config);
                    validIndices.Add(i);
                }
            }

            // 记录有效源索引（过滤掉 null）
            m_validSourceIndices = validIndices.ToArray();

            // 预分配缓冲区
            if (model.Bones.Count > 0) {
                m_bufferSize = model.Bones.Count;
                m_tempTransformsBuffer1 = new Matrix?[m_bufferSize];
                m_tempTransformsBuffer2 = new Matrix?[m_bufferSize];
            }

            // 预分配二维混合缓冲区
            AllocateBlend2DBuffers(definition.Samples.Length);
        }

        /// <summary>
        /// 创建二维混合空间
        /// </summary>
        public BlendSpaceSource(AnimationBlendSpaceDefinition2D definition, Model model) {
            m_definition2D = definition ?? throw new ArgumentNullException(nameof(definition));
            m_model = model ?? throw new ArgumentNullException(nameof(model));
            m_is2D = true;
            m_syncTime = definition.SyncTime;
            Name = definition.Name ?? "BlendSpace2D";
            if (definition.Samples == null
                || definition.Samples.Length == 0) {
                m_sources = Array.Empty<ClipAnimationSource>();
                m_validSourceIndices = Array.Empty<int>();
                return;
            }
            m_sources = new ClipAnimationSource[definition.Samples.Length];
            List<int> validIndices = new();
            for (int i = 0; i < definition.Samples.Length; i++) {
                AnimationBlendSample2D sample = definition.Samples[i];
                ModelAnimation anim = model.Animations?.FirstOrDefault(a => a.Name == sample.AnimationName);
                if (anim != null) {
                    AnimationSourceConfig config = sample.AnimationConfig ?? new AnimationSourceConfig { LoopValue = true };
                    config.LoopValue = true;
                    m_sources[i] = new ClipAnimationSource(model, anim, config);
                    validIndices.Add(i);
                }
            }

            // 记录有效源索引（过滤掉 null）
            m_validSourceIndices = validIndices.ToArray();

            // 预分配缓冲区
            if (model.Bones.Count > 0) {
                m_bufferSize = model.Bones.Count;
                m_tempTransformsBuffer1 = new Matrix?[m_bufferSize];
                m_tempTransformsBuffer2 = new Matrix?[m_bufferSize];
            }

            // 预分配二维混合缓冲区
            AllocateBlend2DBuffers(definition.Samples.Length);
        }

        public void Update(float deltaTime, AnimationParameters parameters) {
            if (m_sources == null
                || m_sources.Length == 0) {
                return;
            }

            // 缓存参数值，供 SampleTransforms 使用
            if (parameters != null) {
                if (m_is2D) {
                    if (!string.IsNullOrEmpty(m_definition2D.ParameterNameX)) {
                        m_cachedParamX = parameters.GetFloat(m_definition2D.ParameterNameX);
                    }
                    if (!string.IsNullOrEmpty(m_definition2D.ParameterNameY)) {
                        m_cachedParamY = parameters.GetFloat(m_definition2D.ParameterNameY);
                    }
                }
                else {
                    if (!string.IsNullOrEmpty(m_definition1D.ParameterName)) {
                        m_cachedParamValue = parameters.GetFloat(m_definition1D.ParameterName);
                    }
                }
            }

            // 时间同步模式：所有动画使用相同的归一化时间
            if (m_syncTime) {
                // 更新同步时间
                m_syncedNormalizedTime += deltaTime * GetFirstValidAnimationSpeed();
                if (m_syncedNormalizedTime >= 1f) {
                    m_syncedNormalizedTime -= 1f;
                }
                else if (m_syncedNormalizedTime < 0f) {
                    m_syncedNormalizedTime += 1f;
                }

                // 同步所有有效动画
                foreach (int idx in m_validSourceIndices) {
                    m_sources[idx].Player.SetNormalizedTime(m_syncedNormalizedTime);
                }
            }
            else {
                // 独立更新每个动画
                foreach (int idx in m_validSourceIndices) {
                    m_sources[idx].Update(deltaTime, parameters);
                }
            }
        }

        public float GetFirstValidAnimationSpeed() {
            if (m_validSourceIndices.Length > 0
                && m_sources[m_validSourceIndices[0]] != null) {
                return m_sources[m_validSourceIndices[0]].Player.Speed;
            }
            return 1f;
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            if (m_sources == null
                || m_sources.Length == 0
                || boneTransforms == null) {
                return;
            }

            // 确保缓冲区大小足够
            EnsureBufferSize(boneTransforms.Length);
            if (m_is2D) {
                SampleTransforms2D(boneTransforms, model);
            }
            else {
                SampleTransforms1D(boneTransforms, model);
            }
        }

        public void SampleTransforms1D(Matrix?[] boneTransforms, Model model) {
            // 使用缓存的参数值
            float paramValue = m_cachedParamValue;

            // 找到相邻的两个采样点
            (int lowerIdx, int upperIdx, float t) = FindBlendSamples1D(paramValue);
            if (lowerIdx < 0
                || m_sources[lowerIdx] == null) {
                // 没有有效的动画
                return;
            }
            if (lowerIdx == upperIdx
                || t <= 0.001f) {
                // 只使用一个动画
                m_sources[lowerIdx].SampleTransforms(boneTransforms, model);
            }
            else if (t >= 0.999f) {
                // 只使用另一个动画
                if (m_sources[upperIdx] != null) {
                    m_sources[upperIdx].SampleTransforms(boneTransforms, model);
                }
                else {
                    m_sources[lowerIdx].SampleTransforms(boneTransforms, model);
                }
            }
            else {
                // 混合两个动画
                Array.Clear(m_tempTransformsBuffer1, 0, m_bufferSize);
                Array.Clear(m_tempTransformsBuffer2, 0, m_bufferSize);
                m_sources[lowerIdx].SampleTransforms(m_tempTransformsBuffer1, model);
                m_sources[upperIdx].SampleTransforms(m_tempTransformsBuffer2, model);
                BlendTransforms(boneTransforms, m_tempTransformsBuffer1, m_tempTransformsBuffer2, t);
            }
        }

        public void SampleTransforms2D(Matrix?[] boneTransforms, Model model) {
            // 使用缓存的参数值
            (int[] indices, float[] weights) = FindBlendSamples2D(m_cachedParamX, m_cachedParamY);
            if (indices == null
                || weights == null
                || indices.Length == 0) {
                return;
            }

            // 只有一个有效采样点
            if (indices.Length == 1
                && m_sources[indices[0]] != null) {
                m_sources[indices[0]].SampleTransforms(boneTransforms, model);
                return;
            }

            // 多个采样点加权混合
            // 先混合前两个
            if (indices.Length >= 2
                && m_sources[indices[0]] != null
                && m_sources[indices[1]] != null) {
                Array.Clear(m_tempTransformsBuffer1, 0, m_bufferSize);
                Array.Clear(m_tempTransformsBuffer2, 0, m_bufferSize);
                m_sources[indices[0]].SampleTransforms(m_tempTransformsBuffer1, model);
                m_sources[indices[1]].SampleTransforms(m_tempTransformsBuffer2, model);

                // 归一化权重
                float totalWeight = weights[0] + weights[1];
                float t = totalWeight > 0 ? weights[1] / totalWeight : 0f;
                BlendTransforms(boneTransforms, m_tempTransformsBuffer1, m_tempTransformsBuffer2, t);

                // 继续混合剩余的采样点
                for (int i = 2; i < indices.Length; i++) {
                    if (m_sources[indices[i]] == null) {
                        continue;
                    }
                    Array.Clear(m_tempTransformsBuffer1, 0, m_bufferSize);
                    m_sources[indices[i]].SampleTransforms(m_tempTransformsBuffer1, model);

                    // 计算新的混合权重（使用 for 循环替代 LINQ）
                    float prevTotal = 0f;
                    for (int j = 0; j < i; j++) {
                        prevTotal += weights[j];
                    }
                    totalWeight = prevTotal + weights[i];
                    t = totalWeight > 0 ? weights[i] / totalWeight : 0f;

                    // 复制当前结果到 buffer2
                    Array.Copy(boneTransforms, m_tempTransformsBuffer2, boneTransforms.Length);
                    BlendTransforms(boneTransforms, m_tempTransformsBuffer2, m_tempTransformsBuffer1, t);
                }
            }
        }

        /// <summary>
        /// 使用缓存的参数值进行一维混合采样
        /// </summary>
        public void SampleTransformsWithParam(Matrix?[] boneTransforms, Model model, float paramValue) {
            if (m_sources == null
                || m_sources.Length == 0
                || boneTransforms == null) {
                return;
            }
            EnsureBufferSize(boneTransforms.Length);
            if (m_is2D) {
                SampleTransforms2D(boneTransforms, model);
            }
            else {
                SampleTransforms1DWithParam(boneTransforms, model, paramValue);
            }
        }

        public void SampleTransforms1DWithParam(Matrix?[] boneTransforms, Model model, float paramValue) {
            (int lowerIdx, int upperIdx, float t) = FindBlendSamples1D(paramValue);
            if (lowerIdx < 0
                || m_sources[lowerIdx] == null) {
                return;
            }
            if (lowerIdx == upperIdx
                || t <= 0.001f) {
                m_sources[lowerIdx].SampleTransforms(boneTransforms, model);
            }
            else if (t >= 0.999f) {
                if (m_sources[upperIdx] != null) {
                    m_sources[upperIdx].SampleTransforms(boneTransforms, model);
                }
                else {
                    m_sources[lowerIdx].SampleTransforms(boneTransforms, model);
                }
            }
            else {
                Array.Clear(m_tempTransformsBuffer1, 0, m_bufferSize);
                Array.Clear(m_tempTransformsBuffer2, 0, m_bufferSize);
                m_sources[lowerIdx].SampleTransforms(m_tempTransformsBuffer1, model);
                m_sources[upperIdx].SampleTransforms(m_tempTransformsBuffer2, model);
                BlendTransforms(boneTransforms, m_tempTransformsBuffer1, m_tempTransformsBuffer2, t);
            }
        }

        /// <summary>
        /// 使用缓存的参数值进行二维混合采样
        /// </summary>
        public void SampleTransformsWithParam2D(Matrix?[] boneTransforms, Model model, float paramX, float paramY) {
            if (m_sources == null
                || m_sources.Length == 0
                || boneTransforms == null) {
                return;
            }
            EnsureBufferSize(boneTransforms.Length);
            (int[] indices, float[] weights) = FindBlendSamples2D(paramX, paramY);
            if (indices == null
                || weights == null
                || indices.Length == 0) {
                return;
            }
            if (indices.Length == 1
                && m_sources[indices[0]] != null) {
                m_sources[indices[0]].SampleTransforms(boneTransforms, model);
                return;
            }

            // 累积混合
            if (indices.Length >= 2
                && m_sources[indices[0]] != null
                && m_sources[indices[1]] != null) {
                Array.Clear(m_tempTransformsBuffer1, 0, m_bufferSize);
                Array.Clear(m_tempTransformsBuffer2, 0, m_bufferSize);
                m_sources[indices[0]].SampleTransforms(m_tempTransformsBuffer1, model);
                m_sources[indices[1]].SampleTransforms(m_tempTransformsBuffer2, model);
                float totalWeight = weights[0] + weights[1];
                float t = totalWeight > 0 ? weights[1] / totalWeight : 0f;
                BlendTransforms(boneTransforms, m_tempTransformsBuffer1, m_tempTransformsBuffer2, t);
                for (int i = 2; i < indices.Length; i++) {
                    if (m_sources[indices[i]] == null) {
                        continue;
                    }
                    Array.Clear(m_tempTransformsBuffer1, 0, m_bufferSize);
                    m_sources[indices[i]].SampleTransforms(m_tempTransformsBuffer1, model);

                    // 计算新的混合权重（使用 for 循环替代 LINQ）
                    float prevTotal = 0f;
                    for (int j = 0; j < i; j++) {
                        prevTotal += weights[j];
                    }
                    totalWeight = prevTotal + weights[i];
                    t = totalWeight > 0 ? weights[i] / totalWeight : 0f;
                    Array.Copy(boneTransforms, m_tempTransformsBuffer2, boneTransforms.Length);
                    BlendTransforms(boneTransforms, m_tempTransformsBuffer2, m_tempTransformsBuffer1, t);
                }
            }
        }

        /// <summary>
        /// 查找一维混合采样点
        /// </summary>
        public (int lowerIdx, int upperIdx, float t) FindBlendSamples1D(float value) {
            if (m_definition1D?.Samples == null
                || m_sources == null) {
                return (-1, -1, 0f);
            }
            AnimationBlendSample[] samples = m_definition1D.Samples;

            // 边界情况
            if (value <= samples[0].Value) {
                return (0, 0, 0f);
            }
            if (value >= samples[samples.Length - 1].Value) {
                return (samples.Length - 1, samples.Length - 1, 0f);
            }

            // 查找相邻采样点
            for (int i = 0; i < samples.Length - 1; i++) {
                if (value >= samples[i].Value
                    && value <= samples[i + 1].Value) {
                    float range = samples[i + 1].Value - samples[i].Value;
                    float t = range > 0.0001f ? (value - samples[i].Value) / range : 0f;
                    return (i, i + 1, t);
                }
            }
            return (0, 0, 0f);
        }

        /// <summary>
        /// 查找二维混合采样点 - 返回最近的采样点及其权重
        /// </summary>
        public (int[] indices, float[] weights) FindBlendSamples2D(float paramX, float paramY) {
            if (m_definition2D?.Samples == null
                || m_sources == null) {
                return (null, null);
            }
            AnimationBlendSample2D[] samples = m_definition2D.Samples;
            int sampleCount = samples.Length;

            // 确保缓冲区足够大
            EnsureBlend2DBuffers(sampleCount);

            // 计算到每个采样点的距离（使用预分配数组）
            for (int i = 0; i < sampleCount; i++) {
                float dx = paramX - samples[i].ValueX;
                float dy = paramY - samples[i].ValueY;
                m_blend2DDistances[i] = dx * dx + dy * dy; // 使用距离平方
            }

            // 初始化排序索引
            for (int i = 0; i < sampleCount; i++) {
                m_blend2DSortedIndices[i] = i;
            }

            // 部分排序：只找出最近的 4 个（使用简单的选择排序）
            int maxCount = Math.Min(4, sampleCount);
            for (int i = 0; i < maxCount; i++) {
                int minIdx = i;
                for (int j = i + 1; j < sampleCount; j++) {
                    if (m_blend2DDistances[m_blend2DSortedIndices[j]] < m_blend2DDistances[m_blend2DSortedIndices[minIdx]]) {
                        minIdx = j;
                    }
                }
                if (minIdx != i) {
                    int temp = m_blend2DSortedIndices[i];
                    m_blend2DSortedIndices[i] = m_blend2DSortedIndices[minIdx];
                    m_blend2DSortedIndices[minIdx] = temp;
                }
            }

            // 计算权重（距离的反比）
            float totalWeight = 0f;
            for (int i = 0; i < maxCount; i++) {
                float dist = m_blend2DDistances[m_blend2DSortedIndices[i]];
                if (dist < 0.0001f) {
                    // 非常接近某个采样点，只使用该点
                    m_blend2DWeights[i] = 1f;
                    for (int j = 0; j < i; j++) {
                        m_blend2DWeights[j] = 0f;
                    }
                    totalWeight = 1f;
                    // 返回子数组
                    return (CreateResultArray(m_blend2DSortedIndices, i + 1), CreateResultArray(m_blend2DWeights, i + 1));
                }
                m_blend2DWeights[i] = 1f / dist;
                totalWeight += m_blend2DWeights[i];
            }

            // 归一化权重
            if (totalWeight > 0) {
                for (int i = 0; i < maxCount; i++) {
                    m_blend2DWeights[i] /= totalWeight;
                }
            }

            // 返回子数组
            return (CreateResultArray(m_blend2DSortedIndices, maxCount), CreateResultArray(m_blend2DWeights, maxCount));
        }

        public void EnsureBlend2DBuffers(int requiredSize) {
            if (m_blend2DMaxSamples < requiredSize) {
                m_blend2DMaxSamples = requiredSize;
                m_blend2DDistances = new float[requiredSize];
                m_blend2DSortedIndices = new int[requiredSize];
                m_blend2DWeights = new float[requiredSize];
            }
        }

        public void AllocateBlend2DBuffers(int initialSize) {
            m_blend2DMaxSamples = initialSize;
            m_blend2DDistances = new float[initialSize];
            m_blend2DSortedIndices = new int[initialSize];
            m_blend2DWeights = new float[initialSize];
        }

        public static T[] CreateResultArray<T>(T[] source, int count) {
            T[] result = new T[count];
            Array.Copy(source, result, count);
            return result;
        }

        /// <summary>
        /// 混合两组骨骼变换
        /// </summary>
        public void BlendTransforms(Matrix?[] output, Matrix?[] a, Matrix?[] b, float t) {
            int count = Math.Min(output.Length, Math.Min(a.Length, b.Length));
            for (int i = 0; i < count; i++) {
                if (a[i].HasValue
                    && b[i].HasValue) {
                    output[i] = BlendMatrix(a[i].Value, b[i].Value, t);
                }
                else if (a[i].HasValue) {
                    output[i] = a[i].Value;
                }
                else if (b[i].HasValue) {
                    output[i] = b[i].Value;
                }
                else {
                    output[i] = null;
                }
            }
        }

        /// <summary>
        /// 混合两个矩阵（分解为 T、R、S 分别插值）
        /// </summary>
        public Matrix BlendMatrix(Matrix a, Matrix b, float t) {
            DecomposeMatrix(a, out Vector3 tA, out Quaternion rA, out Vector3 sA);
            DecomposeMatrix(b, out Vector3 tB, out Quaternion rB, out Vector3 sB);
            return Matrix.CreateScale(Vector3.Lerp(sA, sB, t))
                * Matrix.CreateFromQuaternion(Quaternion.Slerp(rA, rB, t))
                * Matrix.CreateTranslation(Vector3.Lerp(tA, tB, t));
        }

        public void DecomposeMatrix(Matrix m, out Vector3 translation, out Quaternion rotation, out Vector3 scale) {
            translation = m.Translation;
            Vector3 right = new(m.M11, m.M12, m.M13);
            Vector3 up = new(m.M21, m.M22, m.M23);
            Vector3 forward = new(m.M31, m.M32, m.M33);
            float scaleX = right.Length();
            float scaleY = up.Length();
            float scaleZ = forward.Length();
            scale = new Vector3(scaleX, scaleY, scaleZ);
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
            if (scaleX * scaleY * scaleZ < 0) {
                scale = -scale;
            }
        }

        public void EnsureBufferSize(int requiredSize) {
            if (m_bufferSize < requiredSize) {
                m_bufferSize = requiredSize;
                m_tempTransformsBuffer1 = new Matrix?[m_bufferSize];
                m_tempTransformsBuffer2 = new Matrix?[m_bufferSize];
            }
        }

        /// <summary>
        /// 获取当前混合参数值（用于调试）
        /// </summary>
        public float GetCurrentParameterValue(AnimationParameters parameters) {
            if (!m_is2D
                && m_definition1D != null
                && !string.IsNullOrEmpty(m_definition1D.ParameterName)) {
                return parameters?.GetFloat(m_definition1D.ParameterName) ?? 0f;
            }
            return 0f;
        }

        /// <summary>
        /// 获取当前二维混合参数值（用于调试）
        /// </summary>
        public (float x, float y) GetCurrentParameterValues2D(AnimationParameters parameters) {
            if (m_is2D && m_definition2D != null) {
                float x = !string.IsNullOrEmpty(m_definition2D.ParameterNameX) ? parameters?.GetFloat(m_definition2D.ParameterNameX) ?? 0f : 0f;
                float y = !string.IsNullOrEmpty(m_definition2D.ParameterNameY) ? parameters?.GetFloat(m_definition2D.ParameterNameY) ?? 0f : 0f;
                return (x, y);
            }
            return (0f, 0f);
        }

        /// <summary>
        /// 获取同步的归一化时间
        /// </summary>
        public float GetSyncedNormalizedTime() => m_syncedNormalizedTime;

        /// <summary>
        /// 设置所有动画的归一化时间
        /// </summary>
        public void SetNormalizedTime(float normalizedTime) {
            m_syncedNormalizedTime = normalizedTime;
            foreach (ClipAnimationSource source in m_sources) {
                source?.Player?.SetNormalizedTime(normalizedTime);
            }
        }
    }
}