using System.Collections.Generic;
using System.Text.Json.Nodes;
using Engine;
using Engine.Animation;
using Engine.Animation.RootMotion;
using Engine.Graphics;
using Engine.Serialization;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentModel : Component {
        public SubsystemSky m_subsystemSky;

        public bool IsSet;
        public bool Animated;
        public bool IsExtrasDrawn;

        public ComponentFrame m_componentFrame;

        public Model m_model;

        public Matrix?[] m_boneTransforms;

        /// <summary>
        /// 同实体上的动画参与者组件（OnEntityAdded 收集）。
        /// </summary>
        public List<ComponentAnimationParticipant> m_animationParticipants;

        public float m_boundingSphereRadius;

        /// <summary>
        /// 上一帧生效的 root motion 物理副作用配置（用于 enter/exit 状态机）。
        /// null 表示当前未应用任何 RM 物理副作用。
        /// </summary>
        PhysicsConfig m_prevRootMotionPhysics;

        /// <summary>
        /// 缓存同实体物理体（Animate body 桥 + ApplyRootMotionPhysics 用），避免每帧 FindComponent。
        /// </summary>
        ComponentBody m_componentBody;

        /// <summary>
        /// 缓存同实体移动组件（ApplyRootMotionPhysics 用），避免每帧 FindComponent。
        /// </summary>
        ComponentLocomotion m_componentLocomotion;

        public AnimationPlayer m_animationPlayer;

        /// <summary>
        /// 动画控制器
        /// </summary>
        public AnimationController AnimationController { get; private set; }

        /// <summary>
        /// 动画模板名称
        /// </summary>
        public string AnimationTemplateName { get; private set; }

        /// <summary>
        /// 动画配置文件路径（可选）
        /// 如果指定，将使用 AnimationConfigLoader 加载配置并创建控制器
        /// </summary>
        public string AnimationConfigJson { get; private set; }

        /// <summary>
        ///     模型偏移
        /// </summary>
        public Vector3 ModelOffset { get; set; }

        /// <summary>
        ///     模型透明度
        /// </summary>
        public float Transparent { get; set; }

        /// <summary>
        ///     模型大小缩放
        /// </summary>
        public float ModelScale { get; set; }

        /// <summary>
        ///     纹理路径
        /// </summary>
        public string TextureRoute { get; set; }

        /// <summary>
        ///     模型路径
        /// </summary>
        public string ModelRoute { get; set; }

        public float? Opacity { get; set; }

        public Vector3? DiffuseColor { get; set; }

        public Vector4? EmissionColor { get; set; }

        public Model Model {
            get => m_model;
            set => SetModel(value);
        }

        public Texture2D TextureOverride { get; set; }

        [Obsolete("Use ModLoader.OnAnimateModel() instead.")]
        public virtual Func<bool> OnAnimate { get; set; }

        public bool CastsShadow { get; set; }

        public int PrepareOrder { get; set; }

        public virtual ModelRenderingMode RenderingMode { get; set; }

        public int[] MeshDrawOrders { get; set; }

        public bool IsVisibleForCamera { get; set; }

        public bool DisableAnimation { get; set; }

        public bool DisableDrawing { get; set; }

        public Matrix[] AbsoluteBoneTransformsForCamera { get; set; }

        public bool VisibleInFppCamera { get; set; }

        public virtual Matrix? GetBoneTransform(int boneIndex) => m_boneTransforms[boneIndex];

        public virtual void SetBoneTransform(int boneIndex, Matrix? transformation) {
            m_boneTransforms[boneIndex] = transformation * Matrix.CreateTranslation(ModelOffset);
        }

        public virtual void CalculateAbsoluteBonesTransforms(Camera camera) {
            bool flag = false;
            ModsManager.HookAction(
                "OnModelCalculateBones",
                loader => {
                    loader.OnModelCalculateBones(this, camera, out bool skip);
                    flag |= skip;
                    return false;
                }
            );
            if (flag) {
                return;
            }
            // 先计算骨骼的世界变换（不包含视图矩阵）
            ProcessBoneHierarchy(Model.RootBone, Matrix.Identity, AbsoluteBoneTransformsForCamera);

            // 然后应用视图矩阵
            for (int i = 0; i < AbsoluteBoneTransformsForCamera.Length; i++) {
                AbsoluteBoneTransformsForCamera[i] = AbsoluteBoneTransformsForCamera[i] * camera.ViewMatrix;
            }
        }

        public virtual void CalculateIsVisible(Camera camera) {
            bool flag = false;
            ModsManager.HookAction(
                "OnModelCalculateIsVisible",
                loader => {
                    loader.OnModelCalculateIsVisible(this, camera, out bool skip);
                    flag |= skip;
                    return false;
                }
            );
            if (flag || DisableDrawing) {
                return;
            }
            if (!VisibleInFppCamera && camera.GameWidget.IsEntityFirstPersonTarget(Entity)) {
                IsVisibleForCamera = false;
                return;
            }
            float num = MathUtils.Sqr(m_subsystemSky.VisibilityRange);
            Vector3 vector = m_componentFrame.Position - camera.ViewPosition;
            vector.Y *= m_subsystemSky.VisibilityRangeYMultiplier;
            if (vector.LengthSquared() < num) {
                BoundingSphere sphere = new(m_componentFrame.Position, m_boundingSphereRadius);
                IsVisibleForCamera = camera.ViewFrustum.Intersection(sphere);
            }
            else {
                IsVisibleForCamera = false;
            }
        }

        public virtual void Animate() {
            // 同步参数（自身 + 参与者），须在 controller.Update 之前，确保状态规则评估时有正确参数
            SyncAnimationParameters();
            SyncParticipants();
            Animated = false;
            ModsManager.HookAction(
                "OnAnimateModel",
                loader => {
                    loader.OnAnimateModel(this, out bool skip);
                    Animated |= skip;
                    return false;
                }
            );
            if (Animated) {
                return;
            }

            // 优先使用动画控制器
            if (AnimationController != null) {
                // 清除上一帧的骨骼变换
                for (int i = 0; i < m_boneTransforms.Length; i++) {
                    m_boneTransforms[i] = null;
                }

                // RootMotion: 同步物理体速度和旋转。
                // 须在 Update 前无条件喂 body 速度：Update 内状态规则可能切到新 RM config（如剑击 AddImpulse），
                // 切换帧 HasRootMotion 仍读旧 config=false → ApplyRootMotion 拿不到 Velocity，首帧冲量被丢弃。
                if (m_componentBody != null) {
                    AnimationController.Velocity = m_componentBody.Velocity;
                    AnimationController.EntityRotation = m_componentBody.Rotation;
                }

                if (!DisableAnimation) {
                    AnimationController.Update(Time.FrameDuration);
                    AnimationController.ComputeBoneTransforms(m_boneTransforms);

                    // RootMotion: 将冲量/速度写回物理体（Update 后读 HasRootMotion，覆盖 config 切换帧）
                    if (m_componentBody != null
                        && AnimationController.HasRootMotion
                        && AnimationController.Velocity.HasValue) {
                        m_componentBody.Velocity = AnimationController.Velocity.Value;
                    }

                    // RootMotion 物理副作用：进入/切出 RM config 时应用/恢复（滞后一帧生效）
                    ApplyRootMotionPhysics();
                }

                Animated = true;
            }
            // 后备：简单动画播放
            else if (m_animationPlayer != null && m_animationPlayer.IsPlaying) {
                // 清除上一帧的骨骼变换
                for (int i = 0; i < m_boneTransforms.Length; i++) {
                    m_boneTransforms[i] = null;
                }

                if (!DisableAnimation) {
                    m_animationPlayer.Update(Time.FrameDuration);
                }
                m_animationPlayer.SampleBoneTransforms(m_boneTransforms);
                m_animationPlayer.SamplePointerTargets(Model);
                m_animationPlayer.SampleMorphWeights(Model);

                // 标记动画已处理
                Animated = true;
            }
        }

        /// <summary>
        /// 应用/恢复 root motion 物理副作用（在 Animate 内 body.Velocity 写回后调用）。
        /// 读 AnimationController.m_currentRootMotionConfig.Physics，按 enter/exit 状态机管理：
        /// - 进入新 Physics（非 null 且 != prev）：apply 禁用 flags
        /// - 保持（== prev）：无操作（flag/bool/TerrainCollidable 均不被 NormalMovement 重置，无需续期）
        /// - 切出（null 且 prev 非 null）：ClearVelocityOnExit 清速 + 恢复物理
        /// - 从一 RM config 切另一 RM config（非 null→非 null）：按旧 config 的 ClearVelocityOnExit 清速 + Restore + apply 新 flags
        /// 用缓存 m_componentBody（独立于 Animate 内 HasRootMotion body 局部变量），因切出帧 config=null → HasRootMotion=false → 那个 body=null，exit 仍须执行。
        /// 注：本方法在 Animate 的 if(!DisableAnimation) 块内调用。若该帧走 Animated 提前返回（mod 经 OnAnimateModel 接管）
        /// 或 DisableAnimation=true，本方法不执行——此时 controller.Update 不跑、config 不自然切换，RM 物理状态冻结。
        /// 正常退出 RM 依赖 controller.Update 推进规则/事件切 config=null，故接管/停动画期间 RM 状态机暂停。
        /// </summary>
        void ApplyRootMotionPhysics() {
            ComponentBody body = m_componentBody;
            if (body == null) {
                return;
            }
            PhysicsConfig phys = AnimationController?.m_currentRootMotionConfig?.Physics;
            ComponentLocomotion loc = m_componentLocomotion;

            if (phys != null) {
                if (!ReferenceEquals(phys, m_prevRootMotionPhysics)) {
                    // 进入新 RM 物理副作用（从另一 RM 切换时先按旧 config 清速 + 恢复旧的）
                    if (m_prevRootMotionPhysics != null) {
                        if (m_prevRootMotionPhysics.ClearVelocityOnExit) {
                            body.Velocity = Vector3.Zero;
                        }
                        RestoreRootMotionPhysics(body, loc);
                    }
                    if (phys.DisableGravity) {
                        body.DisabledPhysicalEffects |= PhysicalEffects.Gravity;
                    }
                    if (phys.DisableTerrainCollision) {
                        body.TerrainCollidable = false;
                    }
                    if (phys.DisableInputMovement && loc != null) {
                        loc.DisableInputMovement = true;
                    }
                    if (phys.DisableAirDrag) {
                        body.DisabledPhysicalEffects |= PhysicalEffects.AirDrag;
                    }
                    if (phys.DisableWaterDrag) {
                        body.DisabledPhysicalEffects |= PhysicalEffects.WaterDrag;
                    }
                    if (phys.DisableGroundDrag) {
                        body.DisabledPhysicalEffects |= PhysicalEffects.GroundDrag;
                    }
                    m_prevRootMotionPhysics = phys;
                }
                // 保持：无需续期（flag/bool/TerrainCollidable 不被 NormalMovement 重置）
            }
            else if (m_prevRootMotionPhysics != null) {
                // 切出 RM：清速 + 恢复物理
                if (m_prevRootMotionPhysics.ClearVelocityOnExit) {
                    body.Velocity = Vector3.Zero;
                }
                RestoreRootMotionPhysics(body, loc);
                m_prevRootMotionPhysics = null;
            }
        }

        /// <summary>
        /// 恢复 root motion 物理副作用（切出时调用）。
        /// </summary>
        void RestoreRootMotionPhysics(ComponentBody body, ComponentLocomotion loc) {
            body.DisabledPhysicalEffects &= ~(PhysicalEffects.Gravity | PhysicalEffects.AirDrag | PhysicalEffects.WaterDrag | PhysicalEffects.GroundDrag);
            body.TerrainCollidable = true;
            if (loc != null) {
                loc.DisableInputMovement = false;
            }
        }

        public virtual void DrawExtras(Camera camera) {
            IsExtrasDrawn = false;
            ModsManager.HookAction(
                "OnModelDrawExtra",
                loader => {
                    loader.OnModelDrawExtra(this, camera, out bool skip);
                    IsExtrasDrawn |= skip;
                    return false;
                }
            );
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_componentFrame = Entity.FindComponent<ComponentFrame>(true);
            m_componentBody = Entity.FindComponent<ComponentBody>();
            m_componentLocomotion = Entity.FindComponent<ComponentLocomotion>();
            ModelRoute = valuesDictionary.GetValue("ModelName", "");
            string modeltype = valuesDictionary.GetValue("ModelType", "Engine.Graphics.Model");
            CastsShadow = valuesDictionary.GetValue<bool>("CastsShadow");
            TextureRoute = valuesDictionary.GetValue("TextureOverride", "");
            TextureOverride = string.IsNullOrEmpty(TextureRoute) ? null : ContentManager.Get<Texture2D>(TextureRoute);
            PrepareOrder = valuesDictionary.GetValue<int>("PrepareOrder");
            Transparent = valuesDictionary.GetValue("Transparent", 1f);
            ModelScale = valuesDictionary.GetValue("ModelScale", 1f);
            m_boundingSphereRadius = valuesDictionary.GetValue<float>("BoundingSphereRadius");
            // 读取动画配置路径（可选）
            string animationConfigPath = valuesDictionary.GetValue("AnimationConfigPath", "");
            if (!string.IsNullOrEmpty(animationConfigPath)) {
                AnimationConfigJson = ContentManager.Get<string>(animationConfigPath, ".json");
            }
            DisableAnimation = valuesDictionary.GetValue<bool>("DisableAnimation", false);
            DisableDrawing = valuesDictionary.GetValue<bool>("DisableDrawing", false);
            VisibleInFppCamera = valuesDictionary.GetValue<bool>("VisibleInFppCamera", false);
            Type type = TypeCache.FindType(modeltype, true, true);
            Model = (Model)ContentManager.Get(type, ModelRoute);
        }

        public virtual void SetModel(Model model) {
            IsSet = false;
            ModsManager.HookAction(
                "OnSetModel",
                modLoader => {
                    modLoader.OnSetModel(this, model, out IsSet);
                    return false;
                }
            );
            if (IsSet) {
                return;
            }
            // 取消旧控制器订阅（在下方重建 controller 之前；mod 通过 OnSetModel 接管时保持原订阅）
            if (AnimationController != null) {
                AnimationController.OnAnimationEvent -= HandleAnimationEvent;
            }
            m_model = model;
            if (m_model != null) {
                m_boneTransforms = new Matrix?[m_model.Bones.Count];
                AbsoluteBoneTransformsForCamera = new Matrix[m_model.Bones.Count];
                MeshDrawOrders = Enumerable.Range(0, m_model.Meshes.Count).ToArray();

                // 初始化动画控制器
                // 优先级：AnimationConfigPath > AnimationTemplateName > 自动播放
                if (!string.IsNullOrEmpty(AnimationConfigJson)) {
                    // 使用配置文件创建控制器
                    var loader = new AnimationConfigLoader();
                    AnimationConfig config = loader.LoadFromJsonNode(JsonNode.Parse(AnimationConfigJson));
                    AnimationController = loader.CreateController(config, m_model);

                    // 应用动画配置中的模型缩放（覆盖 ValuesDictionary 中的值）
                    if (AnimationController.ModelScale != 1f) {
                        ModelScale *= AnimationController.ModelScale;
                    }

                    // 应用骨骼别名表：使旧硬编码骨骼名（Hand1 等）能解析到 glb 真实骨骼名
                    m_model.BoneAliases = config.BoneAliases;
                }
                else if (!string.IsNullOrEmpty(AnimationTemplateName)) {
                    // 使用模板名称创建控制器
                    AnimationController = new AnimationController(m_model, AnimationTemplateName);
                }
                // 直接测试动画用
                /*else if (m_model.HasAnimations) {
                    m_animationPlayer = new AnimationPlayer();
                    m_animationPlayer.SetAnimation(m_model, m_model.Animations[0]);
                    m_animationPlayer.Play(loop: true);
                }*/

                // 订阅新控制器事件 + 通知参与者控制器已就绪
                if (AnimationController != null) {
                    AnimationController.OnAnimationEvent += HandleAnimationEvent;
                    SetupDefaultAnimationEvents();
                    NotifyControllerCreated();
                }
            }
            else {
                m_boneTransforms = null;
                AbsoluteBoneTransformsForCamera = null;
                MeshDrawOrders = null;
                m_animationPlayer = null;
                AnimationController = null;
            }
        }

        /// <summary>
        /// 同步动画参数到控制器（在 Animate 中、controller.Update 之前调用）。
        /// 生物模型子类覆盖此方法同步生物参数；非生物模型默认空。
        /// </summary>
        public virtual void SyncAnimationParameters() { }

        /// <summary>
        /// 处理动画事件。默认转发给同实体的动画参与者；生物模型子类覆盖以处理内置事件（Footstep/Attack 等）。
        /// </summary>
        public virtual void HandleAnimationEvent(AnimationEvent animationEvent) {
            if (animationEvent == null) return;
            if (m_animationParticipants != null && AnimationController != null) {
                foreach (ComponentAnimationParticipant participant in m_animationParticipants) {
                    participant.HandleAnimationEvent(AnimationController, animationEvent);
                }
            }
        }

        /// <summary>
        /// 设置默认动画事件订阅。子类可覆盖以注册特定事件。
        /// </summary>
        public virtual void SetupDefaultAnimationEvents() { }

        public override void OnEntityAdded() {
            base.OnEntityAdded();
            // 收集同实体的动画参与者（此时所有组件已 Load 完成），按 ShouldApplyTo 过滤
            m_animationParticipants = Entity.FindComponents<ComponentAnimationParticipant>()
                .Where(p => p.ShouldApplyTo(this)).ToList();
            // 首次 SetModel 在 Load 内发生时参与者尚未收集，此处补发 OnControllerCreated
            NotifyControllerCreated();
        }

        /// <summary>
        /// 通知所有参与者控制器已就绪（首次 OnEntityAdded + 每次换模型 SetModel 触发）。
        /// </summary>
        private void NotifyControllerCreated() {
            if (m_animationParticipants == null || AnimationController == null) return;
            foreach (ComponentAnimationParticipant participant in m_animationParticipants) {
                participant.OnControllerCreated(AnimationController);
            }
        }

        /// <summary>
        /// 转发参数同步给所有参与者（每帧 Animate 中、controller.Update 之前）。
        /// </summary>
        private void SyncParticipants() {
            if (m_animationParticipants == null || AnimationController == null) return;
            foreach (ComponentAnimationParticipant participant in m_animationParticipants) {
                participant.SyncAnimationParameters(AnimationController);
            }
        }

        public virtual void ProcessBoneHierarchy(ModelBone modelBone, Matrix currentTransform, Matrix[] transforms) {
            Matrix m = modelBone.Transform;
            if (m_boneTransforms[modelBone.Index].HasValue) {
                // AnimationPlayer/AnimationController 输出完整局部变换（含平移），直接替换
                // DAE 模型通过 SetBoneTransform 设旋转，需要保留原始平移
                bool fullTransform = Model.HasSkin
                    || m_animationPlayer?.IsPlaying == true;
                if (fullTransform) {
                    m = m_boneTransforms[modelBone.Index].Value;
                } else {
                    Vector3 translation = m.Translation;
                    m.Translation = Vector3.Zero;
                    m *= m_boneTransforms[modelBone.Index].Value;
                    m.Translation += translation;
                }
            }
            // 根骨骼统一应用 ModelScale
            if (modelBone == Model.RootBone && ModelScale != 1f) {
                m = Matrix.CreateScale(ModelScale) * m;
            }

            // 骨骼世界变换 = 骨骼局部变换 * 父骨骼世界变换
            Matrix.MultiplyRestricted(ref m, ref currentTransform, out transforms[modelBone.Index]);

            foreach (ModelBone childBone in modelBone.ChildBones) {
                ProcessBoneHierarchy(childBone, transforms[modelBone.Index], transforms);
            }
        }
    }
}