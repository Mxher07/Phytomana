using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// 动画配置加载器
    /// 负责从 JSON 文件加载 AnimationConfig 并解析动画引用
    /// </summary>
    public class AnimationConfigLoader {
        /// <summary>
        /// 动画引用协议前缀
        /// </summary>
        public const string AnimationProtocolPrefix = "animation://";

        /// <summary>
        /// 缓存的 JsonSerializerOptions（避免每次创建新实例）
        /// </summary>
        public static readonly JsonSerializerOptions s_jsonOptions = new() {
            PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true
        };

        // 静态构造函数注册自定义转换器
        static AnimationConfigLoader() {
            s_jsonOptions.Converters.Add(new AnimationReferenceConverter());
            s_jsonOptions.Converters.Add(new Vector3JsonConverter());
            s_jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <summary>
        /// 加载动画的回调函数
        /// 参数：动画名称或路径，返回：ModelAnimation 实例
        /// </summary>
        public Func<string, ModelAnimation> LoadAnimationCallback { get; set; }

        /// <summary>
        /// 从 JsonNode 加载配置
        /// 此方法接收已处理继承的 JsonNode，调用方负责在调用前解析继承关系
        /// </summary>
        /// <param name="jsonNode">已处理继承的 JsonNode 对象</param>
        /// <returns>加载的 AnimationConfig 实例</returns>
        /// <exception cref="ArgumentNullException">jsonNode 为 null</exception>
        /// <exception cref="JsonException">JSON 解析失败</exception>
        public AnimationConfig LoadFromJsonNode(JsonNode jsonNode) {
            if (jsonNode == null) {
                throw new ArgumentNullException(nameof(jsonNode));
            }
            AnimationConfig config = jsonNode.Deserialize<AnimationConfig>(s_jsonOptions);
            if (config == null) {
                throw new JsonException("Failed to deserialize AnimationConfig: result is null");
            }

            // 验证配置
            ValidateConfig(config);
            return config;
        }

        /// <summary>
        /// 解析动画引用，获取实际的 ModelAnimation
        /// </summary>
        /// <param name="reference">动画引用配置</param>
        /// <param name="model">目标模型（用于动画查找）</param>
        /// <returns>解析后的 ModelAnimation，如果无法解析则返回 null</returns>
        public ModelAnimation ResolveAnimation(AnimationReference reference, Model model) {
            if (reference == null
                || string.IsNullOrEmpty(reference.Source)) {
                return null;
            }
            string source = reference.Source;

            // 处理 animation:// 协议
            if (source.StartsWith(AnimationProtocolPrefix)) {
                string animationName = source.Substring(AnimationProtocolPrefix.Length);
                return FindAnimationInModel(model, animationName);
            }

            // 使用回调加载外部动画
            if (LoadAnimationCallback != null) {
                return LoadAnimationCallback(source);
            }
            return null;
        }

        /// <summary>
        /// 解析所有动画引用
        /// </summary>
        /// <param name="config">动画配置</param>
        /// <param name="model">目标模型</param>
        /// <returns>解析后的动画字典（别名 -> ModelAnimation）</returns>
        public Dictionary<string, ModelAnimation> ResolveAllAnimations(AnimationConfig config, Model model) {
            Dictionary<string, ModelAnimation> result = new();
            if (config?.Animations == null) {
                return result;
            }
            foreach (KeyValuePair<string, AnimationReference> kvp in config.Animations) {
                string alias = kvp.Key;
                AnimationReference reference = kvp.Value;
                ModelAnimation animation = ResolveAnimation(reference, model);
                if (animation != null) {
                    result[alias] = animation;
                }
            }
            return result;
        }

        /// <summary>
        /// 验证配置的有效性
        /// </summary>
        /// <param name="config">要验证的配置</param>
        /// <exception cref="AnimationConfigValidationException">验证失败</exception>
        public void ValidateConfig(AnimationConfig config) {
            if (config == null) {
                throw new AnimationConfigValidationException("Config is null");
            }
            List<string> errors = new();

            // 验证模板名称
            if (string.IsNullOrEmpty(config.Template)) {
                errors.Add("Template name is required");
            }
            else if (!AnimationTemplateManager.Exists(config.Template)) {
                errors.Add($"Unknown template: {config.Template}");
            }

            // 顶层 rootBoneRotation / rootBoneTranslation 有限性
            if (config.RootBoneRotation.HasValue) {
                Vector3 r = config.RootBoneRotation.Value;
                if (!IsFinite(r.X) || !IsFinite(r.Y) || !IsFinite(r.Z)) {
                    errors.Add("rootBoneRotation has non-finite component");
                }
            }
            if (config.RootBoneTranslation.HasValue) {
                Vector3 t = config.RootBoneTranslation.Value;
                if (!IsFinite(t.X) || !IsFinite(t.Y) || !IsFinite(t.Z)) {
                    errors.Add("rootBoneTranslation has non-finite component");
                }
            }

            // 验证动画引用
            if (config.Animations != null) {
                int index = 0;
                foreach (KeyValuePair<string, AnimationReference> kvp in config.Animations) {
                    string alias = kvp.Key;
                    AnimationReference reference = kvp.Value;
                    if (string.IsNullOrEmpty(alias)) {
                        errors.Add($"Animation alias at index {index} is empty");
                    }
                    if (reference != null) {
                        if (string.IsNullOrEmpty(reference.Source)) {
                            errors.Add($"Animation '{alias}': Source is required");
                        }

                        // Validate SpeedValue
                        if (reference.SpeedValue != null) {
                            // If it's a number, validate the range
                            if (reference.SpeedValue is float speedFloat
                                && speedFloat <= 0) {
                                errors.Add($"Animation '{alias}': Speed must be positive (got {speedFloat})");
                            }
                            else if (reference.SpeedValue is int speedInt
                                && speedInt <= 0) {
                                errors.Add($"Animation '{alias}': Speed must be positive (got {speedInt})");
                            }
                            else if (reference.SpeedValue is string speedStr
                                && !ExpressionEvaluator.IsExpression(speedStr)) {
                                // Static string value that's not an expression - try to parse
                                if (float.TryParse(speedStr, out float parsedSpeed)
                                    && parsedSpeed <= 0) {
                                    errors.Add($"Animation '{alias}': Speed must be positive (got {speedStr})");
                                }
                            }
                        }

                        // Validate StartPhaseValue
                        if (reference.StartPhaseValue != null) {
                            if (reference.StartPhaseValue is float startFloat
                                && (startFloat < 0 || startFloat > 1)) {
                                errors.Add($"Animation '{alias}': startPhase must be between 0 and 1 (got {startFloat})");
                            }
                            else if (reference.StartPhaseValue is int startInt
                                && (startInt < 0 || startInt > 1)) {
                                errors.Add($"Animation '{alias}': startPhase must be between 0 and 1 (got {startInt})");
                            }
                        }

                        // Validate EndPhaseValue
                        if (reference.EndPhaseValue != null) {
                            if (reference.EndPhaseValue is float endFloat
                                && (endFloat < 0 || endFloat > 1)) {
                                errors.Add($"Animation '{alias}': endPhase must be between 0 and 1 (got {endFloat})");
                            }
                            else if (reference.EndPhaseValue is int endInt
                                && (endInt < 0 || endInt > 1)) {
                                errors.Add($"Animation '{alias}': endPhase must be between 0 and 1 (got {endInt})");
                            }
                        }

                        // Validate BlendDurationValue
                        if (reference.BlendDurationValue != null) {
                            if (reference.BlendDurationValue is float blendFloat
                                && blendFloat < 0) {
                                errors.Add($"Animation '{alias}': BlendDuration cannot be negative (got {blendFloat})");
                            }
                            else if (reference.BlendDurationValue is int blendInt
                                && blendInt < 0) {
                                errors.Add($"Animation '{alias}': BlendDuration cannot be negative (got {blendInt})");
                            }
                        }

                        // Validate RootBoneRotationValue（静态值，不支持 NCalc）。
                        // 字符串/NCalc 表达式在反序列化时已被 ReadRootBoneRotationValue 拒绝（抛 JsonException），此处不重复检查。
                        // 不变量：HasRootBoneRotation == true 时 RootBoneRotationValue 必非 null。
                        if (reference.HasRootBoneRotation) {
                            if (reference.RootBoneRotationValue is Vector3 rv) {
                                if (!IsFinite(rv.X) || !IsFinite(rv.Y) || !IsFinite(rv.Z)) {
                                    errors.Add($"Animation '{alias}': rootBoneRotation has non-finite component");
                                }
                            }
                            else if (reference.RootBoneRotationValue is float rf && !IsFinite(rf)) {
                                errors.Add($"Animation '{alias}': rootBoneRotation must be finite (got {rf})");
                            }
                        }
                        // Validate RootBoneTranslation
                        if (reference.HasRootBoneTranslation
                            && reference.RootBoneTranslation.HasValue) {
                            Vector3 tv = reference.RootBoneTranslation.Value;
                            if (!IsFinite(tv.X) || !IsFinite(tv.Y) || !IsFinite(tv.Z)) {
                                errors.Add($"Animation '{alias}': rootBoneTranslation has non-finite component");
                            }
                        }
                    }
                    index++;
                }
            }

            // 验证层配置
            if (config.Layers != null) {
                foreach (KeyValuePair<string, LayerConfig> kvp in config.Layers) {
                    string layerName = kvp.Key;
                    LayerConfig layerConfig = kvp.Value;
                    if (string.IsNullOrEmpty(layerName)) {
                        errors.Add("Layer name cannot be empty");
                    }
                    if (layerConfig?.Driver != null
                        && string.IsNullOrEmpty(layerConfig.Driver.Type)) {
                        errors.Add($"Layer '{layerName}': Driver Type is required");
                    }
                }
            }

            // 验证状态配置
            if (config.States != null) {
                foreach (KeyValuePair<string, StateLayerConfig> kvp in config.States) {
                    string trackName = kvp.Key;
                    StateLayerConfig trackConfig = kvp.Value;
                    if (string.IsNullOrEmpty(trackName)) {
                        errors.Add("State track name cannot be empty");
                    }
                    if (trackConfig?.Rules != null) {
                        ValidateRules(trackConfig.Rules, $"state '{trackName}'", errors);
                    }
                }
            }
            // 递归验证状态规则（支持嵌套 rules 决策树）
            void ValidateRules(List<StateRuleConfig> rules, string path, List<string> errors) {
                for (int i = 0; i < rules.Count; i++) {
                    StateRuleConfig rule = rules[i];
                    string rulePath = $"{path}.{i}";
                    if (string.IsNullOrEmpty(rule.Condition)) {
                        errors.Add($"State rule {rulePath}: Condition is required");
                    }
                    if (rule.Rules != null && rule.Rules.Count == 0) {
                        errors.Add($"State rule {rulePath}: Rules cannot be empty (omit the field or add a child rule)");
                    }
                    if (rule.HasRules && rule.Animation != null) {
                        errors.Add($"State rule {rulePath}: cannot have both Rules and Animation");
                    }
                    if (rule.HasRules) {
                        ValidateRules(rule.Rules, rulePath, errors);
                    }
                }
            }

            if (errors.Count > 0) {
                throw new AnimationConfigValidationException($"Animation config validation failed:\n{string.Join("\n", errors)}", errors);
            }
        }

        private static bool IsFinite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);

        /// <summary>
        /// 在模型中查找指定名称的动画
        /// </summary>
        public ModelAnimation FindAnimationInModel(Model model, string animationName) {
            if (model == null
                || string.IsNullOrEmpty(animationName)) {
                return null;
            }

            // 尝试精确匹配
            foreach (ModelAnimation anim in model.Animations) {
                if (anim.Name == animationName) {
                    return anim;
                }
            }

            // 尝试忽略大小写匹配
            foreach (ModelAnimation anim in model.Animations) {
                if (string.Equals(anim.Name, animationName, StringComparison.OrdinalIgnoreCase)) {
                    return anim;
                }
            }
            return null;
        }

        /// <summary>
        /// 将配置转换为 AnimationController
        /// </summary>
        /// <param name="config">动画配置</param>
        /// <param name="model">目标模型</param>
        /// <returns>配置好的 AnimationController 实例</returns>
        public AnimationController CreateController(AnimationConfig config, Model model) {
            if (config == null) {
                throw new ArgumentNullException(nameof(config));
            }
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }

            // 获取模板名称
            string templateName = config.Template ?? "Simple";

            // 创建控制器
            AnimationController controller = new(model, templateName);

            // 设置根骨骼变换配置（rotation 度→弧度由 controller 内部转；translation 直传）
            controller.SetRootRotationConfig(config.RootBoneRotation);
            controller.SetRootTranslationConfig(config.RootBoneTranslation);
            // snap effective = 顶层配置目标，避免启动时无谓混合
            controller.SnapRootTransformToConfig();

            // 设置模型缩放
            controller.ModelScale = config.ModelScale;

            // 设置初始参数
            if (config.Parameters != null) {
                foreach (KeyValuePair<string, object> kvp in config.Parameters) {
                    SetParameterByType(controller.Parameters, kvp.Key, kvp.Value);
                }
            }

            // 配置层驱动器
            if (config.Layers != null) {
                foreach (KeyValuePair<string, LayerConfig> kvp in config.Layers) {
                    string layerName = kvp.Key;
                    LayerConfig layerConfig = kvp.Value;

                    // 查找层
                    AnimationLayer layer = controller.Layers.FirstOrDefault(l => l.Name == layerName);
                    if (layer == null) {
                        continue;
                    }

                    // 应用骨骼遮罩配置（覆盖模板中的默认值）
                    if (layerConfig?.Bones != null) {
                        layer.BoneMask = layerConfig.Bones.Length > 0 ? layerConfig.Bones : null;
                    }
                    if (layerConfig?.BonesExclude != null) {
                        layer.BoneMaskExclude = layerConfig.BonesExclude.Length > 0 ? layerConfig.BonesExclude : null;
                    }
                    // 应用层权重（覆盖模板值；<=0 视为 1）
                    if (layerConfig?.Weight.HasValue == true) {
                        float w = layerConfig.Weight.Value > 0 ? layerConfig.Weight.Value : 1f;
                        layer.Weight = w;
                        layer.m_configuredWeight = w; // 同步配置权重（权重渐入/中断恢复目标）
                    }
                    // 应用过渡曲线
                    if (!string.IsNullOrEmpty(layerConfig.BlendCurve)) {
                        BlendCurve curve = layerConfig.BlendCurve.Equals("smoothstep", StringComparison.OrdinalIgnoreCase)
                            ? BlendCurve.Smoothstep
                            : BlendCurve.Linear;
                        layer.Curve = curve;
                        layer.m_transition.Curve = curve;
                    }
                    if (layerConfig?.Driver != null) {
                        IAnimationDriver driver = CreateDriver(layerConfig.Driver.Type);
                        if (driver != null) {
                            if (layerConfig.Driver.Properties != null) {
                                ApplyDriverProperties(driver, layerConfig.Driver.Properties);
                            }
                            controller.SetDriver(layerName, driver);
                        }
                    }
                }
            }

            // 配置状态规则
            if (config.States != null
                && config.States.Count > 0) {
                controller.SetStateConfigs(config.States);
            }

            // 设置动画别名引用（用于状态规则中的别名解析）
            if (config.Animations != null
                && config.Animations.Count > 0) {
                controller.SetAnimationReferences(config.Animations);
            }
            return controller;
        }

        /// <summary>
        /// 创建驱动器实例
        /// 优先从 AnimationDriverManager 查找，找不到时回退到反射
        /// </summary>
        public IAnimationDriver CreateDriver(string type) {
            if (string.IsNullOrEmpty(type)) {
                return null;
            }

            // 优先从 AnimationDriverManager 查找
            IAnimationDriver driver = AnimationDriverManager.Create(type);
            if (driver != null) {
                return driver;
            }

            // 最后尝试通过反射创建
            //return CreateGameDriver(type);
            throw new Exception($"Driver \"{type}\" not found");
        }

        /// <summary>
        /// 通过反射创建游戏层驱动器（回退方案）
        /// </summary>
        public IAnimationDriver CreateGameDriver(string typeName) {
            try {
                // 尝试的类型名（原始名和加 Driver 后缀）
                string[] typeNames = new[] { typeName, typeName + "Driver" };

                // 尝试的命名空间
                string[] namespaces = new[] { "Game.Animation.Drivers", "Game" };
                foreach (string tname in typeNames) {
                    foreach (string ns in namespaces) {
                        string fullName = $"{ns}.{tname}";
                        Type type = Serialization.TypeCache.FindType(fullName, true, false);
                        if (type != null) {
                            return Activator.CreateInstance(type) as IAnimationDriver;
                        }
                    }
                }

                // 尝试完整类型名（用户可能提供了完整名称）
                Type directType = Serialization.TypeCache.FindType(typeName, true, false);
                if (directType != null) {
                    return Activator.CreateInstance(directType) as IAnimationDriver;
                }
                return null;
            }
            catch {
                return null;
            }
        }

        /// <summary>
        /// 应用驱动器属性（使用 PropertySetterCache 优化性能）
        /// </summary>
        public void ApplyDriverProperties(IAnimationDriver driver, Dictionary<string, object> properties) {
            if (properties == null
                || driver == null) {
                return;
            }
            foreach (KeyValuePair<string, object> kvp in properties) {
                try {
                    // 获取属性类型以进行值转换
                    Type driverType = driver.GetType();
                    PropertyInfo property = driverType.GetProperty(kvp.Key);
                    if (property == null
                        || !property.CanWrite) {
                        continue;
                    }
                    object value = ConvertValue(kvp.Value, property.PropertyType);

                    // 处理嵌套对象
                    if (value is Dictionary<string, object> nestedDict) {
                        value = CreateNestedObject(property.PropertyType, nestedDict);
                    }
                    if (value != null) {
                        // 使用缓存的属性设置器
                        PropertySetterCache.SetProperty(driver, kvp.Key, value);
                    }
                }
                catch {
                    // 忽略转换失败
                }
            }
        }

        /// <summary>
        /// 创建嵌套对象并设置属性（使用 PropertySetterCache 优化性能）
        /// </summary>
        public object CreateNestedObject(Type targetType, Dictionary<string, object> properties) {
            try {
                // 使用缓存的创建器
                object obj = PropertySetterCache.CreateAndSetProperties(targetType, null);
                if (obj == null) {
                    return null;
                }
                foreach (KeyValuePair<string, object> kvp in properties) {
                    PropertyInfo property = targetType.GetProperty(kvp.Key);
                    if (property == null
                        || !property.CanWrite) {
                        continue;
                    }
                    object value = ConvertValue(kvp.Value, property.PropertyType);
                    if (value is Dictionary<string, object> nestedDict) {
                        value = CreateNestedObject(property.PropertyType, nestedDict);
                    }
                    if (value != null) {
                        PropertySetterCache.SetProperty(obj, kvp.Key, value);
                    }
                }
                return obj;
            }
            catch {
                return null;
            }
        }

        /// <summary>
        /// 转换值到目标类型
        /// </summary>
        public object ConvertValue(object value, Type targetType) {
            if (value == null) {
                return null;
            }

            // 处理 JsonElement（来自 System.Text.Json）
            if (value is JsonElement jsonElement) {
                return ConvertJsonElement(jsonElement, targetType);
            }
            if (targetType == typeof(float)) {
                return Convert.ToSingle(value);
            }
            if (targetType == typeof(double)) {
                return Convert.ToDouble(value);
            }
            if (targetType == typeof(int)) {
                return Convert.ToInt32(value);
            }
            if (targetType == typeof(bool)) {
                return Convert.ToBoolean(value);
            }
            if (targetType == typeof(string)) {
                return value.ToString();
            }
            return Convert.ChangeType(value, targetType);
        }

        /// <summary>
        /// 从 JsonElement 转换值
        /// </summary>
        public object ConvertJsonElement(JsonElement element, Type targetType) {
            // 处理嵌套对象
            if (element.ValueKind == JsonValueKind.Object) {
                Dictionary<string, object> dict = new();
                foreach (JsonProperty prop in element.EnumerateObject()) {
                    dict[prop.Name] = prop.Value;
                }
                return dict;
            }

            // 处理数组
            if (element.ValueKind == JsonValueKind.Array) {
                if (targetType == typeof(float[])) {
                    float[] arr = new float[element.GetArrayLength()];
                    int i = 0;
                    foreach (JsonElement item in element.EnumerateArray()) {
                        arr[i++] = item.GetSingle();
                    }
                    return arr;
                }
                // 其他数组类型可在此扩展
            }

            // 处理字符串 - 可能是表达式
            if (element.ValueKind == JsonValueKind.String) {
                string stringValue = element.GetString();

                // 检查是否是表达式
                if (ExpressionEvaluator.IsExpression(stringValue)) {
                    // 返回原始字符串，让 DynamicProperty 处理
                    return stringValue;
                }
                return stringValue;
            }
            if (targetType == typeof(float)) {
                return element.GetSingle();
            }
            if (targetType == typeof(double)) {
                return element.GetDouble();
            }
            if (targetType == typeof(int)) {
                return element.GetInt32();
            }
            if (targetType == typeof(bool)) {
                return element.GetBoolean();
            }
            if (targetType == typeof(string)) {
                return element.GetString();
            }

            // 默认返回字符串
            return element.ToString();
        }

        /// <summary>
        /// 根据值类型设置参数
        /// </summary>
        public void SetParameterByType(AnimationParameters parameters, string name, object value) {
            if (value == null) {
                return;
            }

            // 处理 JsonElement
            if (value is JsonElement jsonElement) {
                switch (jsonElement.ValueKind) {
                    case JsonValueKind.Number: parameters.SetFloat(name, jsonElement.GetSingle()); break;
                    case JsonValueKind.True:
                    case JsonValueKind.False: parameters.SetBool(name, jsonElement.GetBoolean()); break;
                    case JsonValueKind.String:
                        // 字符串参数暂不处理
                        break;
                }
                return;
            }
            switch (value) {
                case float f: parameters.SetFloat(name, f); break;
                case double d: parameters.SetFloat(name, (float)d); break;
                case int i: parameters.SetFloat(name, i); break;
                case bool b: parameters.SetBool(name, b); break;
                case Vector3 v: parameters.SetVector3(name, v); break;
                default:
                    // 尝试转换为 float
                    if (double.TryParse(value.ToString(), out double num)) {
                        parameters.SetFloat(name, (float)num);
                    }
                    break;
            }
        }

        /// <summary>
        /// 获取配置中定义的动画别名列表
        /// </summary>
        /// <param name="config">动画配置</param>
        /// <returns>动画别名列表</returns>
        public IReadOnlyList<string> GetAnimationAliases(AnimationConfig config) {
            List<string> aliases = new();
            if (config?.Animations != null) {
                foreach (KeyValuePair<string, AnimationReference> kvp in config.Animations) {
                    aliases.Add(kvp.Key);
                }
            }
            return aliases;
        }

        /// <summary>
        /// 获取动画引用配置
        /// </summary>
        /// <param name="config">动画配置</param>
        /// <param name="alias">动画别名</param>
        /// <returns>动画引用配置，如果不存在则返回 null</returns>
        public AnimationReference GetAnimationReference(AnimationConfig config, string alias) {
            if (config?.Animations == null
                || string.IsNullOrEmpty(alias)) {
                return null;
            }
            return config.Animations.TryGetValue(alias, out AnimationReference reference) ? reference : null;
        }
    }

    /// <summary>
    /// 动画配置验证异常
    /// </summary>
    public class AnimationConfigValidationException : Exception {
        /// <summary>
        /// 验证错误列表
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        public AnimationConfigValidationException(string message) : base(message) => Errors = new List<string>();

        public AnimationConfigValidationException(string message, IEnumerable<string> errors) : base(message) => Errors = new List<string>(errors);

        public AnimationConfigValidationException(string message, Exception innerException) : base(message, innerException) =>
            Errors = new List<string>();
    }
}