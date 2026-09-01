namespace Game {
    /// <summary>
    /// 设置文案三档解析 + enum 成员名解析。
    /// 复用 LanguageControl.Get 的命中标志 r，不靠返回值非空判定。
    /// </summary>
    public static class ModSettingLocalizer {
        /// <summary>
        /// 解析 Name/Title/Description/Text 文案。
        /// idChain = [顶层pageId, ..., itemId]（不含 packageName）。
        /// useIdFallback：Name/Title/Text 兜底用 idChain 末段（Id），Description 兜底用空串。
        /// </summary>
        public static string ResolveText(string packageName, string[] idChain, string fieldName, string rawValue, bool useIdFallback) {
            // 第 1 档：整体 [ ] 包裹才算 token
            if (rawValue != null && rawValue.Length >= 2 && rawValue[0] == '[' && rawValue[^1] == ']') {
                string inner = rawValue.Substring(1, rawValue.Length - 2);
                string[] tokenKeys = inner.Split('/', ':');
                string tokenResult = LanguageControl.Get(out bool tokenHit, tokenKeys);
                if (tokenHit) return tokenResult;
                // token 未命中 → 继续走默认回退
            }
            else if (rawValue != null) {
                // 第 4 档：字面量（非整体 [ ] 包裹，直接显示原文）
                return rawValue;
            }
            // rawValue == null（字段缺失）或 token 未命中 → 第 2 档默认回退
            // keys = ["ModSettings", packageName, ...idChain, fieldName]
            string[] keys = new string[idChain.Length + 3];
            keys[0] = "ModSettings";
            keys[1] = packageName;
            idChain.CopyTo(keys, 2);
            keys[keys.Length - 1] = fieldName;
            string fallbackResult = LanguageControl.Get(out bool fallbackHit, keys);
            if (fallbackHit) return fallbackResult;
            // 第 3 档：兜底
            return useIdFallback && idChain.Length > 0 ? idChain[^1] : string.Empty;
        }

        /// <summary>
        /// enum 成员本地化文本。未命中时 LanguageControl.Get 返回 keys.Last()=memberName，天然回退英文成员名。
        /// </summary>
        public static string GetEnumMemberText(string packageName, Type enumType, object value) {
            string memberName = Enum.GetName(enumType, value) ?? value?.ToString() ?? string.Empty;
            if (LanguageControl.TryGet(out string result, "ModSettings", packageName, enumType.Name, memberName)) {
                return result;
            }
            return memberName;
        }

        /// <summary>从描述符 CachedPath 取 packageName（path 首段），用于 enum 成员本地化。</summary>
        public static string ExtractPackageName(ModSettingItem d) => d.CachedPath != null && d.CachedPath.Contains('/')
            ? d.CachedPath.Substring(0, d.CachedPath.IndexOf('/'))
            : null;

        /// <summary>从描述符 CachedPath 取 idChain（去 packageName 前缀，按 '/' 拆分），用于 ResolveText 的 idChain 参数。</summary>
        public static string[] ExtractIdChain(ModSettingItem d) {
            if (d.CachedPath == null) return Array.Empty<string>();
            int slash = d.CachedPath.IndexOf('/');
            string rest = slash >= 0 ? d.CachedPath.Substring(slash + 1) : d.CachedPath;
            return string.IsNullOrEmpty(rest) ? Array.Empty<string>() : rest.Split('/');
        }
    }
}
