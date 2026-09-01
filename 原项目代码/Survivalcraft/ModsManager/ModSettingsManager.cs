using System.Text;
using System.Xml.Linq;
using Engine;
using TemplatesDatabase;
using XmlUtilities;
using Engine.Serialization;

namespace Game {
    public static class ModSettingsManager {
        /// <summary>
        ///     储存每一个没有使用到的Mod设置的键值对，键：Mod的包名，值：Mod的设置信息XElement
        /// </summary>
        public static Dictionary<string, XElement> ModSettingsCache { get; private set; } = new();

        /// <summary>
        ///     储存每个模组的键盘鼠标键位映射设置，键：模组包名，值：模组的键位映射设置
        /// </summary>
        public static Dictionary<string, ValuesDictionary> ModKeyboardMapSettings { get; private set; } = new();

        /// <summary>
        ///     储存每个模组的手柄键位映射设置，键：模组包名，值：模组的键位映射设置
        /// </summary>
        public static Dictionary<string, ValuesDictionary> ModGamepadMapSettings { get; private set; } = new();

        /// <summary>
        ///     储存每个模组的相机设置，键：模组包名，值：模组的相机设置
        /// </summary>
        public static Dictionary<string, ValuesDictionary> ModCameraManageSettings { get; private set; } = new();

        public static Dictionary<string, List<ModSettingPage>> ModSettingPages => m_dataDrivenPages;

        // ===== 数据驱动设置层 =====
        static Dictionary<string, List<ModSettingPage>> m_dataDrivenPages = new();
        static Dictionary<string, ModSettingItem> m_dataDrivenItems = new();   // CachedPath -> item
        static Dictionary<string, object> m_dataDrivenValues = new();           // CachedPath -> 强类型运行时值

        public const string fName = "ModSettingsManager";

        /// <summary>LoadModSettings 是否已在本会话调用过。SaveModSettings 据此跳过启动竞态期（LoadModSettings 之前的 Window.Deactivated 等早触发）的空缓存覆写。</summary>
        static bool s_loaded;

        public static Dictionary<string, object> CombinedKeyboardMappingSettings {
            get { //合并模组设置和原版设置
                Dictionary<string, object> dictionary = new();
                foreach (KeyValuePair<string, object> item in SettingsManager.KeyboardMappingSettings) {
                    dictionary.TryAdd(item.Key, item.Value);
                }
                foreach (ValuesDictionary item in ModKeyboardMapSettings.Values) {
                    foreach (KeyValuePair<string, object> item2 in item) {
                        dictionary.TryAdd(item2.Key, item2.Value);
                    }
                }
                return dictionary;
            }
        }
        public static Dictionary<string, object> CombinedGamepadMappingSettings {
            get { //合并模组设置和原版设置
                Dictionary<string, object> dictionary = new();
                foreach (KeyValuePair<string, object> item in SettingsManager.GamepadMappingSettings) {
                    dictionary.TryAdd(item.Key, item.Value);
                }
                foreach (ValuesDictionary item in ModGamepadMapSettings.Values) {
                    foreach (KeyValuePair<string, object> item2 in item) {
                        dictionary.TryAdd(item2.Key, item2.Value);
                    }
                }
                return dictionary;
            }
        }

        public static Dictionary<string, int> CombinedCameraManageSettings {
            get {
                Dictionary<string, int> dictionary = new();
                foreach (KeyValuePair<string, object> item in SettingsManager.CameraManageSettings) {
                    dictionary.TryAdd(item.Key, Convert.ToInt32(item.Value));
                }
                foreach (ValuesDictionary item in ModCameraManageSettings.Values) {
                    foreach (KeyValuePair<string, object> item2 in item) {
                        dictionary.TryAdd(item2.Key, Convert.ToInt32(item2.Value));
                    }
                }
                return dictionary;
            }
        }

