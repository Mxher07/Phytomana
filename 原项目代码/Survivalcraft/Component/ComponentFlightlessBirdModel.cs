using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentFlightlessBirdModel : ComponentCreatureModel {
        public ModelBone m_bodyBone;
        public ModelBone m_neckBone;
        public ModelBone m_headBone;
        public ModelBone m_leg1Bone;
        public ModelBone m_leg2Bone;

        public float m_walkAnimationSpeed;
        public float m_walkLegsAngle;
        public float m_walkBobHeight;
        public float m_feedFactor;
        public float m_footstepsPhase;
        public float m_kickFactor;
        public float m_kickPhase;

        [Obsolete("This will not be used anymore.")]
        public float m_legAngle1;
        [Obsolete("This will not be used anymore.")]
        public float m_legAngle2;

        public override float AttackPhase {
            get => m_kickPhase;
            set => m_kickPhase = value;
        }

        public override float AttackFactor {
            get => m_kickFactor;
            set => m_kickFactor = value;
        }

        public override void Update(float dt) {
            float footstepsPhase = m_footstepsPhase;
            float num = m_componentCreature.ComponentLocomotion.SlipSpeed
                ?? Vector3.Dot(m_componentCreature.ComponentBody.Velocity, m_componentCreature.ComponentBody.Matrix.Forward);
            if (MathF.Abs(num) > 0.2f) {
                MovementAnimationPhase += num * dt * m_walkAnimationSpeed;
                m_footstepsPhase += 1.25f * m_walkAnimationSpeed * num * dt;
            }
            else {
                MovementAnimationPhase = 0f;
                m_footstepsPhase = 0f;
            }
            float num2 = (0f - m_walkBobHeight) * MathUtils.Sqr(MathF.Sin((float)Math.PI * 2f * MovementAnimationPhase));
            float num3 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
            Bob += num3 * (num2 - Bob);
            float num4 = MathF.Floor(m_footstepsPhase);
            if (m_footstepsPhase > num4
                && footstepsPhase <= num4) {
                m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f);
            }
            m_feedFactor = FeedOrder ? MathUtils.Min(m_feedFactor + 2f * dt, 1f) : MathUtils.Max(m_feedFactor - 2f * dt, 0f);
            IsAttackHitMoment = false;
            if (AttackOrder) {
                m_kickFactor = MathUtils.Min(m_kickFactor + 6f * dt, 1f);
                float kickPhase = m_kickPhase;
                m_kickPhase = MathUtils.Remainder(m_kickPhase + dt * 2f, 1f);
                if (kickPhase < 0.5f
                    && m_kickPhase >= 0.5f) {
                    IsAttackHitMoment = true;
                }
            }
            else {
                m_kickFactor = MathUtils.Max(m_kickFactor - 6f * dt, 0f);
                if (m_kickPhase != 0f) {
                    if (m_kickPhase > 0.5f) {
                        m_kickPhase = MathUtils.Remainder(MathUtils.Min(m_kickPhase + dt * 2f, 1f), 1f);
                    }
                    else if (m_kickPhase > 0f) {
                        m_kickPhase = MathUtils.Max(m_kickPhase - dt * 2f, 0f);
                    }
                }
            }
            FeedOrder = false;
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

            // 行走参数
            ctrl.Parameters.SetFloat("MovementPhase", MovementAnimationPhase);
            ctrl.Parameters.SetFloat("Bob", Bob);
            ctrl.Parameters.SetFloat("WalkBobHeight", m_walkBobHeight);
            ctrl.Parameters.SetFloat("WalkLegsAngle", m_walkLegsAngle);
            ctrl.Parameters.SetFloat("WalkAnimationSpeed", m_walkAnimationSpeed);

            // 进食参数
            ctrl.Parameters.SetFloat("FeedFactor", m_feedFactor);

            // 攻击参数
            ctrl.Parameters.SetFloat("KickPhase", m_kickPhase);
            ctrl.Parameters.SetFloat("KickFactor", m_kickFactor);

            // 头部追踪角度
            var lookAngles = m_componentCreature.ComponentLocomotion.LookAngles;
            ctrl.Parameters.SetFloat("LookAngleX", lookAngles.X);
            ctrl.Parameters.SetFloat("LookAngleY", lookAngles.Y);

            // 游戏时间（用于进食噪声）
            ctrl.Parameters.SetFloat("GameTime", (float)m_subsystemTime.GameTime);
            ctrl.Parameters.SetFloat("GameTimeDelta", (float)m_subsystemTime.GameTimeDelta);
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
            m_walkAnimationSpeed = valuesDictionary.GetValue<float>("WalkAnimationSpeed");
            m_walkLegsAngle = valuesDictionary.GetValue<float>("WalkLegsAngle");
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
            }
            else {
                m_bodyBone = null;
                m_neckBone = null;
                m_headBone = null;
                m_leg1Bone = null;
                m_leg2Bone = null;
            }

            // 配置驱动器参数
            if (AnimationController != null) {
                // 把 Database.xml 中的参数传递给驱动器
                AnimationController.Parameters.SetFloat("WalkAnimationSpeed", m_walkAnimationSpeed);
                AnimationController.Parameters.SetFloat("WalkLegsAngle", m_walkLegsAngle);
                AnimationController.Parameters.SetFloat("WalkBobHeight", m_walkBobHeight);

                // 运行时参数初始值
                AnimationController.Parameters.SetFloat("MovementPhase", 0f);
                AnimationController.Parameters.SetFloat("Bob", 0f);
                AnimationController.Parameters.SetFloat("FeedFactor", 0f);
                AnimationController.Parameters.SetFloat("KickPhase", 0f);
                AnimationController.Parameters.SetFloat("KickFactor", 0f);
            }
        }
    }
}
