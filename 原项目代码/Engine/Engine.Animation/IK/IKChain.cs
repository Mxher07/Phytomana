using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// IK 骨骼链定义
    /// </summary>
    public class IKChain {
        /// <summary>
        /// 链名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 骨骼索引数组（从根到末端的顺序）
        /// </summary>
        public int[] BoneIndices { get; }

        /// <summary>
        /// 末端骨骼名称
        /// </summary>
        public string EndBoneName { get; }

        /// <summary>
        /// 使用的 IK 算法
        /// </summary>
        public IIKAlgorithm Algorithm { get; set; }

        /// <summary>
        /// 末端骨骼的"前方"轴（骨骼局部空间）
        /// </summary>
        public Vector3 AimAxis { get; set; } = Vector3.UnitZ;

        /// <summary>
        /// 目标不可达时的回退策略
        /// </summary>
        public UnreachableStrategy UnreachableStrategy { get; set; } = UnreachableStrategy.ExtendTowardTarget;

        /// <summary>
        /// 过渡期间是否应用 IK
        /// </summary>
        public bool ApplyDuringTransition { get; set; } = true;

        /// <summary>
        /// 关节限制（按骨骼名称索引）
        /// </summary>
        public Dictionary<string, JointLimit> JointLimits { get; set; }

        /// <summary>
        /// 算法配置参数
        /// </summary>
        public IKAlgorithmConfig AlgorithmConfig { get; set; }

        /// <summary>
        /// 创建 IK 链
        /// </summary>
        public IKChain(string name, int[] boneIndices, string endBoneName) {
            Name = name;
            BoneIndices = boneIndices;
            EndBoneName = endBoneName;
        }

        /// <summary>
        /// 链长度（骨骼数量）
        /// </summary>
        public int Length => BoneIndices?.Length ?? 0;

        /// <summary>
        /// 获取骨骼的关节限制
        /// </summary>
        public JointLimit GetJointLimit(int boneIndex, Model model) {
            if (JointLimits == null
                || model == null) {
                return null;
            }
            ModelBone bone = model.m_bones[boneIndex];
            if (bone == null) {
                return null;
            }
            return JointLimits.TryGetValue(bone.Name, out JointLimit limit) ? limit : null;
        }

        /// <summary>
        /// 设置关节限制
        /// </summary>
        public void SetJointLimit(string boneName, JointLimit limit) {
            JointLimits ??= new Dictionary<string, JointLimit>();
            JointLimits[boneName] = limit;
        }
    }
}