        public static void LoadModSettings() {
            s_loaded = true;
            if (!Storage.FileExists(ModsManager.ModsSettingsPath)) {
                RegisterAllDataDriven(); // 文件不存在：仍注册描述符 + Default
                return;
            }

            //读取设置并且加入到ModSettings表内
            try {
                using (Stream stream = Storage.OpenFile(ModsManager.ModsSettingsPath, OpenFileMode.Read)) {
                    XElement element = XElement.Load(stream);
                    foreach (XElement modXElement in element.Elements("Mod")) {
                        string packageName = XmlUtils.GetAttributeValue<string>(modXElement, "PackageName");
                        ModSettingsCache[packageName] = modXElement;
                    }
                }
            }
            catch (Exception e) {
                if (!LanguageControl.TryGet(out string str, fName, "1")) {
                    str = "Error serializing mod settings file:";
                }
                Log.Warning($"{str} {e.Message}");
                // 不 return：残缺/空文件时仍落入下方 RegisterAllDataDriven，按 ModList 注册描述符 + Default。
                // 否则 m_dataDrivenPages 为空，下次 SaveModSettings 会把所有 <DataDrivenSettings> 剥掉写盘 → 设置永久丢失。
            }

            //遍历每个模组，加载设置项，如果设置项已加载，就从ModSettingsCache中删除
            try {
                foreach (ModEntity modEntity in ModsManager.ModList) {
                    string packageName = modEntity.modInfo.PackageName;
                    ValuesDictionary modKeyboardSettings = [];
                    ValuesDictionary modGamepadSettings = [];
                    ValuesDictionary modCameraSettings = [];
                    IEnumerable<KeyValuePair<string, object>> keysToAdd = modEntity.Loaders.SelectMany(item => item.GetKeyboardMappings()); //初始化模组默认键位设置
                    IEnumerable<KeyValuePair<string, object>> gamepadKeysToAdd = modEntity.Loaders.SelectMany(item => item.GetGamepadMappings()); //初始化模组默认键位设置
                    IEnumerable<KeyValuePair<string, int>> camerasToAdd = modEntity.Loaders.SelectMany(item => item.GetCameraList()); //初始化模组默认相机设置
                    foreach (KeyValuePair<string, object> item1 in keysToAdd) {
                        modKeyboardSettings.Add(item1.Key, item1.Value);
                    }
                    foreach (KeyValuePair<string, object> item1 in gamepadKeysToAdd) {
                        modGamepadSettings.Add(item1.Key, item1.Value);
                    }
                    foreach (KeyValuePair<string, int> item2 in camerasToAdd) {
                        modCameraSettings.Add(item2.Key, item2.Value);
                    }
                    if (ModSettingsCache.TryGetValue(packageName, out XElement setting)) {
                        modEntity.LoadSettings(setting);
                        if (setting != null) { //加载模组保存的键位映射、相机设置
                            XElement keyboardMapping = setting.Element("KeyboardMapping");
                            if (keyboardMapping != null) {
                                modKeyboardSettings.ApplyOverrides(keyboardMapping, true);
                            }
                            XElement gamepadMapping = setting.Element("GamepadMapping");
                            if (gamepadMapping != null) {
                                modGamepadSettings.ApplyOverrides(gamepadMapping);
                            }
                            XElement cameraList = setting.Element("CameraList");
                            if (cameraList != null) {
                                modCameraSettings.ApplyOverrides(cameraList, true);
                            }
                        }
                    }
                    ModKeyboardMapSettings[packageName] = modKeyboardSettings;
                    ModGamepadMapSettings[packageName] = modGamepadSettings;
                    ModCameraManageSettings[packageName] = modCameraSettings;
                }
                if (!LanguageControl.TryGet(out string info, fName, "2")) {
                    info = "Loaded mod settings";
                }
                Log.Information(info);
            }
            catch (Exception e) {
                if (!LanguageControl.TryGet(out string str, fName, "3")) {
                    str = "Error loading mod settings:";
                }
                Log.Warning($"{str} {e}");
            }
            RegisterAllDataDriven(); // 文件已读入 ModSettingsCache，按 ModList 注册（命中用持久化值，否则 Default）
        }

