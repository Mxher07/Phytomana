using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentFishModel : ComponentCreatureModel {
        public ModelBone m_bodyBone;

        public ModelBone m_tail1Bone;

        public ModelBone m_tail2Bone;

        public ModelBone m_jawBone;

        public float m_swimAnimationSpeed;

        public bool m_hasVerticalTail;

        public float m_bitingPhase;

        public float m_tailWagPhase;

        public Vector2 m_tailTurn;

        public float m_digInDepth;

        public float m_digInTailPhase;

        public float? BendOrder { get; set; }

        public float DigInOrder { get; set; }

        public override float AttackPhase {
            get => m_bitingPhase;
            set => m_bitingPhase = value;
        }

        public override void Update(float dt) {
            // 游泳相位更新
            if (m_componentCreature.ComponentLocomotion.LastSwimOrder.HasValue
                && m_componentCreature.ComponentLocomotion.LastSwimOrder.Value != Vector3.Zero) {
                float num = m_componentCreature.ComponentLocomotion.LastSwimOrder.Value.LengthSquared() > 0.99f ? 1.75f : 1f;
                MovementAnimationPhase = MathUtils.Remainder(MovementAnimationPhase + m_swimAnimationSpeed * num * dt, 1000f);
            }
            else {
                MovementAnimationPhase = MathUtils.Remainder(MovementAnimationPhase + 0.15f * m_swimAnimationSpeed * dt, 1000f);
            }

            // 转向时的尾巴弯曲
            if (BendOrder.HasValue) {
                if (m_hasVerticalTail) {
                    m_tailTurn.X = 0f;
                    m_tailTurn.Y = BendOrder.Value;
                }
                else {
                    m_tailTurn.X = BendOrder.Value;
                    m_tailTurn.Y = 0f;
                }
            }
            else {
                m_tailTurn.X += MathUtils.Saturate(2f * m_componentCreature.ComponentLocomotion.TurnSpeed * dt)
                    * (0f - m_componentCreature.ComponentLocomotion.LastTurnOrder.X - m_tailTurn.X);
            }

            // 嵌入冰中动画
            if (DigInOrder > m_digInDepth) {
                float num2 = (DigInOrder - m_digInDepth) * MathUtils.Min(1.5f * dt, 1f);
                m_digInDepth += num2;
                m_digInTailPhase += 20f * num2;
            }
            else if (DigInOrder < m_digInDepth) {
                m_digInDepth += (DigInOrder - m_digInDepth) * MathUtils.Min(5f * dt, 1f);
            }

            // 攻击/咬合动画
            float num3 = 0.33f * m_componentCreature.ComponentLocomotion.TurnSpeed;
            float num4 = 1f * m_componentCreature.ComponentLocomotion.TurnSpeed;

            // 从配置读取攻击速度，默认使用原有值
            float attackSpeed = AnimationController?.Parameters.GetFloat("AttackSpeed") ?? 1f;
            num3 *= attackSpeed;
            num4 *= attackSpeed;

            IsAttackHitMoment = false;
            if (AttackOrder || FeedOrder) {
                if (AttackOrder) {
                    m_tailWagPhase = MathUtils.Remainder(m_tailWagPhase + num3 * dt, 1f);
                }
                float bitingPhase = m_bitingPhase;
                m_bitingPhase = MathUtils.Remainder(m_bitingPhase + num4 * dt, 1f);
                if (AttackOrder
                    && bitingPhase < 0.5f
                    && m_bitingPhase >= 0.5f) {
                    IsAttackHitMoment = true;
                }
            }
            else {
                if (m_tailWagPhase != 0f) {
                    m_tailWagPhase = MathUtils.Remainder(MathUtils.Min(m_tailWagPhase + num3 * dt, 1f), 1f);
                }
                if (m_bitingPhase != 0f) {
                    m_bitingPhase = MathUtils.Remainder(MathUtils.Min(m_bitingPhase + num4 * dt, 1f), 1f);
                }
            }

            AttackOrder = false;
            FeedOrder = false;
            BendOrder = null;
            DigInOrder = 0f;
            base.Update(dt);
        }

        /// <summary>
        /// 同步动画参数到动画控制器
        /// </summary>
        public override void SyncAnimationParameters() {
            base.SyncAnimationParameters();

            var ctrl = AnimationController;
            if (ctrl == null) return;

            // 鱼类特有参数
            ctrl.Parameters.SetFloat("TailWagPhase", m_tailWagPhase);
            ctrl.Parameters.SetFloat("TailTurnX", m_tailTurn.X);
            ctrl.Parameters.SetFloat("TailTurnY", m_tailTurn.Y);
            ctrl.Parameters.SetBool("HasVerticalTail", m_hasVerticalTail);
            ctrl.Parameters.SetFloat("DigInDepth", m_digInDepth);
            ctrl.Parameters.SetFloat("DigInTailPhase", m_digInTailPhase);
            ctrl.Parameters.SetBool("IsEmbeddedInIce", m_componentCreature.ComponentBody.IsEmbeddedInIce);

            // 咬合动画参数
            ctrl.Parameters.SetFloat("BitingPhase", m_bitingPhase);
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
            m_hasVerticalTail = valuesDictionary.GetValue<bool>("HasVerticalTail");
            m_swimAnimationSpeed = valuesDictionary.GetValue<float>("SwimAnimationSpeed");
        }

        public override void SetModel(Model model) {
            base.SetModel(model);
            if (IsSet) {
                return;
            }
            if (Model != null) {
                m_bodyBone = Model.FindBone("Body", false);
                m_tail1Bone = Model.FindBone("Tail1", false);
                m_tail2Bone = Model.FindBone("Tail2", false);
                m_jawBone = Model.FindBone("Jaw", false);
            }
            else {
                m_bodyBone = null;
                m_tail1Bone = null;
                m_tail2Bone = null;
                m_jawBone = null;
            }

            // 配置驱动器参数
            if (AnimationController != null) {
                // 把 Database.xml 中的参数传递给驱动器
                AnimationController.Parameters.SetFloat("SwimAnimationSpeed", m_swimAnimationSpeed);
                AnimationController.Parameters.SetBool("HasVerticalTail", m_hasVerticalTail);

                // 初始化运行时参数
                AnimationController.Parameters.SetFloat("TailWagPhase", 0f);
                AnimationController.Parameters.SetFloat("TailTurnX", 0f);
                AnimationController.Parameters.SetFloat("TailTurnY", 0f);
                AnimationController.Parameters.SetFloat("DigInDepth", 0f);
                AnimationController.Parameters.SetFloat("DigInTailPhase", 0f);
                AnimationController.Parameters.SetBool("IsEmbeddedInIce", false);
                AnimationController.Parameters.SetFloat("BitingPhase", 0f);
            }
        }

        public override Vector3 CalculateEyePosition() {
            Matrix matrix = m_componentCreature.ComponentBody.Matrix;
            Vector3 result = m_componentCreature.ComponentBody.Position
                + matrix.Up * 1f * m_componentCreature.ComponentBody.BoxSize.Y
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
    }
}
