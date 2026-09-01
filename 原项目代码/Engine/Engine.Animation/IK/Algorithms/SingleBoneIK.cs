using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// 单骨骼 IK 算法
    /// 适用于链长度为 2 的情况（如脖子-头部）
    /// </summary>
    public class SingleBoneIK : IIKAlgorithm {
        public string Name => "SingleBoneIK";
        public bool SupportsAim => true;

        public void Solve(IKChain chain,
            IKTarget target,
            Matrix?[] boneTransforms,
            Vector3[] worldPositions,
            Model model,
            IKAlgorithmConfig config = null) // config 未使用，单骨骼 IK 无额外配置项
        {
            if (chain == null
                || chain.Length != 2) {
                return;
            }
            if (!target.AimDirection.HasValue
                && !target.Position.HasValue) {
                return;
            }
            int[] indices = chain.BoneIndices;
            int rootIdx = indices[0];
            int endIdx = indices[1];
            Vector3 rootPos = worldPositions[rootIdx];
            SolveSingleBone(
                chain,
                target,
                boneTransforms,
                worldPositions,
                rootIdx,
                endIdx,
                rootPos,
                model
            );
        }

        /// <summary>
        /// 单骨骼 IK：旋转骨骼链让 AimAxis 朝向目标方向
        /// </summary>
        public void SolveSingleBone(IKChain chain,
            IKTarget target,
            Matrix?[] boneTransforms,
            Vector3[] worldPositions,
            int rootIdx,
            int endIdx,
            Vector3 rootPos,
            Model model) {
            // 目标方向（模型空间）
            Vector3 targetDir;
            if (target.AimDirection.HasValue) {
                Vector3 dir = target.AimDirection.Value;
                if (dir.LengthSquared() < 0.0001f) {
                    return;
                }
                targetDir = Vector3.Normalize(dir);
            }
            else if (target.Position.HasValue) {
                Vector3 toTarget = target.Position.Value - rootPos;
                if (toTarget.LengthSquared() < 0.0001f) {
                    return;
                }
                targetDir = Vector3.Normalize(toTarget);
            }
            else {
                return;
            }

            // 始终使用末端骨骼的世界变换来计算当前 AimAxis 方向
            Matrix endWorldTransform = IKUtils.ComputeBoneWorldTransform(boneTransforms, endIdx, model);
            Vector3 currentAimDir = Vector3.Normalize(Vector3.TransformNormal(chain.AimAxis, endWorldTransform));

            // 计算让 AimAxis 指向目标所需的旋转（模型空间）
            Quaternion modelRotation = IKUtils.RotationBetweenVectors(currentAimDir, targetDir);

            // 应用权重
            float weight = target.AimWeight;
            if (weight > 0f
                && weight < 1.0f) {
                modelRotation = Quaternion.Slerp(Quaternion.Identity, modelRotation, weight);
            }

            // 对于两骨骼链，旋转应用到根骨骼
            // 使用 ApplyModelRotation 保持骨骼世界位置不变
            int targetBoneIdx = rootIdx;

            IKUtils.ApplyModelRotation(boneTransforms, targetBoneIdx, modelRotation, rootPos, model);

            // 应用关节限制
            ApplyJointLimits(chain, boneTransforms, model);
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