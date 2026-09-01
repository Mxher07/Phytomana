using System.Text.Json.Nodes;
using Engine;
using Engine.Animation;

namespace Game.Animation {
    /// <summary>
    /// 动画模板注册
    /// 在游戏启动时从配置文件加载所有内置模板
    /// </summary>
    public static class AnimationTemplateRegistration {
        static bool s_registered;
        static readonly object s_lock = new();

        /// <summary>
        /// 是否已注册
        /// </summary>
        public static bool IsRegistered => s_registered;

        /// <summary>
        /// 注册所有内置模板
        /// </summary>
        public static void Register() {
            lock (s_lock) {
                if (s_registered) {
                    return;
                }

                // 从 ContentManager 加载配置文件
                LoadTemplatesFromContent();

                // 如果没有加载到任何模板，使用硬编码的后备方案
                if (AnimationTemplateManager.RegisteredCount == 0) {
                    RegisterFallbackTemplates();
                }
                s_registered = true;
            }
        }

        /// <summary>
        /// 从 ContentManager 加载模板配置文件
        /// </summary>
        static void LoadTemplatesFromContent() {
            // 获取 AnimationTemplates 目录下的所有文件
            ReadOnlyList<ContentInfo> contents = ContentManager.List("AnimationTemplates");
            foreach (ContentInfo contentInfo in contents) {
                // 只处理 .template.json 文件
                if (contentInfo.Filename == null
                    || !contentInfo.Filename.EndsWith(".template.json")) {
                    continue;
                }
                try {
                    // 使用 ContentInfo 的流加载模板
                    using Stream stream = contentInfo.Duplicate();
                    LoadTemplateWithInheritance(stream, contentInfo.ContentPath);
                }
                catch (JsonInheritanceException ex) {
                    // 记录继承相关的详细错误信息
                    Log.Error($"[AnimationTemplate] Failed to load template '{contentInfo.ContentPath}': {ex.Message}");
                }
                catch (Exception ex) {
                    // 记录其他异常
                    Log.Error($"[AnimationTemplate] Failed to load template '{contentInfo.ContentPath}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 加载模板并处理继承
        /// </summary>
        /// <param name="stream">模板文件流</param>
        /// <param name="contentPath">当前模板的内容路径（不含后缀）</param>
        static void LoadTemplateWithInheritance(Stream stream, string contentPath) {
            // 读取 JSON 内容
            using StreamReader reader = new(stream);
            string json = reader.ReadToEnd();
            if (string.IsNullOrEmpty(json)) {
                return;
            }

            // 解析为 JsonNode
            JsonNode jsonNode = JsonNode.Parse(json);
            if (jsonNode == null) {
                return;
            }

            // 创建父配置加载回调
            string currentDirectory = GetDirectoryPath(contentPath);
            Func<string, JsonNode> parentLoader = relativePath => { return LoadParentTemplate(currentDirectory, relativePath); };

            // 解析继承
            jsonNode = JsonInheritanceHelper.ResolveInheritance(jsonNode, parentLoader);

            // 加载模板
            AnimationTemplate template = AnimationTemplateManager.LoadFromJsonNode(jsonNode);
            if (template != null) {
                AnimationTemplateManager.Register(template.Name, template);
            }
        }

        /// <summary>
        /// 加载父模板配置
        /// </summary>
        /// <param name="currentDirectory">当前文件所在目录的内容路径</param>
        /// <param name="relativePath">相对路径</param>
        /// <returns>父配置的 JsonNode，如果不存在则返回 null</returns>
        static JsonNode LoadParentTemplate(string currentDirectory, string relativePath) {
            // 解析父配置的完整路径
            string parentPath = CombinePath(currentDirectory, relativePath);

            // 尝试通过 ContentManager 加载
            Stream stream = ContentManager.GetStream(parentPath);
            if (stream == null) {
                // 尝试添加 .template.json 后缀
                stream = ContentManager.GetStream(parentPath + ".template.json");
                if (stream == null) {
                    return null;
                }
            }
            try {
                using StreamReader reader = new(stream);
                string json = reader.ReadToEnd();
                if (string.IsNullOrEmpty(json)) {
                    return null;
                }
                return JsonNode.Parse(json);
            }
            catch {
                return null;
            }
        }

        /// <summary>
        /// 获取内容路径的目录部分
        /// </summary>
        /// <param name="contentPath">内容路径（如 "AnimationTemplates/FourLegged.template"）</param>
        /// <returns>目录路径（如 "AnimationTemplates"）</returns>
        static string GetDirectoryPath(string contentPath) {
            if (string.IsNullOrEmpty(contentPath)) {
                return string.Empty;
            }
            int lastSlash = contentPath.LastIndexOf('/');
            if (lastSlash > 0) {
                return contentPath.Substring(0, lastSlash);
            }
            return string.Empty;
        }

        /// <summary>
        /// 合并当前目录路径和相对路径
        /// </summary>
        /// <param name="currentDirectory">当前目录路径（如 "AnimationTemplates"）</param>
        /// <param name="relativePath">相对路径（如 "Base.template.json" 或 "../Shared/Base.template.json"）</param>
        /// <returns>合并后的路径</returns>
        static string CombinePath(string currentDirectory, string relativePath) {
            if (string.IsNullOrEmpty(relativePath)) {
                return string.Empty;
            }

            // 处理绝对路径（以 / 开头）
            if (relativePath.StartsWith("/")) {
                return relativePath.Substring(1);
            }

            // 处理相对路径
            if (string.IsNullOrEmpty(currentDirectory)) {
                return relativePath;
            }

            // 分割路径段
            List<string> parts = new(currentDirectory.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
            string[] relativeParts = relativePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in relativeParts) {
                if (part == "..") {
                    // 返回上一级目录
                    if (parts.Count > 0) {
                        parts.RemoveAt(parts.Count - 1);
                    }
                }
                else if (part != ".") {
                    parts.Add(part);
                }
            }
            return string.Join("/", parts);
        }

        /// <summary>
        /// 硬编码后备方案（当配置文件不存在时使用）
        /// </summary>
        static void RegisterFallbackTemplates() {
            // 保留最基础的 Simple 模板作为后备
            AnimationTemplateManager.Register(
                "Simple",
                new AnimationTemplate(
                    "Simple",
                    new Dictionary<string, LayerDefinition> { ["Base"] = new(0, AnimationBlendMode.Override) }
                )
            );
        }

        /// <summary>
        /// 重置注册状态（仅用于测试）
        /// </summary>
        public static void Reset() {
            lock (s_lock) {
                s_registered = false;
                AnimationTemplateManager.Clear();
            }
        }
    }
}