using System.Text.Json;
using Engine;
using Engine.Serialization;

namespace Game {
    /// <summary>
    /// JSON → 设置描述符。容错：缺字段/解析失败降级 + Log，不禁用模组。
    /// 类型/值转换薄封装，复用 HumanReadableConverter，不重写转换逻辑。
    /// </summary>
    public static class ModSettingsParser {
        /// <summary>解析顶层 Settings 数组 → ModSettingPage 列表。失败返回空列表。</summary>
        public static List<ModSettingPage> ParseSettings(JsonElement settingsArray, string packageName) {
            List<ModSettingPage> result = new();
            if (settingsArray.ValueKind != JsonValueKind.Array) return result;
            foreach (JsonElement element in settingsArray.EnumerateArray()) {
                if (ParseElement(element, new List<string>(), packageName) is ModSettingPage page)
                    result.Add(page);
            }
            return result;
        }

        /// <summary>按值类型选默认 Widget。返回 null 表示无默认（需模组自定义）。</summary>
        public static Type GetDefaultWidgetType(Type valueType) {
            if (valueType == typeof(bool)) return typeof(BoolButtonSettingWidget);
            if (typeof(Enum).IsAssignableFrom(valueType)) return typeof(EnumSelectionDialogSettingWidget);
            if (IsNumeric(valueType)) return typeof(NumberSliderSettingWidget);
            if (valueType == typeof(string)) return typeof(TextBoxSettingWidget);
            return null;
        }

        static ModSettingElement ParseElement(JsonElement obj, List<string> idChain, string packageName) {
            if (obj.ValueKind != JsonValueKind.Object) return null;
            bool hasItems = obj.TryGetProperty("Items", out JsonElement itemsEl) && itemsEl.ValueKind == JsonValueKind.Array;
            bool hasType = obj.TryGetProperty("Type", out JsonElement typeEl) && typeEl.ValueKind == JsonValueKind.String;
            bool isSeparator = obj.TryGetProperty("Separator", out JsonElement sepEl) && sepEl.ValueKind == JsonValueKind.True;
            bool hasText = obj.TryGetProperty("Text", out JsonElement textEl) && textEl.ValueKind == JsonValueKind.String;
            bool hasId = obj.TryGetProperty("Id", out JsonElement idEl) && idEl.ValueKind == JsonValueKind.String;

            if (isSeparator) return new ModSettingSeparator();
            if (hasItems) return ParsePage(obj, itemsEl, packageName);
            if (hasType) return ParseItem(obj, typeEl);
            // Label：Text 在则取字面量/token（向后兼容）；Id-only（无 Type/Items/Text）→ 文案走 id链.Id.Name 自动键（ResolveText 第2档）
            if (hasText) {
                string textId = GetString(obj, "Id");
                return new ModSettingLabel { Id = IsValidId(textId) ? textId : null, Text = textEl.GetString() };
            }
            if (hasId) {
                string id = idEl.GetString();
                if (!IsValidId(id)) { Log.Error("[ModSettings] " + L("LabelInvalidId", "Mod setting label missing valid Id or contains '/', skipped")); return null; }
                return new ModSettingLabel { Id = id, Text = null };
            }
            return null;
        }

        static ModSettingPage ParsePage(JsonElement obj, JsonElement itemsEl, string packageName) {
            string id = GetString(obj, "Id");
            if (!IsValidId(id)) { Log.Error("[ModSettings] " + L("PageInvalidId", "Mod '{0}' page missing valid Id or contains '/', skipped", packageName)); return null; }
            ModSettingPage page = new() {
                Id = id,
                Name = GetString(obj, "Name"),
                Title = GetString(obj, "Title"),
                Description = GetString(obj, "Description")
            };
            foreach (JsonElement child in itemsEl.EnumerateArray()) {
                if (ParseElement(child, new List<string>(), packageName) is ModSettingElement el)
                    page.Items.Add(el);
            }
            return page;
        }