        public static void SaveModSettings() {
            // 启动竞态保护：LoadModSettings 尚未首次运行时，ModSettingsCache/m_dataDrivenPages 为空，
            // 此时写盘会用空内容覆写文件，永久丢失所有模组设置（Window.Deactivated 等可能在加载早期触发 SaveSettings）。
            if (!s_loaded) return;
            foreach (ModEntity modEntity in ModsManager.ModList) {
                string packageName = modEntity.modInfo.PackageName;
                XElement settingsElement = new("Mod");
                XmlUtils.SetAttributeValue(settingsElement, "PackageName", packageName);
                try {
                    modEntity.SaveSettings(settingsElement);
                }
                catch (Exception e) {
                    if (!LanguageControl.TryGet(out string str, fName, "4")) {
                        str = "Error saving the mod settings of [{0}]:";
                    }
                    Log.Warning($"{string.Format(str, packageName)} {e}");
                }
                //保存模组的键盘鼠标键位映射设置
                XElement keyboardMapping = new("KeyboardMapping");
                if (ModKeyboardMapSettings.TryGetValue(packageName, out ValuesDictionary modKeyboardSettings)
                    && modKeyboardSettings.Count > 0) {
                    modKeyboardSettings.Save(keyboardMapping);
                    settingsElement.Add(keyboardMapping);
                }
                //保存模组的手柄键位映射设置
                XElement gamepadMapping = new("GamepadMapping");
                if (ModGamepadMapSettings.TryGetValue(packageName, out ValuesDictionary modGamepadSettings)
                    && modGamepadSettings.Count > 0) {
                    modGamepadSettings.Save(gamepadMapping);
                    settingsElement.Add(gamepadMapping);
                }
                //保存模组的相机设置
                XElement cameraList = new("CameraList");
                if (ModCameraManageSettings.TryGetValue(packageName, out ValuesDictionary modCameraSettings)
                    && modCameraSettings.Count > 0) {
                    modCameraSettings.Save(cameraList);
                    settingsElement.Add(cameraList);
                }
                SaveDataDriven(packageName, settingsElement);
                //模组保存了设置
                if (settingsElement.Elements().Any()
                    || settingsElement.Attributes().Count() > 1) {
                    ModSettingsCache[packageName] = settingsElement;
                }
            }
            XElement xElement = new("ModSettings");
            foreach (KeyValuePair<string, XElement> settingElement in ModSettingsCache) {
                xElement.Add(settingElement.Value);
            }
            try {
                using (Stream stream = Storage.OpenFile(ModsManager.ModsSettingsPath, OpenFileMode.Create)) {
                    XmlUtils.SaveXmlToStream(xElement, stream, Encoding.UTF8, true);
                }
            }
            catch (Exception e) {
                if (!LanguageControl.TryGet(out string str, fName, "5")) {
                    str = "Error saving mod settings file:";
                }
                Log.Warning($"{str} {e.Message}");
            }
            if (!LanguageControl.TryGet(out string info, fName, "6")) {
                info = "Saved mod settings";
            }
            Log.Information(info);
        }

        // ===== 数据驱动设置：值存取 =====

        /// <summary>模组读取设置值。path = packageName + 逐级页面 Id + itemId（完整 Id 链，不省略）。</summary>
        public static T Get<T>(params string[] path) {
            string key = string.Join("/", path);
            if (!m_dataDrivenValues.TryGetValue(key, out object v))
                throw new KeyNotFoundException($"ModSettingsManager.Get: 设置项不存在 path=[{key}]");
            if (v is not T t)
                throw new InvalidCastException($"ModSettingsManager.Get: 类型不匹配 path=[{key}] 值类型={v?.GetType()} 请求={typeof(T)}");
            return t;
        }

        public static bool TryGet<T>(out T value, params string[] path) {
            string key = string.Join("/", path);
            if (m_dataDrivenValues.TryGetValue(key, out object v)) {
                if (v is T t) {
                    value = t;
                    return true;
                }
            }
            value = default;
            return false;
        }

        /// <summary>热路径：按描述符高速取值（零字符串拼接）。</summary>
        public static T Get<T>(ModSettingItem item) {
            if (!m_dataDrivenValues.TryGetValue(item.CachedPath, out object v))
                throw new KeyNotFoundException($"ModSettingsManager.Get: 设置项不存在 path=[{item.CachedPath}]");
            if (v is not T t)
                throw new InvalidCastException($"ModSettingsManager.Get: 类型不匹配 path=[{item.CachedPath}] 值类型={v?.GetType()} 请求={typeof(T)}");
            return t;
        }

        public static bool TryGet<T>(out T value, ModSettingItem item) {
            if (m_dataDrivenValues.TryGetValue(item.CachedPath, out object v)) {
                if (v is T t) {
                    value = t;
                    return true;
                }
            }
            value = default;
            return false;
        }

        /// <summary>UI 取当前值（raw object，无类型校验，含 null）。模组读取请用 Get&lt;T&gt;。</summary>
        internal static object GetValue(string[] path) {
            string key = string.Join("/", path);
            m_dataDrivenValues.TryGetValue(key, out object v);
            return v;
        }

        /// <summary>热路径：按 packageName + subPath（不含 packageName）查描述符。</summary>
        public static ModSettingItem FindItem(string packageName, params string[] subPath) {
            string key = packageName + "/" + string.Join("/", subPath);
            m_dataDrivenItems.TryGetValue(key, out ModSettingItem item);
            return item;
        }

