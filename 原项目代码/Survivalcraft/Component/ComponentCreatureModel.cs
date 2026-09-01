using Engine;
using Engine.Animation;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public abstract class ComponentCreatureModel : ComponentModel, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemGameInfo m_subsystemGameInfo;

        public ComponentCreature m_componentCreature;

        public Vector3? m_eyePosition;

        public Quaternion? m_eyeRotation;

        public float m_injuryColorFactor;

        public Vector3 m_randomLookPoint;

        public Random m_random = new();

        /// <summary>
        /// 脚步声计时器，用于防止频繁播放
        /// </summary>
        float m_footstepCooldown;

        /// <summary>
        /// 上次脚步声时间
        /// </summary>
        float m_lastFootstepTime;

        public float Bob { get; set; }

        public float MovementAnimationPhase { get; set; }

        public float DeathPhase { get; set; }

        public Vector3 DeathCauseOffset { get; set; }

        public Vector3? LookAtOrder { get; set; }

        public bool LookRandomOrder { get; set; }

        public float HeadShakeOrder { get; set; }

        public bool AttackOrder { get; set; }

        public bool FeedOrder { get; set; }

        public bool RowLeftOrder { get; set; }

        public bool RowRightOrder { get; set; }

        public float AimHandAngleOrder { get; set; }

        public Vector3 InHandItemOffsetOrder { get; set; }

        public Vector3 InHandItemRotationOrder { get; set; }

        public bool IsAttackHitMoment { get; set; }
        public virtual float AttackPhase { get; set; }
        public virtual float AttackFactor { get; set; }

        public Vector3 EyePosition {
            get {
                if (!m_eyePosition.HasValue) {
                    m_eyePosition = CalculateEyePosition();
                }
                return m_eyePosition.Value;
            }
        }

        public Quaternion EyeRotation {
            get {
                if (!m_eyeRotation.HasValue) {
                    m_eyeRotation = CalculateEyeRotation();
                }
                return m_eyeRotation.Value;
            }
        }

        public UpdateOrder UpdateOrder {
            get {
                ComponentBody parentBody = m_componentCreature.ComponentBody.ParentBody;
                if (parentBody != null) {
                    ComponentCreatureModel componentCreatureModel = parentBody.Entity.FindComponent<ComponentCreatureModel>();
                    if (componentCreatureModel != null) {
                        return componentCreatureModel.UpdateOrder + 1;
                    }
                }
                return UpdateOrder.CreatureModels;
            }
        }

        public override void Animate() {
            // SyncAnimationParameters + 参与者转发已由 base.Animate() 在 controller.Update 之前完成
            base.Animate();

            // glTF 模型（有动画或有蒙皮）需要将实体变换应用到根骨骼
            // 这是 ComponentSimpleModel 中相同的逻辑，但 ComponentCreatureModel 之前遗漏了
            bool isGltfModel = Model.HasSkin || Model.HasAnimations;
            if (Animated && isGltfModel) {
                // 获取实体的位置和旋转
                Vector3 entityPosition = m_componentFrame.Position;
                Quaternion entityRotation = m_componentFrame.Rotation;
                Matrix entityTransform = Matrix.CreateFromQuaternion(entityRotation) * Matrix.CreateTranslation(entityPosition);

                // 获取根骨骼变换（可能是动画采样的或原始的）
                Matrix rootTransform;
                if (m_boneTransforms[Model.RootBone.Index].HasValue) {
                    rootTransform = m_boneTransforms[Model.RootBone.Index].Value;
                } else {
                    rootTransform = Model.RootBone.Transform;
                }

                // 应用根骨骼变换修正（旋转 + 平移，已混合）
                // 顺序 R*T（行向量约定=先 R 后 T）：先绕骨骼原点旋转模型，再平移。
                // 平移在实体轴（不被 R 旋转），即 [0,1,0] 恒指实体上方。
                if (AnimationController != null) {
                    Matrix correction = Matrix.CreateFromQuaternion(AnimationController.EffectiveRootRotation)
                                      * Matrix.CreateTranslation(AnimationController.EffectiveRootTranslation);
                    rootTransform = correction * rootTransform;
                }

                // 叠加实体变换
                m_boneTransforms[Model.RootBone.Index] = rootTransform * entityTransform;
            }

            if (!Animated) {
                bool flag = false;
                ModsManager.HookAction(
                    "OnModelAnimate",
                    loader => {
#pragma warning disable CS0618
                        loader.OnModelAnimate(this, out bool skip);
#pragma warning restore CS0618
                        flag = flag | skip;
                        return false;
                    }
                );
                if (!flag) {
                    AnimateCreature();
                }
            }
            float opacity = m_componentCreature.ComponentSpawn.SpawnDuration > 0f
                ? (float)MathUtils.Saturate(
                    (m_subsystemGameInfo.TotalElapsedGameTime - m_componentCreature.ComponentSpawn.SpawnTime)
                    / m_componentCreature.ComponentSpawn.SpawnDuration
                )
                : 1f;
            Opacity = MathUtils.Min(opacity, Transparent);
            if (m_componentCreature.ComponentSpawn.DespawnTime.HasValue) {
                Opacity = MathUtils.Min(
                    Opacity.Value,
                    (float)MathUtils.Saturate(
                        1.0
                        - (m_subsystemGameInfo.TotalElapsedGameTime - m_componentCreature.ComponentSpawn.DespawnTime.Value)
                        / m_componentCreature.ComponentSpawn.DespawnDuration
                    )
                );
            }
            DiffuseColor = Vector3.Lerp(Vector3.One, new Vector3(1f, 0f, 0f), m_injuryColorFactor);
            if (Opacity.HasValue
                && Opacity.Value < 1f) {
                bool num = m_componentCreature.ComponentBody.ImmersionFactor >= 1f;
                bool flag = m_subsystemSky.ViewUnderWaterDepth > 0f;
                RenderingMode = num == flag ? ModelRenderingMode.TransparentAfterWater : ModelRenderingMode.TransparentBeforeWater;
            }
            else {
                RenderingMode = ModelRenderingMode.Solid;
            }
        }

        public abstract void AnimateCreature();

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentCreature.ComponentHealth.Injured += delegate(Injury injury) {
                ComponentCreature attacker = injury.Attacker;
                if (attacker == null) {
                    return;
                }
                if (DeathPhase == 0f
                    && m_componentCreature.ComponentHealth.Health == 0f) {
                    DeathCauseOffset = attacker.ComponentBody.BoundingBox.Center() - m_componentCreature.ComponentBody.BoundingBox.Center();
                }
            };
        }

        /// <summary>
        /// 处理动画事件：转发给参与者后处理内置事件（Footstep/Attack 等）。
        /// </summary>
        public override void HandleAnimationEvent(AnimationEvent animationEvent) {
            base.HandleAnimationEvent(animationEvent);
            if (animationEvent == null) return;

            switch (animationEvent.Name) {
                case "Footstep":
                    OnFootstepEvent(animationEvent);
                    break;
                case "AttackHit":
                    OnAttackHitEvent(animationEvent);
                    break;
                case "AttackStart":
                    OnAttackStartEvent(animationEvent);
                    break;
                case "AttackEnd":
                    OnAttackEndEvent(animationEvent);
                    break;
            }
        }

        /// <summary>
        /// 处理脚步声事件
        /// </summary>
        public virtual void OnFootstepEvent(AnimationEvent animationEvent) {
            // 检查冷却时间，防止频繁触发
            if (m_footstepCooldown > 0f) return;

            // 检查是否在地面或水中
            var body = m_componentCreature.ComponentBody;
            if (body.StandingOnValue.HasValue || body.ImmersionFactor > 0.5f) {
                // 触发布局音效系统（需要 Mod 扩展）
                ModsManager.HookAction(
                    "OnCreatureFootstep",
                    loader => {
                        loader.OnCreatureFootstep(m_componentCreature, animationEvent.Parameter);
                        return false;
                    }
                );
                m_footstepCooldown = 0.2f; // 200ms 冷却
                m_lastFootstepTime = (float)m_subsystemTime.GameTime;
            }
        }

        /// <summary>
        /// 处理攻击命中事件
        /// </summary>
        public virtual void OnAttackHitEvent(AnimationEvent animationEvent) {
            // 标记攻击命中帧
            IsAttackHitMoment = true;

            // 通知 Mod 系统
            ModsManager.HookAction(
                "OnCreatureAttackHit",
                loader => {
                    loader.OnCreatureAttackHit(m_componentCreature, animationEvent.Parameter);
                    return false;
                }
            );
        }

        /// <summary>
        /// 处理攻击开始事件
        /// </summary>
        public virtual void OnAttackStartEvent(AnimationEvent animationEvent) {
            ModsManager.HookAction(
                "OnCreatureAttackStart",
                loader => {
                    loader.OnCreatureAttackStart(m_componentCreature, animationEvent.Parameter);
                    return false;
                }
            );
        }

        /// <summary>
        /// 处理攻击结束事件
        /// </summary>
        public virtual void OnAttackEndEvent(AnimationEvent animationEvent) {
            IsAttackHitMoment = false;

            ModsManager.HookAction(
                "OnCreatureAttackEnd",
                loader => {
                    loader.OnCreatureAttackEnd(m_componentCreature, animationEvent.Parameter);
                    return false;
                }
            );
        }

        public override void OnEntityAdded() {
            base.OnEntityAdded();
            m_componentCreature.ComponentBody.PositionChanged += delegate { m_eyePosition = null; };
            m_componentCreature.ComponentBody.RotationChanged += delegate { m_eyeRotation = null; };
        }

        public virtual void Update(float dt) {
            // 更新脚步声冷却时间
            if (m_footstepCooldown > 0f) {
                m_footstepCooldown -= dt;
            }

            if (LookRandomOrder) {
                Matrix matrix = m_componentCreature.ComponentBody.Matrix;
                Vector3 v = Vector3.Normalize(m_randomLookPoint - m_componentCreature.ComponentCreatureModel.EyePosition);
                if (m_random.Float(0f, 1f) < 0.25f * dt
                    || Vector3.Dot(matrix.Forward, v) < 0.2f) {
                    float s = m_random.Float(-5f, 5f);
                    float s2 = m_random.Float(-1f, 1f);
                    float s3 = m_random.Float(3f, 8f);
                    m_randomLookPoint = m_componentCreature.ComponentCreatureModel.EyePosition
                        + s3 * matrix.Forward
                        + s2 * matrix.Up
                        + s * matrix.Right;
                }
                LookAtOrder = m_randomLookPoint;
            }
            if (LookAtOrder.HasValue) {
                Vector3 forward = m_componentCreature.ComponentBody.Matrix.Forward;
                Vector3 v2 = LookAtOrder.Value - m_componentCreature.ComponentCreatureModel.EyePosition;
                float x = Vector2.Angle(new Vector2(forward.X, forward.Z), new Vector2(v2.X, v2.Z));
                float y = MathF.Asin(0.99f * Vector3.Normalize(v2).Y);
                m_componentCreature.ComponentLocomotion.LookOrder = new Vector2(x, y) - m_componentCreature.ComponentLocomotion.LookAngles;
            }
            if (HeadShakeOrder > 0f) {
                HeadShakeOrder = MathUtils.Max(HeadShakeOrder - dt, 0f);
                float num = 1f * MathUtils.Saturate(4f * HeadShakeOrder);
                m_componentCreature.ComponentLocomotion.LookOrder = new Vector2(
                        num * (float)Math.Sin(16.0 * m_subsystemTime.GameTime + 0.01f * GetHashCode()),
                        0f
                    )
                    - m_componentCreature.ComponentLocomotion.LookAngles;
            }
            if (m_componentCreature.ComponentHealth.Health == 0f) {
                // 死亡速度从配置读取，默认 3f
                float deathSpeed = AnimationController?.Parameters.GetFloat("DeathSpeed") ?? 3f;
                DeathPhase = MathUtils.Min(DeathPhase + deathSpeed * dt, 1f);
            }
            m_eyePosition = null;
            m_eyeRotation = null;
            LookRandomOrder = false;
            LookAtOrder = null;
        }

        /// <summary>
        /// 同步动画参数到动画控制器
        /// </summary>
        public override void SyncAnimationParameters() {
            base.SyncAnimationParameters();
            var ctrl = AnimationController;
            if (ctrl == null) return;

            // 运动参数
            ctrl.Parameters.SetFloat("MovementPhase", MovementAnimationPhase);
            ctrl.Parameters.SetFloat("DeathPhase", DeathPhase);
            ctrl.Parameters.SetFloat("GameTime", (float)m_subsystemTime.GameTime);  // 用于进食噪声

            // 死亡动画参数
            ctrl.Parameters.SetVector3("DeathCauseOffset", DeathCauseOffset);
            var boundingBox = m_componentCreature.ComponentBody.BoundingBox;
            ctrl.Parameters.SetFloat("BodyHeight", boundingBox.Max.Y - boundingBox.Min.Y);

            // ComponentBody 参数
            var body = m_componentCreature.ComponentBody;
            var velocity = body.Velocity;
            var matrix = body.Matrix;

            // 世界坐标（驱动器需要用来定位身体骨骼）
            ctrl.Parameters.SetVector3("Position", body.Position);
            ctrl.Parameters.SetVector3("Rotation", body.Rotation.ToYawPitchRoll());  // 完整旋转
            ctrl.Parameters.SetFloat("RotationY", body.Rotation.ToYawPitchRoll().X);
            ctrl.Parameters.SetVector3("BodyForward", matrix.Forward);  // 用于死亡方向计算
            ctrl.Parameters.SetVector3("BodyRight", matrix.Right);

            // 速度参数（优先使用 SlipSpeed，用于滑行时的动画同步）
            // 原始代码: float num = SlipSpeed ?? Vector3.Dot(Velocity, Forward)
            var locomotion = m_componentCreature.ComponentLocomotion;
            ctrl.Parameters.SetFloat("Speed", locomotion?.SlipSpeed ?? Vector3.Dot(velocity, matrix.Forward));
            ctrl.Parameters.SetFloat("SpeedAbs", velocity.Length());
            ctrl.Parameters.SetBool("IsInWater", body.ImmersionFactor > 0);
            ctrl.Parameters.SetBool("IsOnGround", body.StandingOnValue.HasValue);
            ctrl.Parameters.SetFloat("ImmersionFactor", body.ImmersionFactor);

            // ComponentLocomotion 参数
            if (locomotion != null) {
                ctrl.Parameters.SetBool("IsFlying", locomotion.m_flying);
                ctrl.Parameters.SetBool("IsCreativeFly", locomotion.IsCreativeFlyEnabled);
                ctrl.Parameters.SetFloat("WalkSpeed", locomotion.WalkSpeed);
            }

            // ComponentHealth 参数
            var health = m_componentCreature.ComponentHealth;
            if (health != null) {
                ctrl.Parameters.SetFloat("Health", health.Health);
                ctrl.Parameters.SetBool("IsDead", health.Health <= 0);
            }

            // 头部追踪
            if (LookAtOrder.HasValue) {
                Vector3 lookDir = LookAtOrder.Value - EyePosition;
                float lookX = MathF.Atan2(lookDir.X, lookDir.Z);
                float lookY = MathF.Asin(lookDir.Y / lookDir.Length());
                ctrl.Parameters.SetFloat("LookAngleX", lookX);
                ctrl.Parameters.SetFloat("LookAngleY", lookY);
            }

            // 活动状态
            ctrl.Parameters.SetBool("IsAttacking", AttackOrder);
            ctrl.Parameters.SetBool("IsFeeding", FeedOrder);
        }

        public virtual Vector3 CalculateEyePosition() {
            Matrix matrix = m_componentCreature.ComponentBody.Matrix;
            Vector3 result = m_componentCreature.ComponentBody.Position
                + matrix.Up * 0.95f * m_componentCreature.ComponentBody.BoxSize.Y
                + matrix.Forward * 0.45f * m_componentCreature.ComponentBody.BoxSize.Z;
            ModsManager.HookAction(
                "RecalculateModelEyePosition",
                loader => {
                    loader.RecalculateModelEyePosition(this, ref result);
                    return false;
                }
            );
            return result;
        }

        public virtual Quaternion CalculateEyeRotation() {
            Quaternion result = m_componentCreature.ComponentBody.Rotation
                * Quaternion.CreateFromYawPitchRoll(
                    0f - m_componentCreature.ComponentLocomotion.LookAngles.X,
                    m_componentCreature.ComponentLocomotion.LookAngles.Y,
                    0f
                );
            ModsManager.HookAction(
                "RecalculateModelEyeRotation",
                loader => {
                    loader.RecalculateModelEyeRotation(this, ref result);
                    return false;
                }
            );
            return result;
        }
    }
}