        static ModSettingItem ParseItem(JsonElement obj, JsonElement typeEl) {
            string id = GetString(obj, "Id");
            if (!IsValidId(id)) { Log.Error("[ModSettings] " + L("ItemInvalidId", "Setting item missing valid Id or contains '/', skipped")); return null; }
            string typeStr = typeEl.GetString();
            Type type = ResolveType(typeStr);
            if (type == null) { Log.Error("[ModSettings] " + L("TypeResolveFailed", "Type '{0}' of setting '{1}' could not be resolved, skipped", typeStr, id)); return null; }

            ModSettingItem item = new() {
                Id = id,
                Name = GetString(obj, "Name"),
                Description = GetString(obj, "Description"),
                Type = type,
                Default = ResolveDefault(obj, type),
                WidgetType = ResolveWidget(GetString(obj, "Widget"), type)
            };
            if (obj.TryGetProperty("WidgetProperties", out JsonElement propsEl) && propsEl.ValueKind == JsonValueKind.Object)
                item.WidgetProperties = propsEl.Clone(); // Clone 脱离 JsonDocument 生命周期
            return item;
        }

        static bool IsValidId(string id) => !string.IsNullOrEmpty(id) && !id.Contains('/');

        static Type ResolveType(string typeStr) {
            if (string.IsNullOrEmpty(typeStr)) return null;
            // FindType 依赖全限定名或已注册短名表；简单名未注册时 GetType 查不到（只查全局命名空间）。
            // 内置类型多在 Game 命名空间，简单名补该前缀；模组自定义类型应以全限定名声明。
            Type t = TypeCache.FindType(typeStr, true, false);
            if (t == null && !typeStr.Contains('.')) {
                t = TypeCache.FindType("Game." + typeStr, true, false);
            }
            return t;
        }

        static object ResolveDefault(JsonElement obj, Type type) {
            if (!obj.TryGetProperty("Default", out JsonElement defEl)) return GetDefaultOf(type);
            try { return ConvertValue(type, defEl); }
            catch (Exception e) { Log.Error("[ModSettings] " + L("DefaultParseFailed", "Default parse failed, using default({0}): {1}", type.Name, e.Message)); return GetDefaultOf(type); }
        }

        static object GetDefaultOf(Type type) {
            if (type == typeof(bool)) return false;
            if (type.IsValueType) return Activator.CreateInstance(type); // 数值=0，enum=首项
            return null;
        }

        /// <summary>JSON 值 → 字符串表示 → HumanReadableConverter 转值。</summary>
        internal static object ConvertValue(Type type, JsonElement valueEl) {
            string s = ValueElementToString(valueEl);
            return HumanReadableConverter.ConvertFromString(type, s);
        }

        static string ValueElementToString(JsonElement el) {
            return el.ValueKind switch {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.True => "True",
                JsonValueKind.False => "False",
                JsonValueKind.Number => el.GetRawText(),
                _ => el.GetRawText()
            };
        }

        /// <summary>内置别名/类名直接映射 typeof；模组自定义全限定名走 FindType。</summary>
        static Type ResolveWidget(string widgetStr, Type valueType) {
            if (!string.IsNullOrEmpty(widgetStr)) {
                switch (widgetStr) {
                    case "BoolButtonSettingWidget": return typeof(BoolButtonSettingWidget);
                    case "EnumSelectionDialogSettingWidget": return typeof(EnumSelectionDialogSettingWidget);
                    case "EnumSliderSettingWidget": return typeof(EnumSliderSettingWidget);
                    case "NumberSliderSettingWidget": return typeof(NumberSliderSettingWidget);
                    case "TextBoxSettingWidget": return typeof(TextBoxSettingWidget);
                }
                // 模组自定义 Widget：按全限定名查
                Type t = TypeCache.FindType(widgetStr, true, false);
                if (t != null && typeof(IModSettingItemWidget).IsAssignableFrom(t)) return t;
                Log.Error("[ModSettings] " + L("WidgetResolveFailed", "Widget '{0}' not found or not an IModSettingItemWidget, falling back to default", widgetStr));
            }
            return GetDefaultWidgetType(valueType); // 缺省/不合法 → 类型默认
        }

        static bool IsNumeric(Type t) {
            return t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)
                || t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort) || t == typeof(sbyte)
                || t == typeof(float) || t == typeof(double) || t == typeof(decimal);
        }

        static string GetString(JsonElement obj, string name) =>
            obj.TryGetProperty(name, out JsonElement el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

        /// <summary>取本类命名空间下本地化日志串，未命中回退 engDefault，string.Format 填参数。</summary>
        static string L(string key, string engDefault, params object[] args) {
            if (!LanguageControl.TryGet(out string s, nameof(ModSettingsParser), key)) s = engDefault;
            return string.Format(s, args);
        }
    }
}
