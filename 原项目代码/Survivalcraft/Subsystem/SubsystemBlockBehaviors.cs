using System.Reflection;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemBlockBehaviors : Subsystem {
        public SubsystemBlockBehavior[][] m_blockBehaviorsByContents;

        public List<SubsystemBlockBehavior> m_blockBehaviors = [];

        public ReadOnlyList<SubsystemBlockBehavior> BlockBehaviors => new(m_blockBehaviors);

        public SubsystemBlockBehavior[] GetBlockBehaviors(int contents) => m_blockBehaviorsByContents[contents];

        public override void Load(ValuesDictionary valuesDictionary) {
            m_blockBehaviorsByContents = new SubsystemBlockBehavior[BlocksManager.Blocks.Length][];
            Dictionary<int, HashSet<SubsystemBlockBehavior>> dictionary = [];
            for (int i = 0; i < m_blockBehaviorsByContents.Length; i++) {
                dictionary[i] = [];
                string[] array = BlocksManager.Blocks[i].Behaviors.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (string text in array) {
                    SubsystemBlockBehavior item = Project.FindSubsystem<SubsystemBlockBehavior>(text.Trim(), true);
                    dictionary[i].Add(item);
                }
            }
            foreach (SubsystemBlockBehavior item2 in Project.FindSubsystems<SubsystemBlockBehavior>()) {
                m_blockBehaviors.Add(item2);
                int[] handledBlocks = item2.HandledBlocks;
                foreach (int key in handledBlocks) {
                    dictionary[key].Add(item2);
                }
            }
            for (int k = 0; k < m_blockBehaviorsByContents.Length; k++) {
                SubsystemBlockBehavior[] behaviors = dictionary[k].ToArray();
                m_blockBehaviorsByContents[k] = behaviors;
                // 聚合：任一 behavior override 了 OnUse（DeclaringType 非基类）→ 该方块可 Use（GetPriorityUse 返 PriorityUse）。
                // 反射自动识别（含旧模组 override OnUse 的 behavior），无需子类显式声明 HasUseAction，也不依赖 BlocksData 列。
                // 假定所有 behavior 在本 Load 前注册（SC mod 加载时序固定 behavior 列表于 Load 前）；Load 后动态注册的 behavior 不被识别。
                foreach (SubsystemBlockBehavior behavior in behaviors) {
                    MethodInfo onUse = behavior.GetType().GetMethod(nameof(SubsystemBlockBehavior.OnUse));
                    if (onUse != null && onUse.DeclaringType != typeof(SubsystemBlockBehavior)) {
                        BlocksManager.Blocks[k].DefaultIsUseable = true;
                        break;
                    }
                }
            }
        }
    }
}