using System.Numerics;
using Engine.Graphics;
using SharpGLTF.Schema2;

namespace Engine.Media {
    public static class GltfBoneConverter {
        public static void ConvertBones(ModelRoot modelRoot, ModelData modelData, List<Node> allNodes, Dictionary<Node, int> nodeToIndex) {
            // 创建临时映射：Node -> 临时索引
            Dictionary<Node, int> nodeToTempIndex = new();
            for (int i = 0; i < allNodes.Count; i++) {
                nodeToTempIndex[allNodes[i]] = i;
            }

            // 找出所有根节点（没有视觉父节点的节点）
            List<int> rootIndices = new();
            for (int i = 0; i < allNodes.Count; i++) {
                if (allNodes[i].VisualParent == null) {
                    rootIndices.Add(i);
                }
            }

            // 检查是否需要创建虚拟根骨骼
            bool needVirtualRoot = rootIndices.Count > 1;
            int virtualRootIndex = -1;
            int totalBoneCount = allNodes.Count + (needVirtualRoot ? 1 : 0);

            // 创建临时骨骼数据数组
            ModelBoneData[] tempBones = new ModelBoneData[totalBoneCount];
            int boneOffset = needVirtualRoot ? 1 : 0;

            // 如果需要虚拟根骨骼，创建它
            if (needVirtualRoot) {
                tempBones[0] = new ModelBoneData { Name = "Root", Transform = Matrix.Identity, ParentBoneIndex = -1 };
                virtualRootIndex = 0;
            }

            // 创建节点对应的骨骼数据
            for (int i = 0; i < allNodes.Count; i++) {
                Node node = allNodes[i];
                int boneIndex = i + boneOffset;
                tempBones[boneIndex] = new ModelBoneData {
                    Name = node.Name ?? $"Node{node.LogicalIndex}", Transform = node.LocalMatrix, ParentBoneIndex = -1 // 先设为 -1，后面再更新
                };
            }

            // 设置父骨骼索引（使用临时索引）
            for (int i = 0; i < allNodes.Count; i++) {
                Node node = allNodes[i];
                int boneIndex = i + boneOffset;
                Node parent = node.VisualParent;
                if (parent != null
                    && nodeToTempIndex.TryGetValue(parent, out int parentIndex)) {
                    tempBones[boneIndex].ParentBoneIndex = parentIndex + boneOffset;
                }
                else if (needVirtualRoot) {
                    // 没有父节点的节点，设置为虚拟根骨骼的子节点
                    tempBones[boneIndex].ParentBoneIndex = virtualRootIndex;
                }
            }

            // 拓扑排序：确保父骨骼在子骨骼之前
            // 计算每个骨骼的深度，按深度排序
            int[] depths = new int[totalBoneCount];
            for (int i = 0; i < totalBoneCount; i++) {
                depths[i] = CalculateDepth(i, tempBones);
            }

            // 创建排序映射：旧索引 -> 新索引
            int[] oldToNew = new int[totalBoneCount];
            List<int> sortedIndices = new();
            for (int i = 0; i < totalBoneCount; i++) {
                sortedIndices.Add(i);
            }
            sortedIndices.Sort((a, b) => depths[a].CompareTo(depths[b]));
            for (int newIndex = 0; newIndex < sortedIndices.Count; newIndex++) {
                oldToNew[sortedIndices[newIndex]] = newIndex;
            }

            // 按排序顺序添加骨骼，并更新父索引
            foreach (int oldIndex in sortedIndices) {
                ModelBoneData bone = tempBones[oldIndex];
                if (bone.ParentBoneIndex >= 0) {
                    bone.ParentBoneIndex = oldToNew[bone.ParentBoneIndex];
                }
                modelData.Bones.Add(bone);
            }

            // 更新 nodeToIndex 映射（用于后续的蒙皮和网格处理）
            nodeToIndex.Clear();
            for (int i = 0; i < allNodes.Count; i++) {
                int oldIndex = i + boneOffset;
                Node node = allNodes[i];
                nodeToIndex[node] = oldToNew[oldIndex];
            }

            // 如果没有节点，创建一个默认根节点
            if (modelData.Bones.Count == 0) {
                modelData.Bones.Add(new ModelBoneData { Name = "Root", ParentBoneIndex = -1, Transform = Matrix.Identity });
            }
        }

        public static int CalculateDepth(int boneIndex, ModelBoneData[] bones) {
            int depth = 0;
            int current = boneIndex;
            while (bones[current].ParentBoneIndex >= 0
                && depth < bones.Length) {
                current = bones[current].ParentBoneIndex;
                depth++;
            }
            return depth;
        }

        public static void ConvertSkins(ModelRoot modelRoot, ModelData modelData, Dictionary<Node, int> nodeToIndex) {
            // 查找第一个有 Skin 的节点
            // 注意：glTF 允许一个模型有多个 Skin（用于不同的网格），但大多数模型只有一个
            // 这是一个简化处理，未来可以扩展为支持多个 Skin
            Skin firstSkin = null;
            foreach (Node node in modelRoot.LogicalNodes) {
                if (node.Skin != null) {
                    firstSkin = node.Skin;
                    break;
                }
            }
            if (firstSkin == null) {
                return; // 没有蒙皮数据
            }

            // 提取关节索引
            IReadOnlyList<Node> skinJoints = firstSkin.Joints;
            int jointCount = skinJoints.Count;
            int[] jointIndices = new int[jointCount];
            for (int i = 0; i < jointCount; i++) {
                Node joint = skinJoints[i];
                jointIndices[i] = nodeToIndex.TryGetValue(joint, out int idx) ? idx : 0;
            }

            // 提取逆绑定矩阵
            IReadOnlyList<Matrix4x4> inverseBindMatrices = firstSkin.InverseBindMatrices;
            Matrix[] ibm = new Matrix[jointCount];
            for (int i = 0; i < jointCount; i++) {
                if (i < inverseBindMatrices.Count) {
                    ibm[i] = inverseBindMatrices[i];
                }
                else {
                    ibm[i] = Matrix.Identity;
                }
            }

            // 获取骨架根节点索引
            int skeletonRootIndex = -1;
            if (firstSkin.Skeleton != null
                && nodeToIndex.TryGetValue(firstSkin.Skeleton, out int rootIdx)) {
                skeletonRootIndex = rootIdx;
            }
            modelData.Skin = new ModelSkin { JointIndices = jointIndices, InverseBindMatrices = ibm, SkeletonRootIndex = skeletonRootIndex };
        }
    }
}