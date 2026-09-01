using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentBirdModel : ComponentCreatureModel {
        public bool m_hasWings;

        public ModelBone m_bodyBone;

        public ModelBone m_neckBone;

        public ModelBone m_headBone;

        public ModelBone m_leg1Bone;

        public ModelBone m_leg2Bone;

        public ModelBone m_wing1Bone;

        public ModelBone m_wing2Bone;

        public float m_flyAnimationSpeed;

        public float m_walkAnimationSpeed;

        public float m_peckAnimationSpeed;

        public float m_walkBobHeight;

        public float m_peckPhase;

        public float m_kickPhase;
        public float FlyPhase { get; set; }

        public override float AttackPhase {
            get => m_kickPhase;
            set => m_kickPhase = value;
        }

        public override float AttackFactor { get; set; }

        public override void Update(float dt) {
            float num = Vector3.Dot(m_componentCreature.ComponentBody.Velocity, m_componentCreature.ComponentBody.Matrix.Forward);
            if (MathF.Abs(num) > 0.1f) {
                MovementAnimationPhase += num * dt * m_walkAnimationSpeed;
            }
            else {
                float num2 = MathF.Floor(MovementAnimationPhase);
                if (MovementAnimationPhase != num2) {
                    MovementAnimationPhase = MovementAnimationPhase - num2 > 0.5f
                        ? MathUtils.Min(MovementAnimationPhase + 2f * dt, num2 + 1f)
                        : MathUtils.Max(MovementAnimationPhase - 2f * dt, num2);
                }
            }
            float num3 = (0f - m_walkBobHeight) * MathUtils.Sqr(MathF.Sin((float)Math.PI * 2f * MovementAnimationPhase));
            float num4 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
            Bob += num4 * (num3 - Bob);
            if (m_hasWings) {
                if (m_componentCreature.ComponentLocomotion.LastFlyOrder.HasValue) {
                    float num5 = m_componentCreature.ComponentLocomotion.LastFlyOrder.Value.LengthSquared() > 0.99f ? 1.5f : 1f;
                    FlyPhase = MathUtils.Remainder(FlyPhase + m_flyAnimationSpeed * num5 * dt, 1f);
                    if (m_componentCreature.ComponentLocomotion.LastFlyOrder.Value.Y < -0.1f
                        && m_componentCreature.ComponentBody.Velocity.Length() > 4f) {
                        FlyPhase = 0.72f;
                    }
                }
                else if (FlyPhase != 1f) {
                    FlyPhase = MathUtils.Min(FlyPhase + m_flyAnimationSpeed * dt, 1f);
                }
            }
            if (FeedOrder) {
                m_peckPhase += m_peckAnimationSpeed * dt;
                if (m_peckPhase > 0.75f) {
                    m_peckPhase -= 0.5f;
                }
            }
            else if (m_peckPhase != 0f) {
                m_peckPhase = MathUtils.Remainder(Math.Min(m_peckPhase + m_peckAnimationSpeed * dt, 1f), 1f);
            }
            FeedOrder = false;
            IsAttackHitMoment = false;
            if (AttackOrder) {
                AttackFactor = MathUtils.Min(AttackFactor + 2f * dt, 1f);
                float kickPhase = m_kickPhase;
                m_kickPhase = MathUtils.Remainder(m_kickPhase + dt * 2f, 1f);
                if (kickPhase < 0.5f
                    && m_kickPhase >= 0.5f) {
                    IsAttackHitMoment = true;
                }
            }
            else {
                AttackFactor = MathUtils.Max(AttackFactor - 2f * dt, 0f);
                if (m_kickPhase != 0f) {
                    if (m_kickPhase > 0.5f) {
                        m_kickPhase = MathUtils.Remainder(MathUtils.Min(m_kickPhase + dt * 2f, 1f), 1f);
                    }
                    else if (m_kickPhase > 0f) {
                        m_kickPhase = MathUtils.Max(m_kickPhase - dt * 2f, 0f);
                    }
                }
            }
            AttackOrder = false;
            base.Update(dt);
        }

        /// <summary>
        /// 同步动画参数到动画控制器
        /// </summary>
        public override void SyncAnimationParameters() {
            base.SyncAnimationParameters();

            var ctrl = AnimationController;
            if (ctrl == null) return;

            // 鸟类特有参数
            ctrl.Parameters.SetFloat("MovementPhase", MovementAnimationPhase);
            ctrl.Parameters.SetFloat("FlyPhase", FlyPhase);
            ctrl.Parameters.SetFloat("Bob", Bob);
            ctrl.Parameters.SetFloat("WalkBobHeight", m_walkBobHeight);

            // 啄食和攻击参数
            ctrl.Parameters.SetFloat("PeckPhase", m_peckPhase);
            ctrl.Parameters.SetFloat("KickPhase", m_kickPhase);
            ctrl.Parameters.SetFloat("AttackFactor", m_peckAnimationSpeed);

            // 飞行状态
            ctrl.Parameters.SetFloat("FlySpeed", m_componentCreature.ComponentLocomotion.FlySpeed);

            // 头部追踪角度
            var lookAngles = m_componentCreature.ComponentLocomotion.LookAngles;
            ctrl.Parameters.SetFloat("LookAngleX", lookAngles.X);
            ctrl.Parameters.SetFloat("LookAngleY", lookAngles.Y);

            // 身体旋转（用于死亡动画）
            var rotation = m_componentCreature.ComponentBody.Rotation.ToYawPitchRoll();
            ctrl.Parameters.SetVector3("Rotation", rotation);
        }

        /// <summary>
        /// 动画由 AnimationController 和驱动器处理，不再需要硬编码
        /// </summary>
        public override void AnimateCreature() {
            // 空实现 - 动画由 AnimationController 处理
            // 如果没有配置 AnimationController，生物将不会动画
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_flyAnimationSpeed = valuesDictionary.GetValue<float>("FlyAnimationSpeed");
            m_walkAnimationSpeed = valuesDictionary.GetValue<float>("WalkAnimationSpeed");
            m_peckAnimationSpeed = valuesDictionary.GetValue<float>("PeckAnimationSpeed");
            m_walkBobHeight = valuesDictionary.GetValue<float>("WalkBobHeight");
        }

        public override void SetModel(Model model) {
            base.SetModel(model);
            if (IsSet) {
                return;
            }
            if (Model != null) {
                m_bodyBone = Model.FindBone("Body", false);
                m_neckBone = Model.FindBone("Neck", false);
                m_headBone = Model.FindBone("Head", false);
                m_leg1Bone = Model.FindBone("Leg1", false);
                m_leg2Bone = Model.FindBone("Leg2", false);
                m_wing1Bone = Model.FindBone("Wing1", false);
                m_wing2Bone = Model.FindBone("Wing2", false);
            }
            else {
                m_bodyBone = null;
                m_neckBone = null;
                m_headBone = null;
                m_leg1Bone = null;
                m_leg2Bone = null;
                m_wing1Bone = null;
                m_wing2Bone = null;
            }
            m_hasWings = m_wing1Bone != null && m_wing2Bone != null;

            // 配置驱动器参数
            if (AnimationController != null) {
                // 把 Database.xml 中的参数传递给驱动器
                AnimationController.Parameters.SetFloat("FlyAnimationSpeed", m_flyAnimationSpeed);
                AnimationController.Parameters.SetFloat("WalkAnimationSpeed", m_walkAnimationSpeed);
                AnimationController.Parameters.SetFloat("PeckAnimationSpeed", m_peckAnimationSpeed);
                AnimationController.Parameters.SetFloat("WalkBobHeight", m_walkBobHeight);

                // 运行时参数初始值
                AnimationController.Parameters.SetFloat("MovementPhase", 0f);
                AnimationController.Parameters.SetFloat("FlyPhase", 1f);
                AnimationController.Parameters.SetFloat("PeckPhase", 0f);
                AnimationController.Parameters.SetFloat("KickPhase", 0f);
                AnimationController.Parameters.SetFloat("AttackFactor", 0f);
                AnimationController.Parameters.SetFloat("Bob", 0f);
                AnimationController.Parameters.SetBool("HasWings", m_hasWings);
            }
        }
    }
}