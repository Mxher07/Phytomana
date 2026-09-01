using Engine;

namespace Game {
    /// <summary>
    /// 按 descriptor.WidgetType 实例化 IModSettingItemWidget。
    /// 实例化后查 Supports(Type)，不满足回退默认 Widget；默认也不满足返回 null（Screen 跳过该项 + Log）。
    /// 回退时被丢弃的首选 Widget 已在 Initialize 建好控件树，但未挂入 Screen，由 GC 回收，不持有需显式释放的资源。
    /// </summary>
    public static class ModSettingItemWidgetFactory {
        public static IModSettingItemWidget Create(ModSettingItem descriptor, object currentValue, string nameText, string descriptionText) {
            IModSettingItemWidget widget = TryCreate(descriptor.WidgetType, descriptor, currentValue, nameText, descriptionText);
            if (widget != null && widget.Supports(descriptor.Type)) return widget;
            Type defaultType = ModSettingsParser.GetDefaultWidgetType(descriptor.Type);
            if (defaultType != null) {
                IModSettingItemWidget fallback = TryCreate(defaultType, descriptor, currentValue, nameText, descriptionText);
                if (fallback != null && fallback.Supports(descriptor.Type)) {
                    Log.Error("[ModSettings] " + L("WidgetFallback", "Setting '{0}' widget unsupported, fell back to default '{1}'.", descriptor.Id, defaultType.Name));
                    return fallback;
                }
            }
            Log.Error("[ModSettings] " + L("WidgetNone", "Setting '{0}' has no usable widget (type {1}), skipped.", descriptor.Id, descriptor.Type.Name));
            return null;
        }

        static IModSettingItemWidget TryCreate(Type type, ModSettingItem descriptor, object currentValue, string nameText, string descriptionText) {
            if (type == null) return null;
            try {
                IModSettingItemWidget widget = Activator.CreateInstance(type) as IModSettingItemWidget;
                widget?.Initialize(descriptor, currentValue, nameText, descriptionText);
                return widget;
            }
            catch (Exception e) { Log.Error("[ModSettings] " + L("WidgetCreateFailed", "Widget '{0}' instantiation failed: {1}", type.Name, e.Message)); return null; }
        }

        /// <summary>取本类命名空间下本地化日志串，未命中回退 engDefault，string.Format 填参数。</summary>
        static string L(string key, string engDefault, params object[] args) {
            if (!LanguageControl.TryGet(out string s, nameof(ModSettingItemWidgetFactory), key)) s = engDefault;
            return string.Format(s, args);
        }
    }
}