        /// <summary>值变更事件：Set 写完字典后触发，参数 (key, value)，key = string.Join("/", path) 与 ModSettingItem.CachedPath 同形。供 UI 层订阅以刷新显示。订阅者抛异常不影响其它订阅者（逐委托 try/catch）。</summary>
        public static event Action<string, object> SettingChanged;

        /// <summary>逐订阅者触发 SettingChanged，单个订阅者抛异常不影响其它（与下方 loader 分发同风格）。订阅者约定不抛异常，但此处仍兜底以防第三方 mod 订阅者故障阻断 UI 刷新。</summary>
        static void RaiseSettingChanged(string key, object value) {
            if (SettingChanged == null) return;
            foreach (Delegate d in SettingChanged.GetInvocationList()) {
                try { ((Action<string, object>)d)(key, value); }
                catch (Exception e) {
                    if (!LanguageControl.TryGet(out string msg, fName, "12")) msg = "SettingChanged subscriber error: {0}";
                    Log.Error("[ModSettings] " + string.Format(msg, e.Message));
                }
            }
        }

        /// <summary>Widget 写回值。更新字典 + 精准分发 OnSettingChanged。</summary>
        public static void Set(string[] path, object value) {
            if (path == null || path.Length == 0) return;
            string key = string.Join("/", path);
            m_dataDrivenValues[key] = value;
            RaiseSettingChanged(key, value);
            string packageName = path[0];
            string[] subPath = path[1..];
            // 精准分发到目标模组 loaders：复用 HookAction 会广播所有注册 loader 且 subPath 不含 packageName，
            // loader 无法自判归属，不符"按 packageName 分发"，故改精准定位 PackageNameToModEntity + 手写 try/catch。
            if (ModsManager.PackageNameToModEntity.TryGetValue(packageName, out ModEntity entity)) {
                foreach (ModLoader loader in entity.Loaders) {
                    try { loader.OnModSettingChanged(subPath, value); }
                    catch (Exception e) {
                        if (!LanguageControl.TryGet(out string msg, fName, "10")) msg = "OnSettingChanged error, loader={0}: {1}";
                        Log.Error("[ModSettings] " + string.Format(msg, loader.GetType().Name, e.Message));
                    }
                }
            }
        }

        // ===== 数据驱动设置：注册 / 持久化 / 生命周期 =====

        public static List<ModSettingPage> GetPages(string packageName) =>
            m_dataDrivenPages.TryGetValue(packageName, out List<ModSettingPage> pages) ? pages : null;

        // LoadModSettings 调：清空 + 按 ModList 注册（cache 已填或空）
        static void RegisterAllDataDriven() {
            m_dataDrivenPages.Clear();
            m_dataDrivenItems.Clear();
            m_dataDrivenValues.Clear();
            foreach (ModEntity modEntity in ModsManager.ModList) {
                string packageName = modEntity.modInfo.PackageName;
                ModSettingsCache.TryGetValue(packageName, out XElement cacheEl);
                RegisterDataDriven(packageName, modEntity.modInfo.Settings, cacheEl);
            }
        }

        // 仅 ModList 内模组注册：禁用模组不注册，XML 节点靠 ModSettingsCache 保留
        internal static void RegisterDataDriven(string packageName, List<ModSettingPage> pages, XElement persistedMod) {
            if (pages == null || pages.Count == 0) return;
            m_dataDrivenPages[packageName] = pages;
            Dictionary<string, string> persisted = ReadPersistedValues(persistedMod);
            foreach (ModSettingPage page in pages)
                RegisterElement(packageName, new List<string>(), page, persisted);
        }

        static void RegisterElement(string packageName, List<string> idChain, ModSettingElement el, Dictionary<string, string> persisted) {
            if (el is ModSettingPage page) {
                idChain.Add(page.Id);
                foreach (ModSettingElement child in page.Items)
                    RegisterElement(packageName, idChain, child, persisted);
                idChain.RemoveAt(idChain.Count - 1);
            }
            else if (el is ModSettingItem item) {
                idChain.Add(item.Id);
                string cachedPath = packageName + "/" + string.Join("/", idChain);
                item.CachedPath = cachedPath;
                m_dataDrivenItems[cachedPath] = item;
                m_dataDrivenValues[cachedPath] = LoadItemValue(item, persisted);
                idChain.RemoveAt(idChain.Count - 1);
            }
            // separator/label 无值，跳过
        }

