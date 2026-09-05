using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using Engine;
using Game;

namespace Phytomana {
    /// <summary>
    /// 单个魔力方块的声明：能存多少魔力、是否可作为魔力传递目标、法杖可否检查其状态。
    /// </summary>
    public class ManaBlockDefinition {
        /// <summary>方块类名（与 BlocksManager 的类型名一致）。</summary>
        public string BlockName;

        /// <summary>解析出的方块值；未知方块为 -1。</summary>
        public int Contents = -1;

        /// <summary>魔力存储上限（存储魔力）。</summary>
        public float MaxMana;

        /// <summary>是否可被传递：可作为法杖链路/魔力传递的目标（是否可被传递）。</summary>
        public bool CanTransfer;

        /// <summary>法杖可否检查其状态（可被法杖检查状态）。</summary>
        public bool StaffCheckable;
    }

    /// <summary>
    /// 魔力方块注册表。由外部数据文件 <c>Assets/Phytomana/ManaBlocks.xml</c> 驱动，
    /// 集中声明每个方块的「存储魔力 / 是否可被传递 / 可被法杖检查状态」三项属性，
    /// 避免把这些数值硬编码进各子系统。文件缺失或条目缺失时，调用方回退到既有默认行为。
    /// </summary>
    public static class ManaBlockRegistry {
        public const string ContentPath = "Phytomana/ManaBlocks";

        static readonly Dictionary<int, ManaBlockDefinition> m_byContents = [];
        static readonly Dictionary<string, ManaBlockDefinition> m_byName = [];

        public static bool IsInitialized { get; private set; }

        public static int Count => m_byContents.Count;

        /// <summary>由 PhytomanaMod 在 BlocksInitalized 时机调用（此时方块索引已就绪）。</summary>
        internal static void Initialize() {
            m_byContents.Clear();
            m_byName.Clear();
            XElement root = ContentManager.Get<XElement>(ContentPath, null, false);
            if (root == null) {
                IsInitialized = true;
                Log.Warning("[PhytoMana]ManaBlockRegistry: ManaBlocks.xml not found, falling back to built-in defaults.");
                return;
            }
            foreach (XElement element in root.Elements("Block")) {
                string name = (string)element.Attribute("Name");
                if (string.IsNullOrWhiteSpace(name)) {
                    continue;
                }
                ManaBlockDefinition definition = new() {
                    BlockName = name,
                    MaxMana = ParseFloat(element.Attribute("MaxMana"), 0f),
                    CanTransfer = ParseBool(element.Attribute("CanTransfer"), false),
                    StaffCheckable = ParseBool(element.Attribute("StaffCheckable"), false)
                };
                definition.Contents = BlocksManager.GetBlockIndex(name, false);
                m_byName[name] = definition;
                if (definition.Contents >= 0) {
                    m_byContents[definition.Contents] = definition;
                }
                else {
                    Log.Warning($"[PhytoMana]ManaBlockRegistry: unknown block name \"{name}\" in ManaBlocks.xml, skipped index binding.");
                }
            }
            IsInitialized = true;
            Log.Information($"[PhytoMana]ManaBlockRegistry: {m_byContents.Count} mana block definitions loaded.");
        }

        public static bool TryGet(int contents, out ManaBlockDefinition definition) => m_byContents.TryGetValue(contents, out definition);

        public static bool IsManaBlock(int contents) => m_byContents.ContainsKey(contents);

        /// <summary>按方块值取魔力上限；无定义返回 0。</summary>
        public static float GetMaxMana(int contents) => m_byContents.TryGetValue(contents, out ManaBlockDefinition definition) ? definition.MaxMana : 0f;

        /// <summary>按方块类名取魔力上限；无定义或值无效时返回 fallback。</summary>
        public static float GetMaxMana(string blockName, float fallback) {
            if (blockName != null
                && m_byName.TryGetValue(blockName, out ManaBlockDefinition definition)
                && definition.MaxMana > 0f) {
                return definition.MaxMana;
            }
            return fallback;
        }

        /// <summary>是否可作为魔力传递目标；无定义返回 false。</summary>
        public static bool CanTransfer(int contents) => m_byContents.TryGetValue(contents, out ManaBlockDefinition definition) && definition.CanTransfer;

        /// <summary>法杖可否检查其状态；无定义返回 false。</summary>
        public static bool IsStaffCheckable(int contents) => m_byContents.TryGetValue(contents, out ManaBlockDefinition definition) && definition.StaffCheckable;

        /// <summary>把数据文件中的魔力上限叠加进目标字典（仅覆盖有效方块值且 MaxMana>0 的条目）。</summary>
        public static void ApplyMaxManaOverrides(Dictionary<int, float> target) {
            foreach (KeyValuePair<int, ManaBlockDefinition> pair in m_byContents) {
                if (pair.Value.MaxMana > 0f) {
                    target[pair.Key] = pair.Value.MaxMana;
                }
            }
        }

        static float ParseFloat(XAttribute attribute, float fallback) {
            string text = (string)attribute;
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && value >= 0f
                ? value
                : fallback;
        }

        static bool ParseBool(XAttribute attribute, bool fallback) {
            string text = (string)attribute;
            return bool.TryParse(text, out bool value) ? value : fallback;
        }
    }
}
