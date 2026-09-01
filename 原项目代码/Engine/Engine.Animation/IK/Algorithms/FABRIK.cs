using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// FABRIK (Forward And Backward Reaching Inverse Kinematics) 算法
    /// 收敛快，结果更自然
    /// </summary>
    public class FABRIK : IIKAlgorithm {
        public string Name => "FABRIK";
        public bool SupportsAim => false;

        // 位置缓存
        public Vector3[] m_positionsCache;

        // 骨骼长度缓存
        public float[] m_boneLengthsCache;

        public void Solve(IKChain chain,
            IKTarget target,
            Matrix?[] boneTransforms,
            Vector3[] worldPositions,
            Model model,
            IKAlgorithmConfig config = null) {
            if (chain == null
                || chain.Length < 2
                || !target.Position.HasValue) {
                return;
            }
            config ??= new IKAlgorithmConfig();
            int maxIterations = config.MaxIterations;
            float tolerance = config.Tolerance;
            int[] indices = chain.BoneIndices;
            int n = indices.Length;
            Vector3 targetPos = target.Position.Value;
            Vector3 rootPos = worldPositions[indices[0]];

            // 确保缓存足够大
            if (m_positionsCache == null
                || m_positionsCache.Length < n) {
                m_positionsCache = new Vector3[n];
            }
            if (m_boneLengthsCache == null
                || m_boneLengthsCache.Length < n - 1) {
                m_boneLengthsCache = new float[n - 1];
            }

            // 计算骨骼长度
            for (int i = 0; i < n - 1; i++) {
                m_boneLengthsCache[i] = Vector3.Distance(worldPositions[indices[i]], worldPositions[indices[i + 1]]);
            }

            // 计算总链长度
            float totalLength = 0;
            for (int i = 0; i < n - 1; i++) {
                totalLength += m_boneLengthsCache[i];
            }

            // 检查目标是否可达
            float distToTarget = Vector3.Distance(rootPos, targetPos);

            // 初始化位置数组
            for (int i = 0; i < n; i++) {
                m_positionsCache[i] = worldPositions[indices[i]];
            }

            // 如果目标不可达，伸展到最大
            if (distToTarget > totalLength) {
                Vector3 dir = Vector3.Normalize(targetPos - rootPos);
                for (int i = 1; i < n; i++) {
                    m_positionsCache[i] = m_positionsCache[i - 1] + dir * m_boneLengthsCache[i - 1];
                }
            }
            else {
                // FABRIK 迭代
                for (int iter = 0; iter < maxIterations; iter++) {
                    // 前向阶段：从末端向根
                    m_positionsCache[n - 1] = targetPos;
                    for (int i = n - 2; i >= 0; i--) {
                        Vector3 diff = m_positionsCache[i] - m_positionsCache[i + 1];
                        float dist = diff.Length();
                        if (dist < 0.0001f) {
                            m_positionsCache[i] = m_positionsCache[i + 1];
                        }
                        else {
                            Vector3 dir = diff / dist;
                            m_positionsCache[i] = m_positionsCache[i + 1] + dir * m_boneLengthsCache[i];
                        }
                    }

                    // 后向阶段：从根向末端
                    m_positionsCache[0] = rootPos;
                    for (int i = 1; i < n; i++) {
                        Vector3 diff = m_positionsCache[i] - m_positionsCache[i - 1];
                        float dist = diff.Length();
                        if (dist < 0.0001f) {
                            m_positionsCache[i] = m_positionsCache[i - 1];
                        }
                        else {
                            Vector3 dir = diff / dist;
                            m_positionsCache[i] = m_positionsCache[i - 1] + dir * m_boneLengthsCache[i - 1];
                        }
                    }

                    // 检查收敛
                    float error = Vector3.Distance(m_positionsCache[n - 1], targetPos);
                    if (error < tolerance) {
                        break;
                    }
                }
            }

            // 从位置计算骨骼旋转
            CalculateBoneRotations(chain, m_positionsCache, boneTransforms, worldPositions, model);

            // 应用关节限制
            ApplyJointLimits(chain, boneTransforms, model);
        }

        /// <summary>
        /// 从位置数组计算骨骼旋转
        /// </summary>
        public void CalculateBoneRotations(IKChain chain, Vector3[] positions, Matrix?[] boneTransforms, Vector3[] worldPositions, Model model) {
            int[] indices = chain.BoneIndices;
            int n = indices.Length;
            // 累积旋转：父骨骼旋转会传播到子骨骼，必须追踪
            Quaternion cumulativeRotation = Quaternion.Identity;
            for (int i = 0; i < n - 1; i++) {
                int boneIdx = indices[i];

                // 原始方向，经累积旋转变换后的当前实际方向
                Vector3 origDiff = worldPositions[indices[i + 1]] - worldPositions[boneIdx];
                if (origDiff.LengthSquared() < 0.0001f) {
                    continue;
                }
                Vector3 oldDir = Vector3.Normalize(Vector3.Transform(origDiff, cumulativeRotation));

                // 新方向（FABRIK 计算的目标位置）
                Vector3 newDiff = positions[i + 1] - positions[i];
                if (newDiff.LengthSquared() < 0.0001f) {
                    continue;
                }
                Vector3 newDir = Vector3.Normalize(newDiff);

                // 计算旋转
                Quaternion rotation = IKUtils.RotationBetweenVectors(oldDir, newDir);

                // 累积旋转（新旋转在最外层）
                cumulativeRotation = rotation * cumulativeRotation;

                // 使用 ApplyModelRotation 保持骨骼世界位置不变
                IKUtils.ApplyModelRotation(boneTransforms, boneIdx, rotation, positions[i], model);
            }
        }

        /// <summary>
        /// 应用关节限制
        /// </summary>
        public void ApplyJointLimits(IKChain chain, Matrix?[] boneTransforms, Model model) {
            if (chain.JointLimits == null
                || model == null) {
                return;
            }
            foreach (int boneIdx in chain.BoneIndices) {
                JointLimit limit = chain.GetJointLimit(boneIdx, model);
                if (limit != null
                    && boneTransforms[boneIdx].HasValue) {
                    Matrix transform = boneTransforms[boneIdx].Value;
                    boneTransforms[boneIdx] = limit.ApplyLimit(transform);
                }
            }
        }
    }
}