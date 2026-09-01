namespace Engine.Animation.RootMotion {
    /// <summary>
    /// 位移应用模式
    /// </summary>
    public enum TranslationMode {
        /// <summary>
        /// 不应用位移（默认）
        /// </summary>
        None,

        /// <summary>
        /// 融合当前速度和动画速度
        /// </summary>
        Blend,

        /// <summary>
        /// 每次循环添加冲量
        /// </summary>
        AddImpulse,

        /// <summary>
        /// 直接覆盖速度
        /// </summary>
        Override
    }

    /// <summary>
    /// 融合方式（Blend 模式）
    /// </summary>
    public enum BlendMethod {
        /// <summary>
        /// 平滑阻尼（推荐）
        /// </summary>
        SmoothDamp,

        /// <summary>
        /// 加权平均
        /// </summary>
        WeightedAverage,

        /// <summary>
        /// 弹簧阻尼模型
        /// </summary>
        SpringDamper
    }

    /// <summary>
    /// 冲量计算方式（AddImpulse 模式）
    /// </summary>
    public enum ImpulseMethod {
        /// <summary>
        /// 平均速度 = 总位移 / 动画时长（默认）
        /// </summary>
        Average,

        /// <summary>
        /// 峰值速度 = 动画中最大瞬时速度
        /// </summary>
        Peak,

        /// <summary>
        /// 加权速度 = 考虑速度分布的加权平均
        /// </summary>
        Weighted
    }

    /// <summary>
    /// 缩放应用模式
    /// </summary>
    public enum ScaleMode {
        /// <summary>
        /// 不应用缩放（默认）
        /// </summary>
        None,

        /// <summary>
        /// 覆盖碰撞体尺寸
        /// </summary>
        Override
    }

    /// <summary>
    /// 缩放数据来源
    /// </summary>
    public enum ScaleSource {
        /// <summary>
        /// 从动画根骨骼缩放提取
        /// </summary>
        Animation,

        /// <summary>
        /// 使用配置的固定值
        /// </summary>
        Fixed
    }

    /// <summary>
    /// 位移应用配置
    /// </summary>
    public class TranslationConfig {
        /// <summary>
        /// 位移应用模式
        /// </summary>
        public TranslationMode Mode { get; set; } = TranslationMode.None;

        // Blend 模式参数

        /// <summary>
        /// 融合方式
        /// </summary>
        public BlendMethod BlendMethod { get; set; } = BlendMethod.SmoothDamp;

        /// <summary>
        /// SmoothDamp 平滑时间
        /// </summary>
        public float SmoothTime { get; set; } = 0.3f;

        /// <summary>
        /// WeightedAverage 权重
        /// </summary>
        public float BlendWeight { get; set; } = 0.5f;

        /// <summary>
        /// SpringDamper 刚度
        /// </summary>
        public float SpringStiffness { get; set; } = 100f;

        /// <summary>
        /// SpringDamper 阻尼
        /// </summary>
        public float SpringDamping { get; set; } = 10f;

        // AddImpulse 模式参数

        /// <summary>
        /// 冲量计算方式
        /// </summary>
        public ImpulseMethod ImpulseMethod { get; set; } = ImpulseMethod.Average;

        /// <summary>
        /// 冲量缩放因子
        /// </summary>
        public float ImpulseScale { get; set; } = 1.0f;

        /// <summary>
        /// 冲量触发的绝对动画相位（0-1）
        /// 0.0 = 动画首帧，1.0 = 动画末帧
        /// 默认 -1 表示自动：正播时使用 endPhase，反播时使用 startPhase
        /// 例如跳跃冲量在动画 18.2% 处触发：0.182
        /// </summary>
        public float ImpulsePhase { get; set; } = -1f;

        /// <summary>
        /// 直接指定冲量值（覆盖动画数据）
        /// </summary>
        public Vector3? ImpulseOverride { get; set; }

        /// <summary>
        /// 冲量速度向量（m/s，body-local 空间：由 TranslationApplier 经 body.Rotation 转世界）。
        /// AddImpulse 模式下若设置，冲量 = value，覆盖动画位移数据。
        /// 可组合方向，如剑击 [0, 2, -6] = 前 6 + 上 2（上扬轻微浮空，减地面阻力作用时间）。
        /// </summary>
        public Vector3? ImpulseSpeedOverride { get; set; }

        // 速度控制

        /// <summary>
        /// 速度遮罩：哪些轴受根运动影响（X, Y, Z 分量，默认全部影响）
        /// </summary>
        public Vector3 VelocityMask { get; set; } = Vector3.One;

        // 安全限制

        /// <summary>
        /// 最大速度限制（米/秒），防止动画数据异常
        /// </summary>
        public float MaxSpeed { get; set; } = 20f;

        /// <summary>
        /// 最大冲量限制（米/秒），仅 AddImpulse 模式
        /// </summary>
        public float MaxImpulse { get; set; } = 50f;
    }

    /// <summary>
    /// 缩放应用配置
    /// </summary>
    public class ScaleConfig {
        /// <summary>
        /// 缩放应用模式
        /// </summary>
        public ScaleMode Mode { get; set; } = ScaleMode.None;

        /// <summary>
        /// 缩放数据来源
        /// </summary>
        public ScaleSource Source { get; set; } = ScaleSource.Animation;

        /// <summary>
        /// 固定缩放值（Fixed 模式）
        /// </summary>
        public Vector3? Value { get; set; }

        /// <summary>
        /// 过渡时长（秒）
        /// </summary>
        public float BlendDuration { get; set; } = 0.2f;

        /// <summary>
        /// 最小缩放限制（默认 0.01）
        /// </summary>
        public Vector3? MinScale { get; set; }
    }

    /// <summary>
    /// 根运动期间的物理副作用配置。由 ComponentModel.ApplyRootMotionPhysics 自动应用：
    /// 进入该 root motion 配置时生效，切出（config 变 null）时恢复。
    /// 副作用在 Animate（绘制阶段）应用，对下一帧 Locomotion/Body 生效（滞后一帧）。
    /// 注：物理副作用以实体为单位作用于 ComponentBody。同一实体多 ComponentModel（身体+服装层）共享同一 body，
    /// 仅应由一个 model 的 RM config 配 Physics，否则多 model 的 ApplyRootMotionPhysics 会在同一 body 上相互踩踏。
    /// </summary>
    public class PhysicsConfig {
        /// <summary>
        /// 禁用重力：在 ComponentBody 重力应用点查 PhysicalEffects.Gravity flag，
        /// 不被 NormalMovement 每帧 IsGravityEnabled=true 重置。
        /// </summary>
        public bool DisableGravity { get; set; }

        /// <summary>
        /// 禁用地形碰撞：设 ComponentBody.TerrainCollidable=false。依赖 TerrainCollidable 字段语义，
        /// 同时禁用移动方块碰撞箱收集（FindMovingBlocksCollisionBoxes 亦由 TerrainCollidable 门控）。
        /// NormalMovement 不重置此字段，设一次即稳定。
        /// </summary>
        public bool DisableTerrainCollision { get; set; }

        /// <summary>
        /// 禁用输入移动：设 ComponentLocomotion.DisableInputMovement=true，
        /// 跳过 NormalMovement（走路/飞行/跳跃/游泳），避免覆盖 root motion 速度。
        /// </summary>
        public bool DisableInputMovement { get; set; }

        /// <summary>
        /// 切出该 root motion 配置时清零物理体速度（消除末速惯性，防甩飞悬空）。
        /// </summary>
        public bool ClearVelocityOnExit { get; set; }

        /// <summary>
        /// 禁用空气阻力：在 ComponentBody 空气阻力应用点查 PhysicalEffects.AirDrag flag。
        /// </summary>
        public bool DisableAirDrag { get; set; }

        /// <summary>
        /// 禁用水阻力：在 ComponentBody 水阻力应用点查 PhysicalEffects.WaterDrag flag。
        /// </summary>
        public bool DisableWaterDrag { get; set; }

        /// <summary>
        /// 禁用地面阻力：在 ComponentBody 地面阻力应用点查 PhysicalEffects.GroundDrag flag。
        /// </summary>
        public bool DisableGroundDrag { get; set; }

        /// <summary>
        /// 一键开关全部物理副作用（含 ClearVelocityOnExit）。
        /// 设 true 时全部开启，false 时全部关闭；读取时返回是否全部开启。
        /// JSON 中用 "disableAll": true 替代逐一列举。单独使用，勿与其他 disable*/clearVelocityOnExit 混用
        /// （System.Text.Json 按属性在 JSON 文档中的出现顺序逐个 set，混用时后出现的覆盖先出现的；
        /// 若需单字段覆盖 disableAll，该字段必须在 JSON 中排在 "disableAll" 之后）。
        /// 运行时 ApplyRootMotionPhysics 仍读各具体字段，本属性仅供配置便捷。
        /// </summary>
        public bool DisableAll {
            get => DisableGravity && DisableTerrainCollision && DisableInputMovement
                && ClearVelocityOnExit && DisableAirDrag && DisableWaterDrag && DisableGroundDrag;
            set {
                DisableGravity = value;
                DisableTerrainCollision = value;
                DisableInputMovement = value;
                ClearVelocityOnExit = value;
                DisableAirDrag = value;
                DisableWaterDrag = value;
                DisableGroundDrag = value;
            }
        }
    }

    /// <summary>
    /// 根运动配置
    /// </summary>
    public class RootMotionConfig {
        /// <summary>
        /// 采样运动数据的骨骼名称。为空时使用自动检测（从 RootBone 向下 BFS 查找第一个有动画数据的骨骼）
        /// </summary>
        public string SourceBone { get; set; }

        /// <summary>
        /// 位移应用配置
        /// </summary>
        public TranslationConfig Translation { get; set; } = new();

        /// <summary>
        /// 缩放应用配置
        /// </summary>
        public ScaleConfig Scale { get; set; } = new();

        /// <summary>
        /// 物理副作用配置。null 表示该 root motion 不产生物理副作用。
        /// </summary>
        public PhysicsConfig Physics { get; set; }
    }
}