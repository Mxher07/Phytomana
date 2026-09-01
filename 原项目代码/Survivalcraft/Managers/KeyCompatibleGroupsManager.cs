namespace Game {
    public static class KeyCompatibleGroupsManager {
        public class KeyCompatibleGroup(string groupId) {
            public string GroupId { get; } = groupId;
            readonly HashSet<string> keys = [];

            public void AddKey(string key) => keys.Add(key);

            public void AddKeys(params string[] keys1) {
                foreach (string key in keys1) {
                    AddKey(key);
                }
            }

            public bool ContainsKey(string key) => keys.Contains(key);
            public bool ContainsAll(IEnumerable<string> keyList) => keyList.All(k => keys.Contains(k));
            public IReadOnlyCollection<string> Keys => keys;
        }

        public static readonly Dictionary<string, KeyCompatibleGroup> m_compatibleGroups = [];

        public static void Initialize() {
            m_compatibleGroups.Clear();
            ModsManager.HookAction(
                "InitKeyCompatibleGroups",
                loader => {
                    loader.InitKeyCompatibleGroups();
                    return false;
                }
            );
        }

        /// <summary>
        ///     添加按键到兼容组
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static void AddKeyToCompatibleGroup(string groupId, params string[] keys) {
            if (!m_compatibleGroups.TryGetValue(groupId, out KeyCompatibleGroup group)) {
                group = new KeyCompatibleGroup(groupId);
            }
            group.AddKeys(keys);
            m_compatibleGroups[groupId] = group;
        }

        /// <summary>
        ///     获取兼容组信息
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public static KeyCompatibleGroup GetCompatibleGroup(string groupId) =>
            m_compatibleGroups.TryGetValue(groupId, out KeyCompatibleGroup group) ? group : null;

        /// <summary>
        ///     输入按键名称列表，检查是否存在冲突
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool HasConflict(List<string> list) {
            if (list.Count <= 1) {
                return false;
            }
            foreach (KeyCompatibleGroup group in m_compatibleGroups.Values) { // 检查是否存在包含所有键的兼容组
                if (group.ContainsAll(list)) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     输入按键名称列表，检查是否存在冲突，并输出冲突的按键列表
        /// </summary>
        /// <param name="list"></param>
        /// <param name="conflictKeys"></param>
        /// <returns></returns>
        public static bool HasConflict(List<string> list, out List<string> conflictKeys) {
            conflictKeys = [];
            if (list.Count <= 1) {
                return false;
            }
            foreach (string key in list) {
                bool hasCompatibleGroup = false;
                foreach (KeyCompatibleGroup group in m_compatibleGroups.Values) {
                    if (group.ContainsAll(list)) {
                        hasCompatibleGroup = true;
                        break;
                    }
                }
                if (!hasCompatibleGroup
                    && !conflictKeys.Contains(key)) {
                    conflictKeys.Add(key);
                }
            }
            return conflictKeys.Count > 0;
        }
    }
}