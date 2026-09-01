using System.Text.Json;
using System.Text.Json.Nodes;

namespace Engine.Animation {
    /// <summary>
    /// 动画模板管理器
    /// 提供模板的注册、获取和管理功能
    /// </summary>
    public static class AnimationTemplateManager {
        public static readonly Dictionary<string, AnimationTemplate> s_templates = new();

        public static readonly JsonSerializerOptions s_jsonOptions = new() {
            PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true
        };

        /// <summary>
        /// 已注册的模板数量
        /// </summary>
        public static int RegisteredCount => s_templates.Count;

        /// <summary>
        /// 注册动画模板
        /// </summary>
        /// <param name="name">模板名称</param>
        /// <param name="template">模板实例</param>
        public static void Register(string name, AnimationTemplate template) {
            if (string.IsNullOrEmpty(name)) {
                return;
            }
            s_templates[name] = template;
        }

        /// <summary>
        /// 获取动画模板
        /// </summary>
        /// <param name="name">模板名称</param>
        /// <returns>模板实例，如果未注册则返回 null</returns>
        public static AnimationTemplate Get(string name) {
            if (string.IsNullOrEmpty(name)) {
                return null;
            }
            return s_templates.TryGetValue(name, out AnimationTemplate template) ? template : null;
        }

        /// <summary>
        /// 检查模板是否存在
        /// </summary>
        /// <param name="name">模板名称</param>
        /// <returns>是否存在</returns>
        public static bool Exists(string name) => !string.IsNullOrEmpty(name) && s_templates.ContainsKey(name);

        /// <summary>
        /// 清除所有注册（主要用于测试）
        /// </summary>
        public static void Clear() {
            s_templates.Clear();
        }

        /// <summary>
        /// 获取所有已注册的模板名称
        /// </summary>
        public static IEnumerable<string> GetRegisteredNames() => s_templates.Keys;

        #region JSON 加载方法

        /// <summary>
        /// 从 JsonNode 加载动画模板
        /// 此方法接收已处理继承的 JsonNode，仅执行反序列化和模板创建
        /// </summary>
        /// <param name="jsonNode">已处理继承的 JSON 节点</param>
        /// <returns>加载的模板实例，如果失败则返回 null</returns>
        public static AnimationTemplate LoadFromJsonNode(JsonNode jsonNode) {
            if (jsonNode == null) {
                return null;
            }
            try {
                AnimationTemplateConfig config = jsonNode.Deserialize<AnimationTemplateConfig>(s_jsonOptions);
                if (config == null
                    || string.IsNullOrEmpty(config.Name)) {
                    return null;
                }
                AnimationTemplate template = CreateTemplateFromConfig(config);
                if (template != null) {
                    // 自动注册模板
                    Register(template.Name, template);
                }
                return template;
            }
            catch (JsonException) {
                return null;
            }
        }

        /// <summary>
        /// 从文件加载动画模板
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>加载的模板实例，如果失败则返回 null</returns>
        public static AnimationTemplate LoadFromFile(string path) {
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            try {
                string json = File.ReadAllText(path);
                JsonNode jsonNode = JsonNode.Parse(json);
                return LoadFromJsonNode(jsonNode);
            }
            catch {
                return null;
            }
        }

        #endregion

        #region 私有方法

        public static AnimationTemplate CreateTemplateFromConfig(AnimationTemplateConfig config) {
            if (config == null
                || string.IsNullOrEmpty(config.Name)) {
                return null;
            }

            // 转换层定义
            Dictionary<string, LayerDefinition> layers = new();
            if (config.Layers != null) {
                foreach ((string name, TemplateLayerConfig layerConfig) in config.Layers) {
                    if (layerConfig == null) {
                        continue;
                    }
                    LayerDefinition layer = CreateLayerFromConfig(layerConfig);
                    if (layer != null) {
                        layers[name] = layer;
                    }
                }
            }

            // 转换必需骨骼列表
            string[] requiredBones = null;
            if (config.RequiredBones != null
                && config.RequiredBones.Count > 0) {
                requiredBones = config.RequiredBones.ToArray();
            }
            return new AnimationTemplate(config.Name, layers, requiredBones);
        }

        public static LayerDefinition CreateLayerFromConfig(TemplateLayerConfig config) {
            if (config == null) {
                return null;
            }
            AnimationBlendMode blendMode = ParseBlendMode(config.BlendMode);
            string[] boneMask = null;
            if (config.BoneMask != null
                && config.BoneMask.Count > 0) {
                boneMask = config.BoneMask.ToArray();
            }
            string[] boneMaskExclude = null;
            if (config.BoneMaskExclude != null
                && config.BoneMaskExclude.Count > 0) {
                boneMaskExclude = config.BoneMaskExclude.ToArray();
            }
            float weight = config.Weight ?? 1f;
            if (weight <= 0f) {
                weight = 1f; // <=0 视为未设，回退默认 1
            }
            return new LayerDefinition(config.Index, blendMode, boneMask, boneMaskExclude, weight);
        }

        public static AnimationBlendMode ParseBlendMode(string value) {
            if (string.IsNullOrEmpty(value)) {
                return AnimationBlendMode.Override;
            }
            return value.ToLowerInvariant() switch {
                "override" => AnimationBlendMode.Override,
                "additive" => AnimationBlendMode.Additive,
                _ => AnimationBlendMode.Override
            };
        }

        #endregion
    }
}