using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// 两骨骼 IK 解析解算法
    /// 适用于手臂、腿部等典型情况
    /// </summary>
    public class TwoBoneIK : IIKAlgorithm {
        public string Name => "TwoBoneIK";
        public bool SupportsAim => true;

        public void Solve(IKChain chain,
            IKTarget target,
            Matrix?[] boneTransforms,
            Vector3[] worldPositions,
            Model model,
            IKAlgorithmConfig config = null) {
            if (chain == null
                || chain.Length < 2) {
                return;
            }

            // 链长度为 2 的情况由 SingleBoneIK 处理
            if (chain.Length == 2) {
                return;
            }

            // 链长度 > 2 时，必须有 Position
            if (!target.Position.HasValue) {
                return;
            }
            config ??= new IKAlgorithmConfig();
            int[] indices = chain.BoneIndices;
            int rootIdx = indices[0];
            int midIdx = indices[1];
            int endIdx = indices[indices.Length - 1];

            // 获取骨骼位置
            Vector3 rootPos = worldPositions[rootIdx];
            Vector3 midPos = worldPositions[midIdx];
            Vector3 endPos = worldPositions[endIdx];

            // 计算骨骼长度
            float len1 = Vector3.Distance(rootPos, midPos);
            float len2 = Vector3.Distance(midPos, endPos);
            if (len1 < 0.0001f
                || len2 < 0.0001f) {
                return;
            }

            // 目标位置
            Vector3 targetPos = target.Position.Value;

            // 计算目标距离
            float targetDist = Vector3.Distance(rootPos, targetPos);

            // 计算骨骼链总长度
            float totalLen = len1 + len2;

            // 计算根骨骼到目标的方向
            Vector3 toTarget = targetPos - rootPos;
            Vector3 toTargetDir = toTarget.LengthSquared() > 0.0001f ? Vector3.Normalize(toTarget) : Vector3.UnitY;

            // 计算中间骨骼新位置（余弦定理）
            Vector3 newMidPos;
            if (targetDist >= totalLen) {
                // 目标超出骨骼链长度：完全伸展
                newMidPos = rootPos + toTargetDir * len1;
            }
            else if (targetDist <= MathF.Abs(len1 - len2)) {
                // 目标太近：折叠
                newMidPos = rootPos + toTargetDir * len1 * 0.5f;
            }
            else {
                // 使用余弦定理计算中间骨骼位置
                // cos(B) = (a² + c² - b²) / (2ac)
                // 其中 a = len1, b = len2, c = targetDist
                float cosB = (len1 * len1 + targetDist * targetDist - len2 * len2) / (2f * len1 * targetDist);
                cosB = Math.Clamp(cosB, -1f, 1f);
                float angleB = MathF.Acos(cosB);

                // 计算中间骨骼相对目标的位置
                // 需要考虑弯曲方向（Hint）
                Vector3 bendDir = CalculateBendDirection(rootPos, worldPositions[midIdx], targetPos, target.Hint);

                // 构建中间骨骼位置
                // 使用向量旋转计算
                float midDist = len1 * MathF.Cos(angleB);
                float bendOffset = len1 * MathF.Sin(angleB);

                // 从根到目标的方向
                Vector3 forward = toTargetDir;

                // 弯曲方向的垂直分量
                Vector3 bendPerpendicular = Vector3.Cross(forward, bendDir);
                if (bendPerpendicular.LengthSquared() < 0.0001f) {
                    bendPerpendicular = Vector3.Cross(forward, Vector3.UnitY);
                    if (bendPerpendicular.LengthSquared() < 0.0001f) {
                        bendPerpendicular = Vector3.Cross(forward, Vector3.UnitX);
                    }
                }
                bendPerpendicular = Vector3.Normalize(bendPerpendicular);

                // 计算新的中间位置
                newMidPos = rootPos + forward * midDist + bendPerpendicular * bendOffset;
            }

            // 根骨骼旋转：从原方向到新方向
            Vector3 oldRootDiff = midPos - rootPos;
            Vector3 newRootDiff = newMidPos - rootPos;
            if (oldRootDiff.LengthSquared() < 0.0001f
                || newRootDiff.LengthSquared() < 0.0001f) {
                return;
            }
            Vector3 oldRootDir = Vector3.Normalize(oldRootDiff);
            Vector3 newRootDir = Vector3.Normalize(newRootDiff);
            Quaternion rootRotation = IKUtils.RotationBetweenVectors(oldRootDir, newRootDir);

            // 转换模型空间旋转到骨骼局部空间
            IKUtils.ApplyModelRotation(boneTransforms, rootIdx, rootRotation, rootPos, model);

            // 中间骨骼旋转：根骨骼旋转已改变 mid→end 方向，必须用旋转后的方向
            Vector3 newMidWorld = Vector3.Transform(midPos - rootPos, rootRotation) + rootPos;
            Vector3 oldMidDir = Vector3.Normalize(Vector3.Transform(endPos - midPos, rootRotation));
            Vector3 newMidDir = Vector3.Normalize(targetPos - newMidWorld);
            Quaternion midRotation = IKUtils.RotationBetweenVectors(oldMidDir, newMidDir);

            IKUtils.ApplyModelRotation(boneTransforms, midIdx, midRotation, newMidWorld, model);

            ApplyJointLimits(chain, boneTransforms, model);

            if (target.AimDirection.HasValue && SupportsAim) {
                ApplyAimConstraint(chain, target, boneTransforms, worldPositions, model, indices);
            }
        }

        /// <summary>
        /// 计算弯曲方向
        /// </summary>
        public Vector3 CalculateBendDirection(Vector3 root, Vector3 mid, Vector3 target, Vector3? hint) {
            if (hint.HasValue) {
                return hint.Value;
            }

            // 用原始中间骨骼位置推导弯曲方向
            Vector3 rootToMid = mid - root;
            Vector3 rootToTarget = target - root;

            Vector3 bendDir = Vector3.Cross(rootToMid, rootToTarget);
            if (bendDir.LengthSquared() < 0.0001f) {
                bendDir = Vector3.Cross(rootToTarget, Vector3.UnitY);
                if (bendDir.LengthSquared() < 0.0001f) {
                    bendDir = Vector3.Cross(rootToTarget, Vector3.UnitX);
                }
            }
            return Vector3.Normalize(bendDir);
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

        /// <summary>
        /// 应用瞄准约束（方向约束）
        /// </summary>
        public void ApplyAimConstraint(IKChain chain,
            IKTarget target,
            Matrix?[] boneTransforms,
            Vector3[] worldPositions,
            Model model,
            int[] indices) {
            if (!target.AimDirection.HasValue) {
                return;
            }
            int endIdx = indices[indices.Length - 1];

            // 获取瞄准轴
            Vector3 aimAxis = chain.AimAxis;

            // 目标方向
            Vector3 targetDir = Vector3.Normalize(target.AimDirection.Value);

            // 计算当前末端骨骼的朝向
            // 使用末端骨骼的世界变换计算当前 AimAxis 方向
            Matrix endWorldTransform = IKUtils.ComputeBoneWorldTransform(boneTransforms, endIdx, model);
            Vector3 currentAimDir = Vector3.Normalize(Vector3.TransformNormal(aimAxis, endWorldTransform));

            // 计算旋转
            Quaternion aimRotation = IKUtils.RotationBetweenVectors(currentAimDir, targetDir);

            // 应用权重
            if (target.AimWeight < 1.0f) {
                aimRotation = Quaternion.Slerp(Quaternion.Identity, aimRotation, target.AimWeight);
            }

            IKUtils.ApplyModelRotation(boneTransforms, endIdx, aimRotation, worldPositions[endIdx], model);
        }
    }
}
