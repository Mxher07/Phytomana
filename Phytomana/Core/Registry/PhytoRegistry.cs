using System;
using System.Collections.Generic;
using System.Reflection;
using Engine;
using Game;

namespace Phytomana {
    /// <summary>
    /// Phytomana 集中注册表。方块/物品的登记由引擎完成（方块表数据 + 程序集反射发现），
    /// 本类在方块初始化完成后统一收集本模组的方块、方块值与花朵节点类型映射，
    /// 对外提供单一查询入口，替代散落各处的 GetBlockIndex 调用。
    /// </summary>
    public static class PhytoRegistry {
        static readonly Dictionary<int, Block> m_blocks = [];

        static readonly Dictionary<int, Type> m_flowerNodeTypes = [];

        static readonly List<int> m_flowerBlockIndices = [];

        /// <summary>
        /// 注册表是否已构建（BlocksInitalized 之后为 true）。
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// 本模组全部方块（含以方块形式存在的物品）的数量。
        /// </summary>
        public static int BlockCount => m_blocks.Count;

        /// <summary>
        /// 全部花朵方块的方块值列表。
        /// </summary>
        public static IReadOnlyList<int> FlowerBlockIndices => m_flowerBlockIndices;

        /// <summary>
        /// 由 PhytomanaMod 在 BlocksInitalized 时机调用：扫描本程序集的方块类型并登记。
        /// </summary>
        internal static void Initialize() {
            m_blocks.Clear();
            m_flowerNodeTypes.Clear();
            m_flowerBlockIndices.Clear();
            Assembly self = typeof(PhytoRegistry).Assembly;
            Block[] blocks = BlocksManager.Blocks;
            for (int i = 0; i < blocks.Length; i++) {
                Block block = blocks[i];
                if (block == null || block.GetType().Assembly != self) {
                    continue;
                }
                m_blocks[i] = block;
                if (block is IPhytoFlowerBlock flowerBlock) {
                    m_flowerBlockIndices.Add(i);
                    try {
                        TilePhytoFlower flower = flowerBlock.CreateFlower(default);
                        if (flower != null) {
                            m_flowerNodeTypes[i] = flower.GetType();
                        }
                    }
                    catch (Exception e) {
                        Log.Error($"[PhytoMana]Registry: failed to probe flower node type of block {block.GetType().Name}: {e}");
                    }
                }
            }
            IsInitialized = true;
            Log.Information($"[PhytoMana]Registry: {m_blocks.Count} blocks registered, {m_flowerBlockIndices.Count} of them are flowers.");
        }

        public static bool IsPhytoBlock(int contents) => m_blocks.ContainsKey(contents);

        public static bool TryGetBlock(int contents, out Block block) => m_blocks.TryGetValue(contents, out block);

        public static bool IsFlowerBlock(int contents) => m_flowerNodeTypes.ContainsKey(contents);

        /// <summary>
        /// 查询花朵方块对应的节点类型（如 TileSunPowerFlower）。
        /// </summary>
        public static bool TryGetFlowerNodeType(int contents, out Type nodeType) => m_flowerNodeTypes.TryGetValue(contents, out nodeType);
    }
}
