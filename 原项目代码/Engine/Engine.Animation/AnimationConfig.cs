using Engine.Animation.RootMotion;
using System.Text.Json.Serialization;

namespace Engine.Animation {
    /// <summary>
    /// 动画完成时执行的动作
    /// </summary>
    public class OnCompleteAction {
        /// <summary>
        /// 动作类型：trigger
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 事件名称（trigger 使用）
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 事件数据（trigger 使用）
        /// </summary>
        public object Data { get; set; }
    }

    /// <summary>
    /// 动画引用配置
    /// </summary>
    public class AnimationReference {
        /// <summary>
        /// 动画来源（animation://名称、driver:驱动器名 或 文件路径）
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 播放速度（静态值或表达式）
        /// 表达式示例："[SpeedAbs] / [WalkSpeed]"
        /// </summary>
        public object SpeedValue { get; set; } = 1f;

        /// <summary>
        /// 是否循环（静态值或表达式）
        /// 表达式示例："not [IsDead]"
        /// </summary>
        public object LoopValue { get; set; } = true;

        /// <summary>
        /// 起始相位 (0-1)，可以是静态值或表达式
        /// </summary>
        public object StartPhaseValue { get; set; } = 0f;

        /// <summary>
        /// 结束相位 (0-1)，可以是静态值或表达式
        /// </summary>
        public object EndPhaseValue { get; set; } = 1f;

        /// <summary>
        /// 是否保留上一动画的姿势（用于平滑过渡）
        /// </summary>
        public bool PreservePose { get; set; } = false;

        /// <summary>
        /// 过渡时长（秒），可以是静态值或表达式
        /// </summary>
        public object BlendDurationValue { get; set; } = 0.3f;

        // 以下 HasXxx 标记对应字段是否在 JSON 中显式设置。
        // 用于别名合并：caller（状态规则/手动 API）显式设置的字段覆盖 alias 默认，
        // 未设置的字段回退到 alias。由 AnimationReferenceConverter.Read 置位。
        public bool HasSpeed;
        public bool HasLoop;
        public bool HasStartPhase;
        public bool HasEndPhase;
        public bool HasBlendDuration;
        public bool HasPreservePose;

        /// <summary>
        /// 驱动器参数（当 Source 为 driver: 时使用）
        /// </summary>
        public Dictionary<string, object> DriverArgs { get; set; }

        /// <summary>
        /// 动画事件配置
        /// </summary>
        public List<AnimationEventConfig> Events { get; set; }

        /// <summary>
        /// 动画完成时执行的动作（非循环动画）
        /// </summary>
        public OnCompleteAction OnComplete { get; set; }

        /// <summary>
        /// 动画被打断时执行的动作（非循环动画播放中被规则切换或层停用，即未自然播完即被切走）。
        /// null 表示该动画不关心打断。触发由 AnimationController.TriggerOnInterruptIfActive 检测。
        /// </summary>
        public OnCompleteAction OnInterrupt { get; set; }

        /// <summary>
        /// 根运动配置
        /// </summary>
        public RootMotionConfig RootMotion { get; set; }

        /// <summary>
        /// 根骨骼旋转覆盖（度）。未设置（HasRootBoneRotation=false）时回退顶层。
        /// 可为 float（绕 Y）或 Vector3（绕 X/Y/Z）。静态值，不支持 NCalc。
        /// 不变量：HasRootBoneRotation == true 时此值必非 null（ReadRootBoneRotationValue 从不返回 null，
        /// MergeWithAlias 保持该不变量）。判过 HasRootBoneRotation 即可直接取值，无需重复判空。
        /// </summary>
        public object RootBoneRotationValue { get; set; }

        /// <summary>
        /// 根骨骼平移覆盖。null=回退顶层。
        /// </summary>
        public Vector3? RootBoneTranslation { get; set; }

        public bool HasRootBoneRotation;
        public bool HasRootBoneTranslation;

        // Cached dynamic properties (avoid repeated allocations)
        public DynamicProperty<float> m_cachedSpeedProperty;
        public DynamicProperty<bool> m_cachedLoopProperty;
        public DynamicProperty<float> m_cachedStartPhaseProperty;
        public DynamicProperty<float> m_cachedEndPhaseProperty;
        public DynamicProperty<float> m_cachedBlendDurationProperty;

        /// <summary>
        /// 创建动态属性包装器（缓存实例）
        /// </summary>
        public DynamicProperty<float> GetSpeedProperty() => m_cachedSpeedProperty ??= new DynamicProperty<float>(SpeedValue);

        public DynamicProperty<bool> GetLoopProperty() => m_cachedLoopProperty ??= new DynamicProperty<bool>(LoopValue);
        public DynamicProperty<float> GetStartPhaseProperty() => m_cachedStartPhaseProperty ??= new DynamicProperty<float>(StartPhaseValue);
        public DynamicProperty<float> GetEndPhaseProperty() => m_cachedEndPhaseProperty ??= new DynamicProperty<float>(EndPhaseValue);
        public DynamicProperty<float> GetBlendDurationProperty() => m_cachedBlendDurationProperty ??= new DynamicProperty<float>(BlendDurationValue);
    }

