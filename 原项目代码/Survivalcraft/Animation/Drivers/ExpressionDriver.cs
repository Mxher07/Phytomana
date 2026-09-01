using Engine;
using Engine.Animation;
using Engine.Graphics;
using NCalc;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 单个骨骼的表达式配置
    /// </summary>
    public class BoneExpressionConfig {
        /// <summary>
        /// 目标骨骼名称
        /// </summary>
        public string BoneName { get; set; }

        /// <summary>
        /// 位置 X 表达式（默认 "0"）
        /// </summary>
        public string PositionX { get; set; } = "0";

        /// <summary>
        /// 位置 Y 表达式（默认 "0"）
        /// </summary>
        public string PositionY { get; set; } = "0";

        /// <summary>
        /// 位置 Z 表达式（默认 "0"）
        /// </summary>
        public string PositionZ { get; set; } = "0";

        /// <summary>
        /// 旋转 X 表达式（度数，默认 "0"）
        /// </summary>
        public string RotationX { get; set; } = "0";

        /// <summary>
        /// 旋转 Y 表达式（度数，默认 "0"）
        /// </summary>
        public string RotationY { get; set; } = "0";

        /// <summary>
        /// 旋转 Z 表达式（度数，默认 "0"）
        /// </summary>
        public string RotationZ { get; set; } = "0";

        /// <summary>
        /// 缩放 X 表达式（默认 "1"）
        /// </summary>
        public string ScaleX { get; set; } = "1";

        /// <summary>
        /// 缩放 Y 表达式（默认 "1"）
        /// </summary>
        public string ScaleY { get; set; } = "1";

        /// <summary>
        /// 缩放 Z 表达式（默认 "1"）
        /// </summary>
        public string ScaleZ { get; set; } = "1";
    }

    /// <summary>
    /// 表达式驱动器
    /// 使用 NCalc 表达式计算骨骼变换
    /// </summary>
    public class ExpressionDriver : IAnimationDriver {
        public string Name => "Expression";
        public AnimationBlendMode BlendMode { get; set; } = AnimationBlendMode.Override;

        // 骨骼配置列表
        public readonly List<BoneExpressionConfig> m_boneConfigs = new();

        // 编译后的表达式缓存
        public readonly Dictionary<string, Expression> m_expressionCache = new();

        // 缓存每个表达式需要的参数名，避免每次求值时重新提取
        public readonly Dictionary<string, string[]> m_requiredParameters = new();

        // 目标骨骼列表缓存
        public string[] m_cachedTargetBones;

        /// <summary>
        /// 添加骨骼表达式配置
        /// </summary>
        public void AddBoneConfig(BoneExpressionConfig config) {
            if (config == null
                || string.IsNullOrEmpty(config.BoneName)) {
                return;
            }
            m_boneConfigs.Add(config);
            m_cachedTargetBones = null;

            // 预编译表达式
            PrecompileExpression(config.PositionX);
            PrecompileExpression(config.PositionY);
            PrecompileExpression(config.PositionZ);
            PrecompileExpression(config.RotationX);
            PrecompileExpression(config.RotationY);
            PrecompileExpression(config.RotationZ);
            PrecompileExpression(config.ScaleX);
            PrecompileExpression(config.ScaleY);
            PrecompileExpression(config.ScaleZ);
        }

        /// <summary>
        /// 清除所有骨骼配置
        /// </summary>
        public void ClearBoneConfigs() {
            m_boneConfigs.Clear();
            m_cachedTargetBones = null;
        }

        /// <summary>
        /// 获取骨骼配置列表（只读）
        /// </summary>
        public IReadOnlyList<BoneExpressionConfig> BoneConfigs => m_boneConfigs;

        // IAnimationDriver 接口实现
        public string[] TargetBones {
            get {
                if (m_cachedTargetBones == null) {
                    m_cachedTargetBones = m_boneConfigs.Select(c => c.BoneName).ToArray();
                }
                return m_cachedTargetBones;
            }
        }

        // 当前参数（用于表达式求值）
        public AnimationParameters _currentParameters;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _currentParameters = parameters;
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            if (_currentParameters == null) {
                return;
            }
            foreach (BoneExpressionConfig config in m_boneConfigs) {
                ModelBone bone = model.FindBone(config.BoneName, false);
                if (bone == null) {
                    continue;
                }
                try {
                    // 计算变换分量
                    float posX = EvaluateFloat(config.PositionX);
                    float posY = EvaluateFloat(config.PositionY);
                    float posZ = EvaluateFloat(config.PositionZ);
                    float rotX = EvaluateFloat(config.RotationX) * MathF.PI / 180f;
                    float rotY = EvaluateFloat(config.RotationY) * MathF.PI / 180f;
                    float rotZ = EvaluateFloat(config.RotationZ) * MathF.PI / 180f;
                    float scaleX = EvaluateFloat(config.ScaleX);
                    float scaleY = EvaluateFloat(config.ScaleY);
                    float scaleZ = EvaluateFloat(config.ScaleZ);

                    // 构建变换矩阵（缩放 -> 旋转 -> 平移）
                    Matrix transform = Matrix.CreateScale(scaleX, scaleY, scaleZ)
                        * Matrix.CreateRotationX(rotX)
                        * Matrix.CreateRotationY(rotY)
                        * Matrix.CreateRotationZ(rotZ)
                        * Matrix.CreateTranslation(posX, posY, posZ);
                    boneTransforms[bone.Index] = transform;
                }
                catch {
                    // 表达式求值失败时跳过该骨骼
                }
            }
        }

        /// <summary>
        /// 预编译表达式
        /// </summary>
        public void PrecompileExpression(string expression) {
            if (string.IsNullOrEmpty(expression)) {
                return;
            }
            if (!m_expressionCache.ContainsKey(expression)) {
                try {
                    Expression expr = new(expression);
                    expr.Options = ExpressionOptions.NoCache;
                    m_expressionCache[expression] = expr;

                    // 提取并缓存参数名
                    List<string> paramNames = expr.GetParameterNames();
                    m_requiredParameters[expression] = paramNames?.ToArray() ?? Array.Empty<string>();

                    // 预注册自定义函数（只注册一次）
                    AnimationExpressionFunctions.RegisterFunctions(expr);
                }
                catch {
                    // 表达式语法错误，忽略
                }
            }
        }

        /// <summary>
        /// 计算浮点表达式
        /// </summary>
        public float EvaluateFloat(string expression) {
            if (string.IsNullOrEmpty(expression)) {
                return 0f;
            }

            // 检查是否为常量
            if (float.TryParse(expression, out float constant)) {
                return constant;
            }
            if (!m_expressionCache.TryGetValue(expression, out Expression expr)) {
                return 0f;
            }
            try {
                // 绑定参数 - 使用可复用字典
                string[] requiredParams = m_requiredParameters.TryGetValue(expression, out string[] params2) ? params2 : null;
                if (requiredParams != null
                    && requiredParams.Length > 0) {
                    foreach (string paramName in requiredParams) {
                        expr.Parameters[paramName] = _currentParameters.GetValue(paramName);
                    }
                }
                else {
                    expr.Parameters.Clear();
                }

                // 求值（函数已在预编译时注册）
                object result = expr.Evaluate();
                return Convert.ToSingle(result);
            }
            catch {
                return 0f;
            }
        }

        /// <summary>
        /// 清除表达式缓存
        /// </summary>
        public void ClearCache() {
            // 移除事件处理器以避免内存泄漏
            foreach (KeyValuePair<string, Expression> kvp in m_expressionCache) {
                AnimationExpressionFunctions.UnregisterFunctions(kvp.Value);
            }
            m_expressionCache.Clear();
            m_requiredParameters.Clear();
        }
    }
}