namespace Engine.Graphics {
    /// <summary>
    /// 蒙皮数据，存储骨骼索引和权重
    /// </summary>
    public class ModelSkin {
        /// <summary>
        /// 关节骨骼索引列表（加载阶段使用）
        /// </summary>
        public int[] JointIndices { get; set; }

        /// <summary>
        /// 关节骨骼列表（运行时，由 Model.Initialize 填充）
        /// </summary>
        public List<ModelBone> Joints { get; set; } = [];

        /// <summary>
        /// 逆绑定矩阵，每个关节一个
        /// </summary>
        public Matrix[] InverseBindMatrices { get; set; }

        /// <summary>
        /// 根骨骼索引（加载阶段使用）
        /// </summary>
        public int SkeletonRootIndex { get; set; } = -1;

        /// <summary>
        /// 根骨骼（运行时，由 Model.Initialize 填充）
        /// </summary>
        public ModelBone SkeletonRoot { get; set; }

        /// <summary>
        /// 关节数量（始终基于 JointIndices 或 InverseBindMatrices 的长度）
        /// </summary>
        public int JointCount => JointIndices?.Length ?? InverseBindMatrices?.Length ?? 0;

        /// <summary>
        /// 解析运行时骨骼引用（由 Model.Initialize 调用）
        /// </summary>
        /// <param name="bones">模型的骨骼列表</param>
        /// <exception cref="InvalidOperationException">当关节索引无效时抛出</exception>
        public void ResolveJoints(List<ModelBone> bones) {
            if (JointIndices == null
                || bones == null) {
                return;
            }
            Joints.Clear();
            for (int i = 0; i < JointIndices.Length; i++) {
                int index = JointIndices[i];
                if (index < 0
                    || index >= bones.Count) {
                    throw new InvalidOperationException($"Invalid joint index {index} at position {i}. Valid range: 0-{bones.Count - 1}");
                }
                Joints.Add(bones[index]);
            }

            // 解析根骨骼
            if (SkeletonRootIndex >= 0
                && SkeletonRootIndex < bones.Count) {
                SkeletonRoot = bones[SkeletonRootIndex];
            }
        }

        /// <summary>
        /// 验证蒙皮数据是否有效
        /// </summary>
        public bool IsValid() => JointCount > 0 && Joints.Count == JointCount && InverseBindMatrices?.Length == JointCount;
    }
}