    /// <summary>
    /// 层配置
    /// </summary>
    public class LayerConfig {
        /// <summary>
        /// 混合模式：override 或 additive
        /// </summary>
        public string BlendMode { get; set; } = "override";

        /// <summary>
        /// 影响的骨骼名称列表（按子树展开：含该骨 + 全部后代）
        /// </summary>
        public string[] Bones { get; set; }

        /// <summary>
        /// 排除的骨骼名称列表（同子树语义，从结果集中扣除）
        /// </summary>
        public string[] BonesExclude { get; set; }

        /// <summary>
        /// 层权重（0-1）。null 表示未设，保留模板值；≤0 视为 1。
        /// </summary>
        public float? Weight { get; set; }

        /// <summary>
        /// 层驱动器配置
        /// </summary>
        public DriverConfig Driver { get; set; }

        /// <summary>
        /// 过渡曲线：linear 或 smoothstep
        /// </summary>
        public string BlendCurve { get; set; } = "linear";
    }

    /// <summary>
    /// 状态规则配置（新格式）
    /// </summary>
    public class StateRuleConfig {
        /// <summary>
        /// 条件表达式（NCalc 语法）
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// 目标动画配置
        /// </summary>
        public AnimationReference Animation { get; set; }

        /// <summary>
        /// 嵌套子规则（与 Animation 互斥）。非空时为分组节点：匹配 Condition 后递归评估子 rules，
        /// 实现"外层失败短路整组"的决策树优化（如 [CrouchFactor]>0.0 失败时跳过所有 crouch 变体）。
        /// </summary>
        public List<StateRuleConfig> Rules { get; set; }

        /// <summary>是否为分组节点（有非空子规则）。与 Animation 互斥。</summary>
        public bool HasRules => Rules != null && Rules.Count > 0;
    }

    /// <summary>
    /// 状态层配置
    /// </summary>
    public class StateLayerConfig {
        /// <summary>
        /// 所属层名称
        /// </summary>
        public string Layer { get; set; }

        /// <summary>
        /// 规则列表
        /// </summary>
        public List<StateRuleConfig> Rules { get; set; } = new();
    }

    /// <summary>
    /// 驱动器配置
    /// </summary>
    public class DriverConfig {
        public string Type { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }

    /// <summary>
    /// 动画配置
    /// </summary>
    public class AnimationConfig {
        /// <summary>
        /// 模板名称
        /// </summary>
        public string Template { get; set; } = "Simple";

        /// <summary>
        /// 根骨骼旋转（度），用于修正模型朝向。
        /// 单数字 = 绕 Y 轴（向后兼容）；Vector3 = 绕 X/Y/Z（数组 [x,y,z] 或对象 {"X":..,"Y":..,"Z":..}）。
        /// 例如：Fox 模型前方 +X，需绕 Y 旋转 90 度面向 +Z。
        /// </summary>
        [JsonConverter(typeof(RootBoneEulerConverter))]
        public Vector3? RootBoneRotation { get; set; }

        /// <summary>
        /// 根骨骼平移（米），用于修正模型原点偏移。
        /// 沿实体本地坐标轴（Y 恒指上方；X/Z 随实体朝向旋转），不被 RootBoneRotation 旋转（R*T 顺序）。
        /// 数组或对象两种写法。
        /// </summary>
        public Vector3? RootBoneTranslation { get; set; }

        /// <summary>
        /// 模型缩放比例
        /// 用于调整模型大小，例如：
        /// - 0.01：厘米单位模型（Fox 等从小型建模软件导出的模型）
        /// - 1.0：米单位模型（默认，大多数 glTF 模型）
        /// - 10.0：放大 10 倍（大型建筑模型可能需要）
        /// </summary>
        public float ModelScale { get; set; } = 1f;

        /// <summary>
        /// 层配置（在此配置驱动器及其属性）
        /// </summary>
        public Dictionary<string, LayerConfig> Layers { get; set; } = new();

        /// <summary>
        /// 状态配置
        /// </summary>
        public Dictionary<string, StateLayerConfig> States { get; set; } = new();

        /// <summary>
        /// 动画引用映射（别名 -> 引用）
        /// </summary>
        public Dictionary<string, AnimationReference> Animations { get; set; } = new();

        /// <summary>
        /// 初始参数值
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// 骨骼别名表（别名 -> 真实骨骼名），由 ComponentModel.SetModel 写入 Model.BoneAliases。
        /// JSON 键名 "boneAliases"（大小写不敏感，自动反序列化）。
        /// </summary>
        public Dictionary<string, string> BoneAliases { get; set; }
    }
}