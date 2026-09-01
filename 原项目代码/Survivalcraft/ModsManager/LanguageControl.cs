using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Engine;
using Game.IContentReader;

namespace Game {
    public static class LanguageControl {
        public static JsonNode jsonNode;
        public static JsonNode englishJsonNode;
        public static string Ok;
        public static string Cancel;
        public static string None;
        public static string Nothing;
        public static string Error;
        public static string On;
        public static string Off;
        public static string Disable;
        public static string Enable;
        public static string Warning;
        public static string Back;
        public static string Allowed;
        public static string NAllowed;
        public static string Unknown;
        public static string Yes;
        public static string No;
        public static string Unavailable;
        public static string Exists;
        public static string Success;
        public static string Delete;
        /// <summary>
        ///     语言标识符、与相应的CultureInfo
        /// </summary>
#if BROWSER
        public static HashSet<string> LanguageTypes = [];
#else
        public static Dictionary<string, CultureInfo> LanguageTypes = [];

        public static CultureInfo CurrentLanguageCultureInfo { get; set; } = new("en-US", false);
#endif

        public static string CurrentLanguageName { get; set; } = "en-US";

        public static void Initialize(string languageType) {
#if BROWSER
            if (!LanguageTypes.Contains(languageType)) {
#else
            if (!LanguageTypes.TryGetValue(languageType, out CultureInfo cultureInfo)) {
#endif
                throw new Exception($"Language {languageType} not supported.");
            }
            Ok = null;
            Cancel = null;
            None = null;
            Nothing = null;
            Error = null;
            On = null;
            Off = null;
            Disable = null;
            Enable = null;
            Warning = null;
            Back = null;
            Allowed = null;
            NAllowed = null;
            Unknown = null;
            Yes = null;
            No = null;
            Unavailable = null;
            Exists = null;
            Success = null;
            Delete = null;
            jsonNode = null;
            ModsManager.SetConfig("Language", languageType);
            CurrentLanguageName = languageType;
#if BROWSER
            Engine.Browser.BrowserInterop.SetDocumentLang(languageType);
#else
            CurrentLanguageCultureInfo = cultureInfo;
#endif
        }

        public static void loadJson(Stream stream) {
            string txt = new StreamReader(stream).ReadToEnd();
            if (txt.Length > 0) { //加载原版语言包
                JsonNode newJsonNode;
                try {
                    newJsonNode = JsonNode.Parse(txt, null, JsonDocumentReader.DefaultJsonOptions);
                }
                catch (Exception e) {
                    Log.Error($"Invalid json file, reason: {e}");
                    return;
                }
                if (jsonNode == null) {
                    jsonNode = newJsonNode;
                }
                else {
                    MergeJsonNode(jsonNode, newJsonNode);
                }
            }
        }

        public static void LoadEnglishJson(Stream stream) {
            string txt = new StreamReader(stream).ReadToEnd();
            if (txt.Length > 0) {
                JsonNode newJsonNode;
                try {
                    newJsonNode = JsonNode.Parse(txt, null, JsonDocumentReader.DefaultJsonOptions);
                }
                catch (Exception e) {
                    Log.Error($"Invalid json file, reason: {e}");
                    return;
                }
                if (englishJsonNode == null) {
                    englishJsonNode = newJsonNode;
                }
                else {
                    MergeJsonNode(englishJsonNode, newJsonNode);
                }
            }
        }

        public static void SetUsual(bool force = false) {
            if (force) {
                Ok = Get("Usual", "ok");
                Cancel = Get("Usual", "cancel");
                None = Get("Usual", "none");
                Nothing = Get("Usual", "nothing");
                Error = Get("Usual", "error");
                On = Get("Usual", "on");
                Off = Get("Usual", "off");
                Disable = Get("Usual", "disable");
                Enable = Get("Usual", "enable");
                Warning = Get("Usual", "warning");
                Back = Get("Usual", "back");
                Allowed = Get("Usual", "allowed");
                NAllowed = Get("Usual", "not allowed");
                Unknown = Get("Usual", "unknown");
                Yes = Get("Usual", "yes");
                No = Get("Usual", "no");
                Unavailable = Get("Usual", "Unavailable");
                Exists = Get("Usual", "exist");
                Success = Get("Usual", "success");
                Delete = Get("Usual", "delete");
            }
            else {
                Ok ??= Get("Usual", "ok");
                Cancel ??= Get("Usual", "cancel");
                None ??= Get("Usual", "none");
                Nothing ??= Get("Usual", "nothing");
                Error ??= Get("Usual", "error");
                On ??= Get("Usual", "on");
                Off ??= Get("Usual", "off");
                Disable ??= Get("Usual", "disable");
                Enable ??= Get("Usual", "enable");
                Warning ??= Get("Usual", "warning");
                Back ??= Get("Usual", "back");
                Allowed ??= Get("Usual", "allowed");
                NAllowed ??= Get("Usual", "not allowed");
                Unknown ??= Get("Usual", "unknown");
                Yes ??= Get("Usual", "yes");
                No ??= Get("Usual", "no");
                Unavailable ??= Get("Usual", "Unavailable");
                Exists ??= Get("Usual", "exist");
                Success ??= Get("Usual", "success");
                Delete ??= Get("Usual", "delete");
            }
        }

        public static void MergeJsonNode(JsonNode oldNode, JsonNode newNode) {
            if (oldNode == null
                || newNode == null) {
                return;
            }
            switch (newNode.GetValueKind()) {
                case JsonValueKind.Object: {
                    if (oldNode.GetValueKind() == JsonValueKind.Object) {
                        JsonObject oldObject = oldNode.AsObject();
                        JsonObject newObject = newNode.AsObject();
                        foreach (KeyValuePair<string, JsonNode> newChild in newObject) {
                            if (newChild.Value == null) {
                                continue;
                            }
                            if (oldObject.TryGetPropertyValue(newChild.Key, out JsonNode oldChild)) {
                                MergeJsonNode(oldChild, newChild.Value);
                            }
                            else {
                                oldObject.Add(newChild.Key, newChild.Value.DeepClone());
                            }
                        }
                    }
                    else {
                        ReplaceJsonNode(oldNode, newNode.DeepClone());
                    }
                    break;
                }
                case JsonValueKind.Array: {
                    if (oldNode.GetValueKind() == JsonValueKind.Array) {
                        JsonArray oldArray = oldNode.AsArray();
                        JsonArray newArray = newNode.AsArray();
                        if (newArray.Count > oldArray.Count) {
                            for (int i = 0; i < oldArray.Count; i++) {
                                MergeJsonNode(oldArray[i], newArray[i]);
                            }
                            for (int i = oldArray.Count; i < newArray.Count; i++) {
                                oldArray.Add(newArray[i]?.DeepClone());
                            }
                        }
                        else {
                            for (int i = 0; i < newArray.Count; i++) {
                                MergeJsonNode(oldArray[i], newArray[i]);
                            }
                        }
                    }
                    else {
                        ReplaceJsonNode(oldNode, newNode.DeepClone());
                    }
                    break;
                }
                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False: {
                    ReplaceJsonNode(oldNode, newNode.DeepClone());
                    break;
                }
            }
        }

        public static void ReplaceJsonNode(JsonNode oldNode, JsonNode newNode) {
            switch (oldNode.Parent) {
                case JsonObject parentObject:
                    parentObject[oldNode.GetPropertyName()] = newNode;
                    return;
                case JsonArray parentArray: parentArray[oldNode.GetElementIndex()] = newNode; break;
            }
        }

        /// <returns>当前设置中的语言的标识符，如果是在加载完成后获取语言，建议改用CurrentLanguageName</returns>
        public static string LName() => ModsManager.Configs["Language"];

        /// <summary>
        ///     获取在当前语言类名键对应的字符串
        /// </summary>
        /// <param name="className">类名</param>
        /// <param name="key">键</param>
        /// <returns>本地化字符串</returns>
        public static string Get(string className, int key) =>
            //获得键值
            Get(className, key.ToString());

        public static string GetWorldPalette(int index) => Get("WorldPalette", "Colors", index.ToString());

        public static string Get(params string[] key) => Get(out bool _, key);

        public static string Get(out bool r, params string[] keys) { //获得键值
            if (jsonNode == null) {
                if (englishJsonNode == null) {
                    r = false;
                    return string.Join(':', keys);
                }
                jsonNode = englishJsonNode;
            }
            string result = Get(out r, jsonNode, keys);
            if (r) {
                return result;
            }
            if (CurrentLanguageName != "en-US") {
                result = Get(out r, englishJsonNode, keys);
            }
            return result;
        }

        public static string Get(out bool r, JsonNode node, params string[] keys) {
            r = false;
            JsonNode nowNode = node;
            bool flag = false;
            foreach (string key in keys) {
                if (string.IsNullOrEmpty(key)
                    || nowNode == null) {
                    break;
                }
                if (nowNode.GetValueKind() == JsonValueKind.Object) {
                    nowNode = nowNode[key];
                    if (nowNode == null) {
                        break;
                    }
                    flag = true;
                }
                else if (nowNode.GetValueKind() == JsonValueKind.Array
                    && int.TryParse(key, out int num)
                    && num >= 0) {
                    JsonArray array = nowNode.AsArray();
                    if (num < array.Count) {
                        nowNode = array[num];
                        flag = true;
                    }
                    else {
                        break;
                    }
                }
                else {
                    break;
                }
            }
            if (nowNode != null) {
                switch (nowNode.GetValueKind()) {
                    case JsonValueKind.String:
                        r = true;
                        return nowNode.GetValue<string>();
                    case JsonValueKind.Number:
                        r = true;
                        return nowNode.GetValue<decimal>().ToString(CultureInfo.InvariantCulture);
                }
            }
            return flag ? keys.Last() : string.Join(':', keys);
        }

        public static bool TryGet(out string result, params string[] keys) {
            result = Get(out bool r, keys);
            return r;
        }

        public static bool TryGet(out string result, JsonNode node, params string[] keys) {
            result = Get(out bool r, node, keys);
            return r;
        }

        public static string GetWithoutFallback(out bool r, params string[] keys) => Get(out r, jsonNode, keys);

        public static string GetBlock(string blockName, string prop) {
            TryGetBlock(blockName, prop, out string result);
            return result;
        }

        public static bool TryGetBlock(string blockName, string prop, out string result) {
            if (blockName.Length == 0) {
                result = string.Empty;
                return false;
            }
            string[] nm = blockName.Split(':'); //这里不要改成集合表达式，在不同的编译器上会导致bug或者编译失败
            result = Get(out bool r, "Blocks", nm.Length < 2 ? $"{blockName}:0" : blockName, prop);
            if (!r) {
                result = Get(out r, "Blocks", $"{nm[0]}:0", prop);
            }
            return r;
        }

        public static string GetContentWidgets(string name, string prop) => Get("ContentWidgets", name, prop);

        public static string GetContentWidgets(string name, int pos) => Get("ContentWidgets", name, pos.ToString());

        public static string GetDatabase(string name, string prop) => Get("Database", name, prop);

        public static string GetFireworks(string name, string prop) => Get("FireworksBlock", name, prop);

        public static void ChangeLanguage(string languageType) {
            Initialize(languageType);
            CachedLanguageFullNames.Clear();
            if (languageType == "en-US"
                && englishJsonNode != null) {
                jsonNode = englishJsonNode;
                SetUsual(true);
            }
            else {
                foreach (ModEntity c in ModsManager.ModList) {
                    c.LoadLauguage();
                }
                SetUsual(true);
            }
#if !MOBILE
            string title = $"{(SettingsManager.SafeMode ? $"[{LanguageControl.Get("Usual", "safeMode")}]" : "")}{Get("Usual", "gameName")} {ModsManager.ShortGameVersion} - API {ModsManager.APIVersionString}";
#if DEBUG
            title = $"[{Get("Usual", "debug")}]{title}";
#endif
            Window.TitlePrefix = title;
#endif
            Dictionary<string, object> objs = [];
            foreach (KeyValuePair<string, Screen> c in ScreensManager.m_screens) {
                Type type = c.Value.GetType();
#pragma warning disable IL2072
                object obj = Activator.CreateInstance(type);
#pragma warning restore IL2072
                objs.Add(c.Key, obj);
            }
            foreach (KeyValuePair<string, object> c in objs) {
                ScreensManager.m_screens[c.Key] = c.Value as Screen;
            }
            CraftingRecipesManager.Initialize();
            BlocksManager.Blocks[ClothingBlock.Index].Initialize();
            BlocksManager.Blocks[EggBlock.Index].Initialize();
            ClothingSlot.Initialize();
            SubsystemPalette.m_nameFormat = LanguageControl.Get("SubsystemPalette", "1");
            ScreensManager.SwitchScreen("MainMenu");
        }

        public static Dictionary<string, string> CachedLanguageFullNames = [];

        public static void CreateLanguageSelectionDialog(Widget parent) {
            if (CachedLanguageFullNames.Count == 0) {
#if BROWSER
                foreach (string name in LanguageTypes) {
                    CachedLanguageFullNames.Add(
                        name,
                        name switch {
                            "en-US" => "English (United States)",
                            "zh-CN" => "中文 (中国）",
                            "ro-RO" => "română (România)",
                            "ru-RU" => "русский (Россия)",
                            "es-419" => "español (Latinoamérica)",
                            "vi-VN" => "Tiếng Việt (Việt Nam)",
                            "pt-BR" => "Português (Brasil)",
                            "zh-CN-old" => "[旧] 中文 (中国）",
                            _ => $"{name}"
                        }
                    );
                }
#else
                CultureInfo oldUICulture = Thread.CurrentThread.CurrentUICulture;
                try {
                    Thread.CurrentThread.CurrentUICulture = CurrentLanguageCultureInfo;
                    foreach ((string name, CultureInfo cultureInfo) in LanguageTypes) {
                        string nativeName = cultureInfo.NativeName;
                        string displayName = cultureInfo.DisplayName;
                        if (name == "zh-CN-old") {
                            CachedLanguageFullNames.Add(name, $"[旧] {nativeName} - {displayName}");
                        }
                        else {
                            CachedLanguageFullNames.Add(name, nativeName == displayName ? nativeName : $"{nativeName} - {displayName}");
                        }
                    }
                }
                finally {
                    Thread.CurrentThread.CurrentUICulture = oldUICulture;
                }
#endif
            }
            IOrderedEnumerable<KeyValuePair<string, string>> sorted = CachedLanguageFullNames.OrderBy(item => item.Key switch {
                    "en-US" => 0,
                    "zh-CN" => 1,
                    "zh-CN-old" => 1000,
                    _ => 2
                }
            );
            DialogsManager.ShowDialog(
                null,
                new ListSelectionDialog(
                    null,
                    sorted,
                    70f,
                    item => item is KeyValuePair<string, string> pair
                        ? new LabelWidget {
                            Text = pair.Value,
                            Color = pair.Key == CurrentLanguageName ? new Color(50, 150, 35) : Color.White,
                            Margin = new Vector2(20f, 0f),
                            HorizontalAlignment = WidgetAlignment.Near,
                            VerticalAlignment = WidgetAlignment.Center
                        }
                        : null,
                    item => {
                        if (item is KeyValuePair<string, string> pair && pair.Key != CurrentLanguageName) {
                            ChangeLanguage(pair.Key);
                        }
                    }
                )
            );
        }
    }
}