        static object LoadItemValue(ModSettingItem item, Dictionary<string, string> persisted) {
            string subPath = item.CachedPath.Substring(item.CachedPath.IndexOf('/') + 1); // 去 packageName/
            if (persisted != null && persisted.TryGetValue(subPath, out string valueStr)) {
                try { return HumanReadableConverter.ConvertFromString(item.Type, valueStr); }
                catch (Exception e) {
                    if (!LanguageControl.TryGet(out string msg, fName, "11")) msg = "Persisted value parse failed, path={0}, using default: {1}";
                    Log.Error("[ModSettings] " + string.Format(msg, item.CachedPath, e.Message));
                }
            }
            return item.Default; // 缺失/失败 → Default
        }

        // SaveModSettings 调：写 <DataDrivenSettings>
        internal static void SaveDataDriven(string packageName, XElement modElement) {
            if (!m_dataDrivenPages.TryGetValue(packageName, out List<ModSettingPage> pages) || pages.Count == 0) return;
            XElement dd = new("DataDrivenSettings");
            string prefix = packageName + "/";
            foreach (KeyValuePair<string, ModSettingItem> kv in m_dataDrivenItems) {
                if (!kv.Key.StartsWith(prefix)) continue;
                ModSettingItem item = kv.Value;
                object val = m_dataDrivenValues.TryGetValue(kv.Key, out object v) ? v : item.Default;
                XElement itemEl = new("Item");
                XmlUtils.SetAttributeValue(itemEl, "Path", kv.Key.Substring(prefix.Length));
                XmlUtils.SetAttributeValue(itemEl, "Type", item.Type.FullName);
                XmlUtils.SetAttributeValue(itemEl, "Value", HumanReadableConverter.ConvertToString(val));
                dd.Add(itemEl);
            }
            if (dd.HasElements) modElement.Add(dd);
        }

        static Dictionary<string, string> ReadPersistedValues(XElement modElement) {
            Dictionary<string, string> result = new();
            if (modElement == null) return result;
            XElement dd = modElement.Element("DataDrivenSettings");
            if (dd == null) return result;
            foreach (XElement itemEl in dd.Elements("Item")) {
                string path = XmlUtils.GetAttributeValue<string>(itemEl, "Path", null);
                string value = XmlUtils.GetAttributeValue<string>(itemEl, "Value", null);
                if (path != null) result[path] = value;
            }
            return result;
        }

        public static void ResetModsKeyboardMappingSettings() {
            foreach (ModEntity modEntity in ModsManager.ModList) {
                string packageName = modEntity.modInfo.PackageName;
                if (ModKeyboardMapSettings.TryGetValue(packageName, out ValuesDictionary keyboardSettings)) {
                    keyboardSettings.Clear();
                    IEnumerable<KeyValuePair<string, object>> keysToAdd = modEntity.Loaders.SelectMany(item => item.GetKeyboardMappings());
                    foreach (KeyValuePair<string, object> item1 in keysToAdd) {
                        keyboardSettings.Add(item1.Key, item1.Value);
                    }
                }
            }
            Log.Information(LanguageControl.Get(fName, "7"));
        }

        public static void ResetModsGamepadMappingSettings() {
            foreach (ModEntity modEntity in ModsManager.ModList) {
                string packageName = modEntity.modInfo.PackageName;
                if (ModGamepadMapSettings.TryGetValue(packageName, out ValuesDictionary gamepadSettings)) {
                    gamepadSettings.Clear();
                    IEnumerable<KeyValuePair<string, object>> gamepadKeysToAdd = modEntity.Loaders.SelectMany(item => item.GetGamepadMappings());
                    foreach (KeyValuePair<string, object> item1 in gamepadKeysToAdd) {
                        gamepadSettings.Add(item1.Key, item1.Value);
                    }
                }
            }
            Log.Information(LanguageControl.Get(fName, "9"));
        }

        public static void ResetModsCameraManageSettings() {
            foreach (ModEntity modEntity in ModsManager.ModList) {
                string packageName = modEntity.modInfo.PackageName;
                if (ModCameraManageSettings.TryGetValue(packageName, out ValuesDictionary cameraSettings)) {
                    cameraSettings.Clear();
                    IEnumerable<KeyValuePair<string, int>> camerasToAdd = modEntity.Loaders.SelectMany(item => item.GetCameraList());
                    foreach (KeyValuePair<string, int> item1 in camerasToAdd) {
                        cameraSettings.Add(item1.Key, item1.Value);
                    }
                }
            }
            Log.Information(LanguageControl.Get(fName, "8"));
        }
    }
}